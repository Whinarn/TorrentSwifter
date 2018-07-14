﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TorrentSwifter.Logging;
using TorrentSwifter.Preferences;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// The peer connection listener.
    /// </summary>
    internal static class PeerListener
    {
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

            int listenPort = Prefs.Peer.ListenPort;
            listener = TcpListener.Create(listenPort);
            listener.Start();
            var localEndPoint = (listener.LocalEndpoint as IPEndPoint);
            listenPort = localEndPoint.Port;
            PeerListener.listenPort = listenPort;

            Log.LogInfo("[PeerListener] Now listening for connections on port {0}", listenPort);

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
                    Log.LogDebug("[PeerListener] Accepted connection: {0}", socket.RemoteEndPoint);

                    var peerConnection = new PeerConnectionTCP(socket);
                    lock (pendingConnections)
                    {
                        pendingConnections.Add(peerConnection);
                    }
                    RegisterConnectionEvents(peerConnection);

                    int handshakeTimeout = Prefs.Peer.HandshakeTimeout;
                    if (handshakeTimeout > 0)
                    {
                        TimeoutConnection(peerConnection, handshakeTimeout);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // We can swallow this because it means that we have stopped listening
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
        }
        #endregion
        #endregion
    }
}
