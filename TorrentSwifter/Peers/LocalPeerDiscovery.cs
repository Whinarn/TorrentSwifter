// Specification: http://www.bittorrent.org/beps/bep_0014.html

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// The local peer discovery, aka Local Service Discovery (LSD).
    /// </summary>
    internal static class LocalPeerDiscovery
    {
        #region Consts
        private const int MulticastPort = 6771;

        private const string BroadcastMessageFormat = "BT-SEARCH * HTTP/1.1\r\nHost: {0}:6771\r\nPort: {1}\r\nInfohash: {2}\r\ncookie: {3}\r\n\r\n\r\n";
        #endregion

        #region Fields
        private static Socket socketV4;
        private static Socket socketV6;

        private static readonly IPAddress multicastIPAddressV4 = IPAddress.Parse("239.192.152.143");
        private static readonly IPAddress multicastIPAddressV6 = IPAddress.Parse("[ff15::efc0:988f]");
        private static readonly IPEndPoint endPointV4 = new IPEndPoint(IPAddress.Broadcast, MulticastPort);
        private static readonly IPEndPoint endPointV6 = new IPEndPoint(multicastIPAddressV6, MulticastPort);
        #endregion

        #region Internal Methods
        internal static void Initialize()
        {
            if (Socket.OSSupportsIPv4 && socketV4 == null)
            {
                socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketV4.EnableBroadcast = true;
                socketV4.MulticastLoopback = false;

                var multicastOptionV4 = new MulticastOption(multicastIPAddressV4);
                socketV4.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOptionV4);
            }

            if (Socket.OSSupportsIPv6 && socketV6 == null)
            {
                socketV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                socketV6.MulticastLoopback = false;

                var multicastOptionV6 = new IPv6MulticastOption(multicastIPAddressV6);
                socketV6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, multicastOptionV6);
            }
        }

        internal static void Uninitialize()
        {
            if (socketV4 != null)
            {
                socketV4.Close();
                socketV4 = null;
            }

            if (socketV6 != null)
            {
                socketV6.Close();
                socketV6 = null;
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
            else if (socketV4 == null && socketV6 == null)
                throw new InvalidOperationException("The local peer discovery has not yet been initialized.");

            int listenPort = PeerListener.Port;
            string infoHashHex = torrent.InfoHash.ToHexString().ToUpperInvariant();
            string cookie = LocalPeerListener.Cookie;

            if (socketV4 != null)
            {
                Broadcast(socketV4, endPointV4, multicastIPAddressV4, listenPort, infoHashHex, cookie);
            }

            if (socketV6 != null)
            {
                Broadcast(socketV6, endPointV6, multicastIPAddressV6, listenPort, infoHashHex, cookie);
            }
        }
        #endregion

        #region Private Methods
        private static void Broadcast(Socket socket, IPEndPoint endPoint, IPAddress ipAddress, int listenPort, string infoHashHex, string cookie)
        {
            string ipAddressText = (ipAddress.AddressFamily == AddressFamily.InterNetwork ? ipAddress.ToString() : string.Format("[{0}]", ipAddress));
            string message = string.Format(BroadcastMessageFormat, ipAddressText, listenPort, infoHashHex, cookie);
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
