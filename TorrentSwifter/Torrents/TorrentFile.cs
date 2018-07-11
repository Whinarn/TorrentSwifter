using System;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A torrent file.
    /// </summary>
    public sealed class TorrentFile
    {
        #region Fields
        private readonly string path;
        private readonly long size;
        private readonly long offset;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the path of this file within the torrent.
        /// </summary>
        public string Path
        {
            get { return path; }
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
        #endregion

        #region Constructor
        internal TorrentFile(string path, long size, long offset)
        {
            this.path = path;
            this.size = size;
            this.offset = offset;
        }
        #endregion
    }
}
