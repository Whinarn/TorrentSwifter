using System;

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A torrent piece block.
    /// </summary>
    public sealed class TorrentBlock
    {
        #region Fields
        private readonly int index;
        private readonly int size;

        private bool isRequested = false;
        private bool isDownloaded = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the index of this block within the parent piece.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// Gets the size of this block within the parent piece.
        /// </summary>
        public int Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets if this block has been requested.
        /// </summary>
        public bool IsRequested
        {
            get { return isRequested; }
            internal set { isRequested = value && !isDownloaded; }
        }

        /// <summary>
        /// Gets if this block has been downloaded.
        /// </summary>
        public bool IsDownloaded
        {
            get { return isDownloaded; }
            internal set
            {
                isDownloaded = value;
                if (isDownloaded)
                {
                    isRequested = false;
                }
            }
        }
        #endregion

        #region Constructor
        internal TorrentBlock(int index, int size)
        {
            this.index = index;
            this.size = size;
        }
        #endregion
    }
}
