// Specification: http://www.bittorrent.org/beps/bep_0016.html

using System;
using System.Collections.Generic;
using System.Linq;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Torrents.Modes
{
    /// <summary>
    /// Super-seeding mode.
    /// </summary>
    public class SuperSeedMode : TorrentModeBase
    {
        private List<Peer> tempPeerList = new List<Peer>(50);
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
                var rankedPieces = GetRankedPieces(torrent);
                foreach (var piece in rankedPieces)
                {
                    torrent.GetPeersWithoutPiece(piece.Index, true, tempPeerList);
                    if (tempPeerList.Count == 0)
                        continue;

                    // TODO: What next?
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
    }
}
