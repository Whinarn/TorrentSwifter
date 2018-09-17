#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

// Specification: http://www.bittorrent.org/beps/bep_0016.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TorrentSwifter.Helpers;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Torrents.Modes
{
    // TODO: Improve the performance of this super-seeding mode.
    //       The upload speed is very limited when this mode is active.

    /// <summary>
    /// Super-seeding mode.
    /// </summary>
    public class SuperSeedMode : TorrentModeBase
    {
        private List<Peer> tempPeerList = new List<Peer>(50);
        private Dictionary<Peer, int> assignedPeers = new Dictionary<Peer, int>(50);
        private int[] piecePeerCounts = null;

        private int isUpdating = 0;
        private object syncObj = new object();

        /// <summary>
        /// Gets if we should request all peers for the same piece block.
        /// </summary>
        public override bool RequestAllPeersForSameBlock
        {
            get { return false; }
        }

        /// <summary>
        /// Gets if we mask our bitmasks for connected peers, useful for maquerading as a leecher when for example super-seeding.
        /// Note that this will also disable the have-piece messages normally sent to peers once we have verified a new piece.
        /// </summary>
        public override bool MaskBitmasks
        {
            get { return true; }
        }

        /// <summary>
        /// Called when the assigned torrent has changed.
        /// </summary>
        protected override void OnTorrentChanged()
        {
            lock (syncObj)
            {
                tempPeerList.Clear();
                assignedPeers.Clear();

                if (torrent != null)
                    piecePeerCounts = new int[torrent.PieceCount];
                else
                    piecePeerCounts = null;
            }
        }

        /// <summary>
        /// Updates this mode.
        /// </summary>
        public override void Update()
        {
            // Prevents us from running this update more than once at the same time
            if (Interlocked.CompareExchange(ref isUpdating, 1, 0) != 0)
                return;

            lock (syncObj)
            {
                // Check for peers that should no longer be assigned
                var peerList = tempPeerList;
                peerList.Clear();
                foreach (var assignedPeer in assignedPeers)
                {
                    Peer peer = assignedPeer.Key;
                    int pieceIndex = assignedPeer.Value;

                    // If the peer is no longer connected or now has this piece we will unassign them from the piece
                    if (!peer.IsConnected || (peer.BitField != null && peer.BitField.Get(pieceIndex)))
                    {
                        peerList.Add(peer);
                        --piecePeerCounts[pieceIndex];
                    }
                }

                // Remove assigned peers that are no longer connected
                foreach (var peer in peerList)
                {
                    UnregisterPeerEvents(peer);
                    assignedPeers.Remove(peer);
                }

                var rankedPieces = GetRankedPieces(torrent);
                peerList.Clear();
                foreach (var piece in rankedPieces)
                {
                    torrent.GetPeersWithoutPiece(piece.Index, true, peerList);
                    RemoveAssignedPeersFromList(peerList);
                    if (peerList.Count == 0)
                        continue;

                    var assignedPeer = RandomHelper.GetRandomFromList(peerList);
                    if (assignedPeer == null)
                        continue;

                    RegisterPeerEvents(assignedPeer);
                    assignedPeer.ReportHavePiece(piece.Index);
                    assignedPeers[assignedPeer] = piece.Index;
                    ++piecePeerCounts[piece.Index];
                }
            }

            // Reset the flag because we are no longer updating
            Interlocked.Exchange(ref isUpdating, 0);
        }

        private IEnumerable<TorrentPiece> GetRankedPieces(Torrent torrent)
        {
            return GetVerifiedPieces(torrent).OrderByDescending(piece => piece.Rarity - (double)piecePeerCounts[piece.Index] * 0.1);
        }

        private IEnumerable<TorrentPiece> GetVerifiedPieces(Torrent torrent)
        {
            int pieceCount = torrent.PieceCount;
            for (int i = 0; i < pieceCount; i++)
            {
                var piece = torrent.GetPiece(i);
                if (!piece.IsVerified)
                    continue;

                yield return piece;
            }
        }

        private void RemoveAssignedPeersFromList(List<Peer> peerList)
        {
            foreach (var peer in assignedPeers.Keys)
            {
                peerList.Remove(peer);
            }
        }

        private void RegisterPeerEvents(Peer peer)
        {
            peer.Disconnected += OnPeerDisconnect;
            peer.BitFieldReceived += OnPeerBitfieldReceived;
            peer.HavePieceReceived += OnPeerHavePiece;
        }

        private void UnregisterPeerEvents(Peer peer)
        {
            peer.Disconnected -= OnPeerDisconnect;
            peer.BitFieldReceived -= OnPeerBitfieldReceived;
            peer.HavePieceReceived -= OnPeerHavePiece;
        }

        private void OnPeerDisconnect(object sender, EventArgs e)
        {
            Peer peer = (sender as Peer);
            if (peer != null)
            {
                UnregisterPeerEvents(peer);
            }

            Update();
        }

        private void OnPeerBitfieldReceived(object sender, BitFieldEventArgs e)
        {
            Update();
        }

        private void OnPeerHavePiece(object sender, PieceEventArgs e)
        {
            Update();
        }
    }
}
