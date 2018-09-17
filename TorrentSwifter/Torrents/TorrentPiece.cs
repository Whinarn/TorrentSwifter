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
    /// A torrent piece.
    /// </summary>
    public sealed class TorrentPiece
    {
        #region Fields
        private readonly Torrent torrent;
        private readonly int index;
        private readonly long offset;
        private readonly int size;

        private volatile bool isVerified = false;
        private volatile bool isVerifying = false;

        private TorrentBlock[] blocks = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the parent torrent of this piece.
        /// </summary>
        public Torrent Torrent
        {
            get { return torrent; }
        }

        /// <summary>
        /// Gets the index of this piece.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// Gets the offset for this piece within the torrent.
        /// </summary>
        public long Offset
        {
            get { return offset; }
        }

        /// <summary>
        /// Gets the size of this piece in bytes.
        /// </summary>
        public int Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets the count of blocks in this piece.
        /// </summary>
        public int BlockCount
        {
            get { return blocks.Length; }
        }

        /// <summary>
        /// Gets if this piece has been fully downloaded and verified.
        /// </summary>
        public bool IsVerified
        {
            get { return isVerified; }
            internal set
            {
                if (isVerified != value)
                {
                    isVerified = value;
                    if (isVerified)
                    {
                        for (int i = 0; i < blocks.Length; i++)
                        {
                            blocks[i].IsDownloaded = true;
                            blocks[i].HasWrittenToDisk = true;
                        }
                    }
                    else if (HasDownloadedAllBlocks())
                    {
                        // Reset the download state of all blocks if we have downloaded all blocks
                        // but the hash was still not correct.
                        for (int i = 0; i < blocks.Length; i++)
                        {
                            blocks[i].IsDownloaded = false;
                            blocks[i].HasWrittenToDisk = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets if this piece is currently being verified.
        /// </summary>
        public bool IsVerifying
        {
            get { return isVerifying; }
            internal set { isVerifying = value; }
        }

        /// <summary>
        /// Gets the download progress of this piece from zero to one.
        /// </summary>
        public double DownloadProgress
        {
            get
            {
                if (isVerified)
                    return 1.0;

                int downloadCount = 0;
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (blocks[i].IsDownloaded)
                    {
                        ++downloadCount;
                    }
                }
                return ((double)downloadCount / (double)blocks.Length);
            }
        }

        /// <summary>
        /// Gets the rarity of this piece between zero and one with one meaning that only one peer has this piece and zero that everyone has it.
        /// The only exception is that this will return double.PositiveInfinity if there are no peers available with this piece.
        /// </summary>
        public double Rarity
        {
            get
            {
                int totalPeerCount = torrent.PeerCount;
                if (totalPeerCount == 0)
                    return double.PositiveInfinity;

                int peerCountWithPiece = torrent.GetPeerCountWithPiece(index);
                if (peerCountWithPiece == 0)
                    return double.PositiveInfinity;
                else if (peerCountWithPiece == 1)
                    return 1.0;

                return (1.0 - ((double)peerCountWithPiece / (double)totalPeerCount));
            }
        }

        /// <summary>
        /// Gets the importance of this piece to us (in terms of downloading) with a higher value being more important than a lower value.
        /// </summary>
        public double Importance
        {
            get
            {
                if (isVerified)
                    return 0.0;

                double downloadProgress = this.DownloadProgress;
                if (downloadProgress >= 1.0)
                    return 0.0;

                double rarity = this.Rarity;
                if (double.IsPositiveInfinity(rarity)) // If no peer has the piece
                    return 0.0;

                return (downloadProgress * 2.0) + rarity;
            }
        }
        #endregion

        #region Constructor
        internal TorrentPiece(Torrent torrent, int index, long offset, int size, int blockSize)
        {
            this.torrent = torrent;
            this.index = index;
            this.offset = offset;
            this.size = size;

            int lastBlockSize = (size % blockSize);
            if (lastBlockSize == 0)
                lastBlockSize = blockSize;

            int blockCount = (((size - 1) / blockSize) + 1);
            blocks = new TorrentBlock[blockCount];
            for (int i = 0; i < blockCount; i++)
            {
                bool isLastPiece = (i == (blockCount - 1));
                int currentBlockSize = (isLastPiece ? lastBlockSize : blockSize);
                blocks[i] = new TorrentBlock(i, currentBlockSize);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a block by its index.
        /// </summary>
        /// <param name="blockIndex">The block index.</param>
        /// <returns>The block.</returns>
        public TorrentBlock GetBlock(int blockIndex)
        {
            return blocks[blockIndex];
        }

        /// <summary>
        /// Returns if we have downloaded all blocks in this piece (as well as written them all to disk).
        /// Does not guarantee that the integrity is intact.
        /// </summary>
        /// <returns>If all blocks are downloaded.</returns>
        public bool HasDownloadedAllBlocks()
        {
            if (isVerified)
                return true;

            bool result = true;

            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                if (!block.IsDownloaded || !block.HasWrittenToDisk)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }
        #endregion
    }
}
