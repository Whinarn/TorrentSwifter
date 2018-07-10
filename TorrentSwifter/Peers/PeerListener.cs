using System;
using System.Net.Sockets;
using TorrentSwifter.Logging;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// The peer connection listener.
    /// </summary>
    public static class PeerListener
    {
        #region Fields
        private static TcpListener listener = null;
        private static int listenPort = 0;
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

            PeerManager.Initialize();
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

            PeerManager.Uninitialize();
        }
        #endregion

        #region Private Methods
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
                    PeerManager.AddPendingConnection(peerConnection);
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
    }
}
