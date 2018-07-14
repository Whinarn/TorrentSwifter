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
        /// Gets the importance of this piece to us (in terms of downloading).
        /// </summary>
        public double Importance
        {
            get
            {
                if (isVerified)
                    return 0.0;

                double downloadProgress = GetDownloadProgress();
                if (downloadProgress >= 1.0)
                    return 0.0;

                double rarity = GetRarity();
                return downloadProgress + rarity;
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

        #region Private Methods
        private double GetDownloadProgress()
        {
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

        private double GetRarity()
        {
            int peerCountWithPiece = torrent.GetPeerCountWithPiece(index);
            int totalPeerCount = torrent.PeerCount;
            return (1.0 - ((double)peerCountWithPiece / (double)totalPeerCount));
        }
        #endregion
    }
}
