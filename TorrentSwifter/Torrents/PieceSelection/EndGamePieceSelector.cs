using System;
using System.Collections.Generic;
using System.Linq;

namespace TorrentSwifter.Torrents.PieceSelection
{
    /// <summary>
    /// A piece selector that is allowed to request the same pieces and blocks to several peers to speed up the final download.
    /// </summary>
    public sealed class EndGamePieceSelector : IPieceSelector
    {
        /// <summary>
        /// Gets the maximum duplicated request count.
        /// One or below means that no duplicated requests are allowed.
        /// </summary>
        public int MaxDuplicatedRequestCount
        {
            get { return int.MaxValue; }
        }

        /// <summary>
        /// Returns the ranked pieces for downloading from this piece selector.
        /// Note that this should not return already downloaded and verified pieces.
        /// </summary>
        /// <param name="torrent">The torrent.</param>
        /// <param name="pieces">The torrent pieces.</param>
        /// <returns>The enumeration of pieces.</returns>
        public IEnumerable<TorrentPiece> GetRankedPieces(Torrent torrent, TorrentPiece[] pieces)
        {
            return pieces.Where((piece) => !piece.IsVerified)
                .OrderByDescending((piece) => piece.Importance);
        }
    }
}
