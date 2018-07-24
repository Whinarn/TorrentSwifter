using System;
using System.Collections.Generic;
using System.Linq;
using TorrentSwifter.Helpers;

namespace TorrentSwifter.Torrents.PieceSelection
{
    /// <summary>
    /// A piece selector that selects the most reare pieces.
    /// </summary>
    public sealed class RarestFirstPieceSelector : IPieceSelector
    {
        private const double DefaultRandomNoise = 0.05;

        private double randomNoise = DefaultRandomNoise;

        /// <summary>
        /// Creates a new rarest-first piece selector with a default random noise of 0.05
        /// </summary>
        public RarestFirstPieceSelector()
        {

        }

        /// <summary>
        /// Creates a new rarest-first piece selector with a specific random noise.
        /// </summary>
        /// <param name="randomNoise">The random noise applied that makes sure that some random is applied to the selection.</param>
        public RarestFirstPieceSelector(double randomNoise)
        {
            this.randomNoise = Math.Max(randomNoise, 0.0);
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
                .OrderByDescending((piece) => piece.Importance + RandomHelper.NextDouble(randomNoise));
        }
    }
}
