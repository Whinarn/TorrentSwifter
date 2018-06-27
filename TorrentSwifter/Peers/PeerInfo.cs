using System;
using System.Net;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Peer information.
    /// </summary>
    public struct PeerInfo : IEquatable<PeerInfo>
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

        #region Public Methods
        /// <summary>
        /// Returns if this peer information equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>If equals.</returns>
        public override bool Equals(object obj)
        {
            if (obj is PeerInfo)
                return Equals((PeerInfo)obj);
            else
                return false;
        }

        /// <summary>
        /// Returns if this peer information equals another peer information.
        /// </summary>
        /// <param name="other">The other peer information.</param>
        /// <returns>If equals.</returns>
        public bool Equals(PeerInfo other)
        {
            if (endPoint == other.endPoint)
                return true;
            else if (endPoint == null)
                return false;
            else if (other.endPoint == null)
                return false;

            return endPoint.Equals(other.endPoint);
        }

        /// <summary>
        /// Returns the hash code for the peer information.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return endPoint.GetHashCode();
        }

        /// <summary>
        /// Returns the text-representation of the peer information.
        /// </summary>
        /// <returns>The text-representation.</returns>
        public override string ToString()
        {
            if (id.HasValue)
            {
                return string.Format("{0} ({1})", endPoint, id.Value);
            }
            else
            {
                return endPoint.ToString();
            }
        }
        #endregion
    }
}
