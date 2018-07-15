using System;
using System.Collections.Generic;
using TorrentSwifter.Peers;

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

        private volatile bool isRequested = false;
        private volatile bool isDownloaded = false;
        private volatile bool hasWrittenToDisk = false;

        private DateTime lastRequestTime = DateTime.MinValue;

        private List<Peer> requestedToPeers = new List<Peer>();
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
        /// Gets if this block is currently being requested.
        /// </summary>
        public bool IsRequested
        {
            get { return isRequested; }
        }

        /// <summary>
        /// Gets the time for the last request of this block.
        /// </summary>
        public DateTime LastRequestTime
        {
            get { return lastRequestTime; }
        }

        /// <summary>
        /// Gets the age of the last request for this block in milliseconds.
        /// </summary>
        public int LastRequestAge
        {
            get
            {
                if (!isRequested)
                    return 0;

                int age = (int)DateTime.UtcNow.Subtract(lastRequestTime).TotalMilliseconds;
                return (age > 0 ? age : 0);
            }
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
                else
                {
                    hasWrittenToDisk = false;
                }
            }
        }

        /// <summary>
        /// Gets if this block has been written to disk yet.
        /// </summary>
        public bool HasWrittenToDisk
        {
            get { return hasWrittenToDisk; }
            internal set { hasWrittenToDisk = value; }
        }
        #endregion

        #region Constructor
        internal TorrentBlock(int index, int size)
        {
            this.index = index;
            this.size = size;
        }
        #endregion

        #region Internal Methods
        internal void AddRequestPeer(Peer peer)
        {
            lock (requestedToPeers)
            {
                if (!requestedToPeers.Contains(peer))
                {
                    requestedToPeers.Add(peer);
                }

                isRequested = (requestedToPeers.Count > 0);
                lastRequestTime = DateTime.UtcNow;
            }
        }

        internal void RemoveRequestPeer(Peer peer)
        {
            lock (requestedToPeers)
            {
                requestedToPeers.Remove(peer);
                isRequested = (requestedToPeers.Count > 0);
            }
        }
        #endregion
    }
}
