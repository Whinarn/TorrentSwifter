using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using TorrentSwifter.Logging;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// The peer connection listener.
    /// </summary>
    public static class PeerListener
    {
        #region Consts
        private const int PeerHandshakeTimeout = 60000; // 1 minute
        #endregion

        #region Fields
        private static TcpListener listener = null;
        private static int listenPort = 0;

        private static List<PeerConnection> pendingConnections = new List<PeerConnection>();
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if we are currently listening for peer connections.
        /// </summary>
        public static bool IsListening
        {
            get { return (listener != null); }
        }

        /// <summary>
        /// Gets the port that we are currently listening to.
        /// Note that the port will be zero when we are not listening for connections.
        /// </summary>
        public static int Port
        {
            get { return listenPort; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts listening to peer connections.
        /// </summary>
        public static void StartListening()
        {
            if (listener != null)
                return;

            int listenPort = Preferences.Peer.ListenPort;
            listener = TcpListener.Create(listenPort);
            listener.Start();
            PeerListener.listenPort = listenPort;

            ListenForNextConnection();
        }

        /// <summary>
        /// Stops listening to peer connections.
        /// </summary>
        public static void StopListening()
        {
            if (listener == null)
                return;

            listenPort = 0;
            listener.Stop();
            listener = null;

            DisconnectAll();
        }
        #endregion

        #region Private Methods
        #region Listen & Accept Socket
        private static void ListenForNextConnection()
        {
            if (listener == null)
                return;

            try
            {
                listener?.BeginAcceptSocket(OnAcceptSocket, listener);
            }
            catch
            {
                StopListening();
            }
        }

        private static void OnAcceptSocket(IAsyncResult ar)
        {
            var listener = ar.AsyncState as TcpListener;
            if (listener == null)
                return;

            try
            {
                var socket = listener.EndAcceptSocket(ar);
                if (socket != null)
                {
                    var peerConnection = new PeerConnectionTCP(socket);
                    lock (pendingConnections)
                    {
                        pendingConnections.Add(peerConnection);
                    }
                    RegisterConnectionEvents(peerConnection);
                    TimeoutConnection(peerConnection, PeerHandshakeTimeout);
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
            }
            finally
            {
                ListenForNextConnection();
            }
        }
        #endregion

        #region Setup Connection
        private static void RegisterConnectionEvents(PeerConnection connection)
        {
            connection.Disconnected += OnConnectionDisconnected;
            connection.Handshaked += OnConnectionHandshaked;
        }

        private static void UnregisterConnectionEvents(PeerConnection connection)
        {
            connection.Disconnected -= OnConnectionDisconnected;
            connection.Handshaked -= OnConnectionHandshaked;
        }
        #endregion

        #region Disconnect All
        private static void DisconnectAll()
        {
            lock (pendingConnections)
            {
                foreach (var connection in pendingConnections)
                {
                    connection.Disconnect();
                }
                pendingConnections.Clear();
            }
        }
        #endregion

        #region Timeout Connection
        private static void TimeoutConnection(PeerConnection connection, int handshakeTimeout)
        {
            Task.Run(async () =>
            {
                await Task.Delay(handshakeTimeout);

                bool isStillPending;
                lock (pendingConnections)
                {
                    isStillPending = pendingConnections.Contains(connection);
                }

                // Abort if the connection is not still pending
                if (!isStillPending)
                    return;

                // If the connection hasn't been handshaked but still is connected, then we disconnect it
                if (connection.IsConnected && !connection.IsHandshaked)
                {
                    connection.Disconnect();
                }
            });
        }
        #endregion

        #region Connection Events
        private static void OnConnectionDisconnected(object sender, EventArgs e)
        {
            var connection = sender as PeerConnection;
            if (connection == null)
                return;

            UnregisterConnectionEvents(connection);
            lock (pendingConnections)
            {
                pendingConnections.Remove(connection);
            }

            connection.Dispose();
        }

        private static void OnConnectionHandshaked(object sender, EventArgs e)
        {
            var connection = sender as PeerConnection;
            if (connection == null)
                return;

            UnregisterConnectionEvents(connection);
            lock (pendingConnections)
            {
                pendingConnections.Remove(connection);
            }

            var peerID = connection.PeerID;
            var torrent = connection.Torrent;
            if (torrent == null || peerID.Equals(PeerID.None))
            {
                // If there is no torrent attached or the peer has no ID then we simply close it now
                connection.Dispose();
                return;
            }

            torrent.OnPeerConnected(peerID, connection);
        }
        #endregion
        #endregion
    }
}
