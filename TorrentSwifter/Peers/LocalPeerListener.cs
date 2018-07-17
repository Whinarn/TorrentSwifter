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
        private static Socket socket;
        private static byte[] receiveBuffer = new byte[MaxUDPPacketSize];

        private static readonly string cookie;

        private static readonly IPAddress multicastIPAddress = IPAddress.Parse("239.192.152.143");
        private static readonly IPEndPoint anyEndPoint = new IPEndPoint(IPAddress.Any, 0);

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
            if (socket != null)
                return;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var localEndPoint = new IPEndPoint(IPAddress.Any, MulticastPort);
            socket.Bind(localEndPoint);

            var multicastOption = new MulticastOption(multicastIPAddress);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);

            StartReceivingData();
        }

        /// <summary>
        /// Stops listening for local peers.
        /// </summary>
        public static void StopListening()
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
        }
        #endregion

        #region Private Methods
        private static void StartReceivingData()
        {
            if (socket == null)
                return;

            try
            {
                EndPoint endPoint = anyEndPoint;
                socket?.BeginReceiveFrom(receiveBuffer, 0, MaxUDPPacketSize, SocketFlags.None, ref endPoint, OnReceivedData, socket);
            }
            catch (ObjectDisposedException)
            {
                // We can ignore this exception, because this means that we are closing down.
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
                StopListening();
            }
        }

        private static void OnReceivedData(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            if (socket == null)
                return;

            try
            {
                EndPoint endPoint = anyEndPoint;
                int receivedByteCount = socket.EndReceiveFrom(ar, ref endPoint);
                if (receivedByteCount <= 0)
                    return;

                var ipEndPoint = endPoint as IPEndPoint;
                if (ipEndPoint == null)
                    return;

                string receivedText = Encoding.ASCII.GetString(receiveBuffer, 0, receivedByteCount);
                var match = messageRegex.Match(receivedText);
                if (!match.Success)
                    return;

                int port;
                string portText = match.Groups["port"].Value;
                if (!int.TryParse(portText, out port) || port <= 0 || port > ushort.MaxValue)
                    return;

                string infoHashHex = match.Groups["hash"].Value;
                if (infoHashHex.Length != 40)
                    return;

                var cookieGroup = match.Groups["cookie"];
                if (cookieGroup.Success)
                {
                    string cookieText = cookieGroup.Value;
                    if (string.Equals(cookieText, cookie)) // This was sent by ourselves
                        return;
                }

                var infoHash = new InfoHash(infoHashHex);
                var torrent = TorrentRegistry.FindTorrentByInfoHash(infoHash);
                if (torrent == null || torrent.IsPrivate)
                    return;

                var peerEndPoint = new IPEndPoint(ipEndPoint.Address, port);
                var peerInfo = new PeerInfo(peerEndPoint);
                torrent.AddPeer(peerInfo);

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
                StartReceivingData();
            }
        }
        #endregion
    }
}
