using System;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A torrent piece.
    /// </summary>
    public sealed class TorrentPiece
    {
        #region Fields
        private readonly int index;
        private readonly long offset;
        private readonly int size;

        private bool isVerified = false;
        private bool[] downloadedBlocks = null;
        #endregion

        #region Properties
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
            get { return downloadedBlocks.Length; }
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
                        for (int i = 0; i < downloadedBlocks.Length; i++)
                        {
                            downloadedBlocks[i] = true;
                        }
                    }
                    else if (HasDownloadedAllBlocks())
                    {
                        // Reset the download state of all blocks if we have downloaded all blocks
                        // but the hash was still not correct.
                        for (int i = 0; i < downloadedBlocks.Length; i++)
                        {
                            downloadedBlocks[i] = false;
                        }
                    }
                }
            }
        }
        #endregion

        #region Constructor
        internal TorrentPiece(int index, long offset, int size, int blockCount)
        {
            this.index = index;
            this.offset = offset;
            this.size = size;

            downloadedBlocks = new bool[blockCount];
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns if we have downloaded all blocks in this piece.
        /// Does not guarantee that the integrity is intact.
        /// </summary>
        /// <returns>If all blocks are downloaded.</returns>
        public bool HasDownloadedAllBlocks()
        {
            if (isVerified)
                return true;

            bool result = true;

            for (int i = 0; i < downloadedBlocks.Length; i++)
            {
                if (!downloadedBlocks[i])
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
