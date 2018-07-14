using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A torrent file.
    /// </summary>
    public sealed class TorrentFile
    {
        #region Fields
        private readonly string localPath;
        private readonly string fullPath;
        private readonly long size;
        private readonly long offset;

        private SemaphoreSlim semaphore = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the local path of this file within the torrent.
        /// </summary>
        public string LocalPath
        {
            get { return localPath; }
        }

        /// <summary>
        /// Gets the full path of this file at the download path.
        /// </summary>
        public string FullPath
        {
            get { return fullPath; }
        }

        /// <summary>
        /// Gets the size of this file.
        /// </summary>
        public long Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets the offset of this file within the torrent.
        /// </summary>
        public long Offset
        {
            get { return offset; }
        }

        /// <summary>
        /// Gets the offset of the end of this file within the torrent.
        /// </summary>
        public long EndOffset
        {
            get { return (offset + size); }
        }

        /// <summary>
        /// Gets if this file currently exists on disk.
        /// </summary>
        public bool Exists
        {
            get { return File.Exists(fullPath); }
        }
        #endregion

        #region Constructor
        internal TorrentFile(string localPath, string fullPath, long size, long offset)
        {
            this.localPath = localPath;
            this.fullPath = fullPath;
            this.size = size;
            this.offset = offset;

            semaphore = new SemaphoreSlim(1, 1);
        }
        #endregion

        #region Internal Methods
        internal int Read(long fileOffset, byte[] buffer, int offset, int count)
        {
            using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fileOffset > 0)
                {
                    if (fileOffset >= fileStream.Length)
                        return 0;

                    fileStream.Seek(fileOffset, SeekOrigin.Begin);
                }
                return fileStream.Read(buffer, offset, count);
            }
        }

        internal async Task<int> ReadAsync(long fileOffset, byte[] buffer, int offset, int count)
        {
            using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fileOffset > 0)
                {
                    if (fileOffset >= fileStream.Length)
                        return 0;

                    fileStream.Seek(fileOffset, SeekOrigin.Begin);
                }
                return await fileStream.ReadAsync(buffer, offset, count);
            }
        }

        internal void Write(long fileOffset, byte[] buffer, int offset, int count)
        {
            semaphore.Wait();
            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Seek(fileOffset, SeekOrigin.Begin);
                    fileStream.Write(buffer, offset, count);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        internal async Task WriteAsync(long fileOffset, byte[] buffer, int offset, int count)
        {
            await semaphore.WaitAsync();
            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Seek(fileOffset, SeekOrigin.Begin);
                    await fileStream.WriteAsync(buffer, offset, count);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        #endregion
    }
}
