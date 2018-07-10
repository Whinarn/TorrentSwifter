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
