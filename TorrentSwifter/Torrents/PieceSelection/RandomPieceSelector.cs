using System;
using System.Collections.Generic;
using System.Linq;
using TorrentSwifter.Helpers;

namespace TorrentSwifter.Torrents.PieceSelection
{
    /// <summary>
    /// A piece selector that randomly selects pieces for download.
    /// </summary>
    public sealed class RandomPieceSelector : IPieceSelector
    {
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
                .OrderByDescending((piece) => piece.DownloadProgress + RandomHelper.NextDouble(0.2));
        }
    }
}
