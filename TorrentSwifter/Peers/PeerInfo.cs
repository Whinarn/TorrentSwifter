using System;
using System.Net;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Peer information.
    /// </summary>
    public struct PeerInfo
    {
        #region Fields
        private PeerID? id;
        private IPEndPoint endPoint;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the peer ID.
        /// </summary>
        public PeerID? ID
        {
            get { return id; }
        }

        /// <summary>
        /// Gets or sets the peer IP end-point.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return endPoint; }
            set { endPoint = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates new peer information.
        /// </summary>
        /// <param name="endPoint">The peer IP end-point.</param>
        public PeerInfo(IPEndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException("endPoint");

            this.id = null;
            this.endPoint = endPoint;
        }

        /// <summary>
        /// Creates new peer information.
        /// </summary>
        /// <param name="id">The peer ID.</param>
        /// <param name="endPoint">The peer IP end-point.</param>
        public PeerInfo(PeerID id, IPEndPoint endPoint)
        {
            if (id.ID == null)
                throw new ArgumentException("The peer ID is invalid.", "id");
            else if (endPoint == null)
                throw new ArgumentNullException("endPoint");

            this.id = id;
            this.endPoint = endPoint;
        }
        #endregion
    }
}
