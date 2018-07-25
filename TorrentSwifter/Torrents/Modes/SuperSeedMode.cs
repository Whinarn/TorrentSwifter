// Specification: http://www.bittorrent.org/beps/bep_0016.html

using System;
using System.Collections.Generic;
using System.Linq;
using TorrentSwifter.Helpers;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// Super-seeding mode.
    /// </summary>
    public class SuperSeedMode : TorrentModeBase
    {
        private List<Peer> tempPeerList = new List<Peer>(50);
        private Dictionary<Peer, int> assignedPeers = new Dictionary<Peer, int>(50);
        private int[] piecePeerCounts = null;
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

                    assignedPeers[assignedPeer] = piece.Index;
                    ++piecePeerCounts[piece.Index];
                }
            }
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
    }
}
