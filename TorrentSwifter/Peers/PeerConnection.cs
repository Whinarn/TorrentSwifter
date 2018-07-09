using System;
using System.Net;
using System.Threading.Tasks;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer connection.
    /// </summary>
    public abstract class PeerConnection : IDisposable
    {
        #region Fields
        /// <summary>
        /// The peer IP end-point.
        /// </summary>
        protected readonly IPEndPoint endPoint;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the peer IP end-point.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return endPoint; }
        }

        /// <summary>
        /// Gets if we are currently attempting to connect.
        /// </summary>
        public abstract bool IsConnecting
        {
            get;
        }

        /// <summary>
        /// Gets if this connection has been established.
        /// </summary>
        public abstract bool IsConnected
        {
            get;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a connection has been established with this peer.
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Occurs when our connection with this peer has been disconnected.
        /// </summary>
        public event EventHandler Disconnected;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new torrent peer connection.
        /// </summary>
        /// <param name="endPoint">The peer IP end-point.</param>
        public PeerConnection(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
        #endregion

        #region Finalizer
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~PeerConnection()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Disposes of this connection.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Connects to this peer synchronously.
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Connects to this peer asynchronously.
        /// </summary>
        /// <returns></returns>
        public abstract Task ConnectAsync();

        /// <summary>
        /// Disconnects from this peer.
        /// </summary>
        public abstract void Disconnect();
        #endregion

        #region Protected Methods
        /// <summary>
        /// Disposes of this peer connection.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Invoke this method when the peer connection has been successfully established.
        /// </summary>
        protected void OnConnected()
        {
            var eventHandler = this.Connected;
            if (eventHandler != null)
            {
                eventHandler.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoke this method when the peer connection has been disconnected.
        /// </summary>
        protected void OnDisconnected()
        {
            var eventHandler = this.Disconnected;
            if (eventHandler != null)
            {
                eventHandler.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion
    }
}
