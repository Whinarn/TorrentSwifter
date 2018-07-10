using System;
using System.Net;
using System.Threading.Tasks;
using TorrentSwifter.Logging;

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
        /// Occurs when a connection attempt has failed with this peer.
        /// </summary>
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;
        /// <summary>
        /// Occurs when our connection with this peer has been disconnected.
        /// </summary>
        public event EventHandler Disconnected;
        /// <summary>
        /// Occurs when the state of this peer has changed.
        /// </summary>
        public event EventHandler StateChanged;
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
        /// The peer connection has been successfully established.
        /// </summary>
        protected virtual void OnConnected()
        {
            Connected.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The connection attempt to this peer has failed.
        /// </summary>
        /// <param name="failedReason">The reason of failure.</param>
        protected virtual void OnConnectionFailed(ConnectionFailedReason failedReason)
        {
            var eventArgs = new ConnectionFailedEventArgs(failedReason);
            ConnectionFailed.SafeInvoke(this, eventArgs);
        }

        /// <summary>
        /// The peer connection has been disconnected.
        /// </summary>
        protected virtual void OnDisconnected()
        {
            Disconnected.SafeInvoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The state of the peer has been changed.
        /// </summary>
        protected virtual void OnStateChanged()
        {
            StateChanged.SafeInvoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
