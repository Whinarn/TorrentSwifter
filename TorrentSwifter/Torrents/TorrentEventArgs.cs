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

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// Torrent piece event arguments.
    /// </summary>
    public sealed class PieceEventArgs : EventArgs
    {
        private readonly int pieceIndex;

        /// <summary>
        /// Gets the piece index.
        /// </summary>
        public int PieceIndex
        {
            get { return pieceIndex; }
        }

        /// <summary>
        /// Creates new torrent piece event arguments.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        public PieceEventArgs(int pieceIndex)
        {
            this.pieceIndex = pieceIndex;
        }
    }

    /// <summary>
    /// Torrent bit field event arguments.
    /// </summary>
    public sealed class BitFieldEventArgs : EventArgs
    {
        private readonly BitField bitField;

        /// <summary>
        /// Gets the bit field.
        /// </summary>
        public BitField BitField
        {
            get { return bitField; }
        }

        /// <summary>
        /// Creates new torrent bit field event arguments.
        /// </summary>
        /// <param name="bitField">The bit field.</param>
        public BitFieldEventArgs(BitField bitField)
        {
            this.bitField = bitField;
        }
    }
}
