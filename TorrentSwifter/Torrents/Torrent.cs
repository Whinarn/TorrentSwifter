using System;
using System.IO;
using TorrentSwifter.Helpers;

namespace TorrentSwifter.Torrents
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

        #region Delegates
        /// <summary>
        /// An event handler for pieces.
        /// </summary>
        /// <param name="torrent">The torrent.</param>
        /// <param name="pieceIndex">The piece index.</param>
        public delegate void PieceEventHandler(Torrent torrent, int pieceIndex);
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

        private class TorrentFile
        {
            private readonly string path;
            private readonly long size;
            private readonly long offset;

            public string Path
            {
                get { return path; }
            }

            public long Size
            {
                get { return size; }
            }

            public long Offset
            {
                get { return offset; }
            }

            public long EndOffset
            {
                get { return (offset + size); }
            }

            public TorrentFile(string path, long size, long offset)
            {
                this.path = path;
                this.size = size;
                this.offset = offset;
            }
        }
        #endregion

        #region Fields
        private readonly TorrentMetaData metaData;
        private readonly InfoHash infoHash;
        private readonly PeerID peerID;
        private readonly string downloadPath;
        private readonly int blockSize;
        private readonly long totalSize;

        private Piece[] pieces = null;
        private TorrentFile[] files = null;
        private object[] fileWriteLocks = null;
        #endregion

        #region Events
        /// <summary>
        /// An event that occurs once a piece has been downloaded and verified.
        /// </summary>
        public event PieceEventHandler PieceVerified;
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
        /// Gets the info hash of this torrent.
        /// </summary>
        public InfoHash InfoHash
        {
            get { return infoHash; }
        }

        /// <summary>
        /// Gets the peer ID for us with this torrent.
        /// </summary>
        public PeerID PeerID
        {
            get { return peerID; }
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
            this.infoHash = metaData.InfoHash;
            this.peerID = PeerManager.GetPeerID();
            this.downloadPath = Path.GetFullPath(downloadPath);
            this.blockSize = blockSize;
            this.totalSize = metaData.TotalSize;

            InitializePieces();
            InitializeFiles();

            // TODO: Initialize cached data, like integrity etc?
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

        private void InitializeFiles()
        {
            var metaDataFiles = metaData.Files;
            files = new TorrentFile[metaDataFiles.Length];
            fileWriteLocks = new object[files.Length];

            for (int i = 0; i < fileWriteLocks.Length; i++)
            {
                fileWriteLocks[i] = new object();
            }

            if (metaDataFiles.Length == 1)
            {
                var file = metaDataFiles[0];
                files[0] = new TorrentFile(downloadPath, file.Size, 0L);

                if (Preferences.Torrent.AllocateFullFileSizes)
                {
                    IOHelper.CreateAllocatedFile(downloadPath, file.Size);
                }
                else
                {
                    IOHelper.CreateEmptyFile(downloadPath);
                }
            }
            else if (metaDataFiles.Length > 1)
            {
                long currentFileOffset = 0L;

                for (int i = 0; i < metaDataFiles.Length; i++)
                {
                    var metaDataFile = metaDataFiles[i];
                    string filePath = IOHelper.GetTorrentFilePath(downloadPath, metaDataFile);
                    files[i] = new TorrentFile(filePath, metaDataFile.Size, currentFileOffset);
                    currentFileOffset += metaDataFile.Size;

                    if (Preferences.Torrent.AllocateFullFileSizes)
                    {
                        IOHelper.CreateAllocatedFile(filePath, metaDataFile.Size);
                    }
                    else
                    {
                        IOHelper.CreateEmptyFile(filePath);
                    }
                }
            }
        }

        private void VerifyPiece(int pieceIndex)
        {
            var piece = pieces[pieceIndex];
            var pieceHash = metaData.PieceHashes[pieceIndex];
            byte[] computedPieceHash = GetPieceHash(pieceIndex);

            bool isVerified = (computedPieceHash != null && pieceHash.Equals(computedPieceHash));
            if (piece.IsVerified != isVerified)
            {
                piece.IsVerified = isVerified;

                if (isVerified)
                {
                    // TODO: Check if we have downloaded and verified all pieces

                    var verifiedHandler = PieceVerified;
                    if (verifiedHandler != null)
                    {
                        verifiedHandler.Invoke(this, pieceIndex);
                    }
                }
            }
        }

        private byte[] ReadPiece(int pieceIndex)
        {
            var piece = pieces[pieceIndex];
            byte[] pieceData = new byte[piece.Size];
            long torrentOffset = ((long)pieceIndex * (long)PieceSize);
            int readByteCount = ReadData(torrentOffset, pieceData, 0, pieceData.Length);
            if (readByteCount != pieceData.Length)
                return null;

            return pieceData;
        }

        private byte[] GetPieceHash(int pieceIndex)
        {
            byte[] pieceData = ReadPiece(pieceIndex);
            return HashHelper.ComputeSHA1(pieceData);
        }
        #endregion

        #region Internal Methods
        internal int ReadData(long torrentOffset, byte[] buffer, int bufferOffset, int count)
        {
            if (torrentOffset < 0 || torrentOffset >= totalSize)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (count < 0 || (bufferOffset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");

            int readByteCount = 0;
            for (int i = 0; i < files.Length && count > 0; i++)
            {
                var file = files[i];
                if (torrentOffset >= file.Offset && torrentOffset < file.EndOffset)
                {
                    long localOffset = torrentOffset - file.Offset;
                    int localCount = (int)Math.Min(file.Size - localOffset, count);

                    if (!File.Exists(file.Path))
                        break;

                    using (var fileStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fileStream.Seek(localOffset, SeekOrigin.Begin);
                        localCount = fileStream.Read(buffer, bufferOffset, localCount);
                        if (localCount <= 0)
                            break;
                    }

                    torrentOffset += localCount;
                    bufferOffset += localCount;
                    count -= localCount;
                    readByteCount += localCount;
                }
            }
            return readByteCount;
        }

        internal void WriteData(long torrentOffset, byte[] buffer, int bufferOffset, int count)
        {
            if (torrentOffset < 0 || torrentOffset >= totalSize)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (count < 0 || (bufferOffset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");

            for (int i = 0; i < files.Length && count > 0; i++)
            {
                var file = files[i];
                if (torrentOffset >= file.Offset && torrentOffset < file.EndOffset)
                {
                    long localOffset = torrentOffset - file.Offset;
                    int localCount = (int)Math.Min(file.Size - localOffset, count);

                    IOHelper.CreateParentDirectoryIfItDoesntExist(file.Path);

                    lock (fileWriteLocks[i])
                    {
                        using (var fileStream = new FileStream(file.Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            fileStream.Seek(localOffset, SeekOrigin.Begin);
                            fileStream.Write(buffer, bufferOffset, localCount);
                        }
                    }

                    torrentOffset += localCount;
                    bufferOffset += localCount;
                    count -= localCount;
                }
            }
        }
        #endregion
    }
}
