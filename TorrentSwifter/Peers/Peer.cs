using System;
using System.Net;
using System.Threading.Tasks;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer.
    /// </summary>
    public sealed class Peer
    {
        #region Fields
        private readonly Torrent torrent;
        private readonly IPEndPoint endPoint;

        private PeerConnection connection = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the owner torrent for this peer.
        /// </summary>
        public Torrent Torrent
        {
            get { return torrent; }
        }

        /// <summary>
        /// Gets the IP end-point of the peer.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return endPoint; }
        }

        /// <summary>
        /// Gets the peer connection.
        /// </summary>
        public PeerConnection Connection
        {
            get { return connection; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new torrent peer.
        /// </summary>
        /// <param name="torrent">The owner torrent.</param>
        /// <param name="endPoint">The peer IP end-point.</param>
        internal Peer(Torrent torrent, IPEndPoint endPoint)
        {
            this.torrent = torrent;
            this.endPoint = endPoint;

            this.connection = new PeerConnectionTCP(endPoint);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to connect to this peer.
        /// </summary>
        /// <returns>If the connection was successfully established.</returns>
        public async Task<bool> Connect()
        {
            if (connection == null)
                return false;
            else if (connection.IsConnecting || connection.IsConnected)
                return false;

            await connection.ConnectAsync();
            return true;
        }

        /// <summary>
        /// Disconnects from this peer.
        /// </summary>
        public void Disconnect()
        {
            if (!connection.IsConnecting && !connection.IsConnected)
                return;

            connection.Disconnect();
        }

        /// <summary>
        /// Returns the text-representation of this peer.
        /// </summary>
        /// <returns>The peer text-representation.</returns>
        public override string ToString()
        {
            // TODO: Expand this
            return endPoint.ToString();
        }
        #endregion
    }
}
