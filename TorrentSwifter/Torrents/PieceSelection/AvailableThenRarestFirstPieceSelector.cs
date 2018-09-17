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

using System;
using System.Collections.Generic;
using System.Linq;
using TorrentSwifter.Helpers;

namespace TorrentSwifter.Torrents.PieceSelection
{
    /// <summary>
    /// A piece selector that selects a random highly available piece first, and then the most rare pieces.
    /// </summary>
    public sealed class AvailableThenRarestFirstPieceSelector : IPieceSelector
    {
        private const double DefaultRandomNoise = 0.05;

        private double randomNoise = DefaultRandomNoise;

        /// <summary>
        /// Creates a new available-then-rarest-first piece selector with a default random noise of 0.05
        /// </summary>
        public AvailableThenRarestFirstPieceSelector()
        {

        }

        /// <summary>
        /// Creates a new available-then-rarest-first piece selector with a specific random noise.
        /// </summary>
        /// <param name="randomNoise">The random noise applied that makes sure that some random is applied to the selection.</param>
        public AvailableThenRarestFirstPieceSelector(double randomNoise)
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
            if (HasDownloadedAnyPiece(pieces))
            {
                // If we have downloaded at least one piece, then we start with the most rare pieces
                return pieces.Where((piece) => !piece.IsVerified)
                    .OrderByDescending((piece) => piece.Importance + RandomHelper.NextDouble(randomNoise));
            }
            else
            {
                // If we have not downloaded any piece yet, start with a piece with high availability first, so that we have something to upload quickly
                return pieces.OrderByDescending((piece) => piece.DownloadProgress + (1.0 - (double.IsPositiveInfinity(piece.Rarity) ? 10.0 : piece.Rarity)));
            }
        }

        private static bool HasDownloadedAnyPiece(TorrentPiece[] pieces)
        {
            bool result = false;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].IsVerified)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
