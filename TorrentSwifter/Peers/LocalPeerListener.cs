// Specification: http://www.bittorrent.org/beps/bep_0014.html

// TODO: Add support for IPv6 part of the specification

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// The local peer listener for the Local Service Discovery (LSD).
    /// </summary>
    internal static class LocalPeerListener
    {
        #region Consts
        private const int MaxUDPPacketSize = 65507; // 65535 - 8 (UDP header) - 20 (IP header)
        private const int MulticastPort = 6771;
        #endregion

        #region Fields
        private static Socket socketV4;
        private static Socket socketV6;

        private static byte[] receiveBufferV4 = new byte[MaxUDPPacketSize];
        private static byte[] receiveBufferV6 = new byte[MaxUDPPacketSize];

        private static readonly string cookie;

        private static readonly IPAddress multicastIPAddressV4 = IPAddress.Parse("239.192.152.143");
        private static readonly IPAddress multicastIPAddressV6 = IPAddress.Parse("[ff15::efc0:988f]");

        private static readonly IPEndPoint anyEndPointV4 = new IPEndPoint(IPAddress.Any, 0);
        private static readonly IPEndPoint anyEndPointV6 = new IPEndPoint(IPAddress.IPv6Any, 0);

        private static readonly Regex messageRegex = new Regex("BT-SEARCH \\* HTTP/1.1\\r\\nHost: 239.192.152.143:6771\\r\\nPort: (?<port>[0-9]{1,5})\\r\\nInfohash: (?<hash>[0-9a-fA-F]{40})\\r\\n([Cc]ookie: (?<cookie>[^\\r\\n]+)\\r\\n)?\\r\\n\\r\\n");
        #endregion

        #region Properties
        /// <summary>
        /// Gets the cookie used to ignore ourselves during the local peer discovery.
        /// </summary>
        public static string Cookie
        {
            get { return cookie; }
        }
        #endregion

        #region Static Initializer
        static LocalPeerListener()
        {
            var guid = Guid.NewGuid();
            var guidBytes = guid.ToByteArray();
            cookie = HexHelper.BytesToHex(guidBytes, 0, 8);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts listening for local peers.
        /// </summary>
        public static void StartListening()
        {
            if (Socket.OSSupportsIPv4 && socketV4 == null)
            {
                socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socketV4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                var localEndPoint = new IPEndPoint(IPAddress.Any, MulticastPort);
                socketV4.Bind(localEndPoint);

                var multicastOption = new MulticastOption(multicastIPAddressV4);
                socketV4.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);

                StartReceivingDataV4();
            }

            if (Socket.OSSupportsIPv6 && socketV6 == null)
            {
                socketV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                socketV6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                var localEndPoint = new IPEndPoint(IPAddress.IPv6Any, MulticastPort);
                socketV6.Bind(localEndPoint);

                var multicastOption = new IPv6MulticastOption(multicastIPAddressV6);
                socketV6.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, multicastOption);

                StartReceivingDataV6();
            }
        }

        /// <summary>
        /// Stops listening for local peers.
        /// </summary>
        public static void StopListening()
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

        #region Private Methods
        private static void StartReceivingDataV4()
        {
            if (socketV4 == null)
                return;

            try
            {
                EndPoint endPoint = anyEndPointV4;
                socketV4?.BeginReceiveFrom(receiveBufferV4, 0, MaxUDPPacketSize, SocketFlags.None, ref endPoint, OnReceivedDataV4, socketV4);
            }
            catch (ObjectDisposedException)
            {
                // We can ignore this exception, because this means that we are closing down.
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);

                if (socketV4 != null)
                {
                    socketV4.Close();
                    socketV4 = null;
                }
            }
        }

        private static void StartReceivingDataV6()
        {
            if (socketV6 == null)
                return;

            try
            {
                EndPoint endPoint = anyEndPointV6;
                socketV6?.BeginReceiveFrom(receiveBufferV6, 0, MaxUDPPacketSize, SocketFlags.None, ref endPoint, OnReceivedDataV6, socketV6);
            }
            catch (ObjectDisposedException)
            {
                // We can ignore this exception, because this means that we are closing down.
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);

                if (socketV6 != null)
                {
                    socketV6.Close();
                    socketV6 = null;
                }
            }
        }

        private static void OnReceivedDataV4(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            if (socket == null)
                return;

            try
            {
                EndPoint endPoint = anyEndPointV4;
                int receivedByteCount = socket.EndReceiveFrom(ar, ref endPoint);
                if (receivedByteCount <= 0)
                    return;

                var ipEndPoint = endPoint as IPEndPoint;
                if (ipEndPoint == null)
                    return;

                string receivedText = Encoding.ASCII.GetString(receiveBufferV4, 0, receivedByteCount);
                if (!HandleBroadcast(receivedText, ipEndPoint))
                    return;

                // TODO: Broadcast if we didn't just recently broadcast for this torrent
            }
            catch (ObjectDisposedException)
            {
                // We can ignore this exception, because this means that we are closing down.
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
            }
            finally
            {
                StartReceivingDataV4();
            }
        }

        private static void OnReceivedDataV6(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            if (socket == null)
                return;

            try
            {
                EndPoint endPoint = anyEndPointV6;
                int receivedByteCount = socket.EndReceiveFrom(ar, ref endPoint);
                if (receivedByteCount <= 0)
                    return;

                var ipEndPoint = endPoint as IPEndPoint;
                if (ipEndPoint == null)
                    return;

                string receivedText = Encoding.ASCII.GetString(receiveBufferV6, 0, receivedByteCount);
                if (!HandleBroadcast(receivedText, ipEndPoint))
                    return;

                // TODO: Broadcast if we didn't just recently broadcast for this torrent
            }
            catch (ObjectDisposedException)
            {
                // We can ignore this exception, because this means that we are closing down.
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
            }
            finally
            {
                StartReceivingDataV6();
            }
        }

        private static bool HandleBroadcast(string broadcastMessage, IPEndPoint endPoint)
        {
            var match = messageRegex.Match(broadcastMessage);
            if (!match.Success)
                return false;

            int port;
            string portText = match.Groups["port"].Value;
            if (!int.TryParse(portText, out port) || port <= 0 || port > ushort.MaxValue)
                return false;

            string infoHashHex = match.Groups["hash"].Value;
            if (infoHashHex.Length != 40)
                return false;

            var cookieGroup = match.Groups["cookie"];
            if (cookieGroup.Success)
            {
                string cookieText = cookieGroup.Value;
                if (string.Equals(cookieText, cookie)) // This was sent by ourselves
                    return false;
            }

            var infoHash = new InfoHash(infoHashHex);
            var torrent = TorrentRegistry.FindTorrentByInfoHash(infoHash);
            if (torrent == null || torrent.IsPrivate)
                return false;

            var peerEndPoint = new IPEndPoint(endPoint.Address, port);
            var peerInfo = new PeerInfo(peerEndPoint);
            torrent.AddPeer(peerInfo);
            return true;
        }
        #endregion
    }
}
