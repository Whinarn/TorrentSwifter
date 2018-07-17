// Specification: http://www.bittorrent.org/beps/bep_0014.html

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    // TODO: Add support for IPv6 multicast
    //       https://docs.microsoft.com/en-us/windows/desktop/winsock/porting-broadcast-applications-to-ipv6

    /// <summary>
    /// The local peer discovery, aka Local Service Discovery (LSD).
    /// </summary>
    internal static class LocalPeerDiscovery
    {
        #region Consts
        private const int BroadcastPort = 6771;

        private const string BroadcastMessageFormat = "BT-SEARCH * HTTP/1.1\r\nHost: 239.192.152.143:6771\r\nPort: {0}\r\nInfohash: {1}\r\ncookie: {2}\r\n\r\n\r\n";
        #endregion

        #region Fields
        private static Socket socket;
        private static readonly IPEndPoint endPoint;
        #endregion

        #region Static Initializer
        static LocalPeerDiscovery()
        {
            endPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
        }
        #endregion

        #region Internal Methods
        internal static void Initialize()
        {
            if (socket != null)
                return;

            socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        }

        internal static void Uninitialize()
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Broadcasts a message.
        /// </summary>
        /// <param name="torrent">The torrent to broadcast for.</param>
        public static void Broadcast(Torrent torrent)
        {
            if (torrent == null)
                throw new ArgumentNullException("torrent");
            else if (torrent.IsPrivate)
                return;
            else if (socket == null)
                throw new InvalidOperationException("The local peer discovery has not yet been initialized.");

            int listenPort = PeerListener.Port;
            string infoHashHex = torrent.InfoHash.ToHexString().ToUpperInvariant();
            string cookie = LocalPeerListener.Cookie;
            string message = string.Format(BroadcastMessageFormat, listenPort, infoHashHex, cookie);
            byte[] messageData = Encoding.ASCII.GetBytes(message);
            try
            {
                socket.SendTo(messageData, 0, messageData.Length, SocketFlags.None, endPoint);
            }
            catch (Exception ex)
            {
                Logging.Log.LogErrorException(ex);
            }
        }
        #endregion
    }
}
