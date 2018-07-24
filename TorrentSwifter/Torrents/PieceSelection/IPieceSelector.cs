using System;
using System.Collections.Generic;

namespace TorrentSwifter.Torrents.PieceSelection
{
    /// <summary>
    /// An interface for piece selectors.
    /// </summary>
    public interface IPieceSelector
    {
        /// <summary>
        /// Returns the ranked pieces for downloading from this piece selector.
        /// Note that this should not return already downloaded and verified pieces.
        /// </summary>
        /// <param name="torrent">The torrent.</param>
        /// <param name="pieces">The torrent pieces.</param>
        /// <returns>The enumeration of pieces.</returns>
        IEnumerable<TorrentPiece> GetRankedPieces(Torrent torrent, TorrentPiece[] pieces);
    }
}
