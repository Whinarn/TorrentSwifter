﻿using System;
using System.IO;
using TorrentSwifter.Helpers;

namespace TorrentSwifter
{
    /// <summary>
    /// A torrent.
    /// </summary>
    public sealed class Torrent
    {
        #region Consts
        private const int MinBlockSize = 1 * (int)SizeHelper.KiloByte;
        private const int MaxBlockSize = 16 * (int)SizeHelper.KiloByte;
        private const int DefaultBlockSize = 16 * (int)SizeHelper.KiloByte;
        #endregion

        #region Classes
        private class Piece
        {
            private readonly int index;
            private readonly int size;

            private bool isVerified = false;
            private bool[] downloadedBlocks = null;

            public int Size
            {
                get { return size; }
            }

            public bool IsVerified
            {
                get { return isVerified; }
                set
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

            public Piece(int index, int size, int blockCount)
            {
                this.index = index;
                this.size = size;

                downloadedBlocks = new bool[blockCount];
            }

            public bool HasDownloadedAllBlocks()
            {
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
        }
        #endregion

        #region Fields
        private readonly TorrentMetaData metaData;
        private readonly string downloadPath;
        private readonly int blockSize;
        private readonly long totalSize;

        private Piece[] pieces = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the torrent meta-data information.
        /// </summary>
        public TorrentMetaData MetaData
        {
            get { return metaData; }
        }

        /// <summary>
        /// Gets the path to download to.
        /// </summary>
        public string DownloadPath
        {
            get { return downloadPath; }
        }

        /// <summary>
        /// Gets the size of each piece.
        /// </summary>
        public int PieceSize
        {
            get { return metaData.PieceSize; }
        }

        /// <summary>
        /// Gets the size of each block.
        /// </summary>
        public int BlockSize
        {
            get { return blockSize; }
        }

        /// <summary>
        /// Gets the total size of this torrent.
        /// </summary>
        public long TotalSize
        {
            get { return totalSize; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new torrent.
        /// </summary>
        /// <param name="metaData">The torrent meta-data information.</param>
        /// <param name="downloadPath">The download path for the torrent.</param>
        /// <param name="blockSize">The desired block size.</param>
        public Torrent(TorrentMetaData metaData, string downloadPath, int blockSize = DefaultBlockSize)
        {
            if (metaData == null)
                throw new ArgumentNullException("metaData");
            else if (blockSize < MinBlockSize)
                throw new ArgumentOutOfRangeException("blockSize", string.Format("The block size must be above or equals to {0} bytes ({1}).", MinBlockSize, SizeHelper.GetHumanReadableSize(MinBlockSize)));
            else if (blockSize > MaxBlockSize)
                throw new ArgumentOutOfRangeException("blockSize", string.Format("The block size must be below or equals to {0} bytes ({1}).", MaxBlockSize, SizeHelper.GetHumanReadableSize(MaxBlockSize)));
            else if (!MathHelper.IsPowerOfTwo(blockSize))
                throw new ArgumentException("The block size must be a power of two.", "blockSize");

            this.metaData = metaData;
            this.downloadPath = Path.GetFullPath(downloadPath);
            this.blockSize = blockSize;
            this.totalSize = metaData.TotalSize;

            InitializePieces();
        }
        #endregion

        #region Private Methods
        private void InitializePieces()
        {
            long totalSize = metaData.TotalSize;
            int pieceSize = metaData.PieceSize;
            int pieceCount = metaData.PieceCount;
            pieces = new Piece[pieceCount];

            int lastPieceSize = (int)(totalSize % pieceSize);
            if (lastPieceSize == 0)
                lastPieceSize = pieceSize;

            for (int i = 0; i < pieceCount; i++)
            {
                bool isLastPiece = (i == (pieceCount - 1));
                int currentPieceSize = (isLastPiece ? lastPieceSize : pieceSize);
                int blockCount = (((currentPieceSize - 1) / blockSize) + 1);
                pieces[i] = new Piece(i, currentPieceSize, blockCount);
            }
        }
        #endregion
    }
}
