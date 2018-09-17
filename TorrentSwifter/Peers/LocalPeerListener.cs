#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

// Specification: http://www.bittorrent.org/beps/bep_0014.html

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TorrentSwifter.Collections;
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

        private const string MessageRequestLine = "BT-SEARCH * HTTP/1.1";
        #endregion

        #region Fields
        private static Socket socketV4;
        private static Socket socketV6;

        private static byte[] receiveBufferV4 = new byte[MaxUDPPacketSize];
        private static byte[] receiveBufferV6 = new byte[MaxUDPPacketSize];

        private static readonly string cookie;

        private static readonly IPAddress multicastIPAddressV4 = IPAddress.Parse("239.192.152.143");
        private static readonly IPAddress multicastIPAddressV6 = IPAddress.Parse("[ff15::efc0:988f]");
        private static readonly string multicastEndpointV4 = string.Format("{0}:{1}", multicastIPAddressV4, MulticastPort);
        private static readonly string multicastEndpointV6 = string.Format("[{0}]:{1}", multicastIPAddressV6, MulticastPort);

        private static readonly IPEndPoint anyEndPointV4 = new IPEndPoint(IPAddress.Any, 0);
        private static readonly IPEndPoint anyEndPointV6 = new IPEndPoint(IPAddress.IPv6Any, 0);
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
            cookie = HexHelper.BytesToHex(guidBytes, 0, 4);
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
                socketV4.EnableBroadcast = true;
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
            HeaderCollection headers;
            if (!TryParseMessage(broadcastMessage, out headers))
                return false;

            string hostText;
            if (!headers.TryGetString("Host", out hostText))
                return false;

            string expectedHostText = (endPoint.AddressFamily == AddressFamily.InterNetwork ? multicastEndpointV4 : multicastEndpointV6);
            if (!string.Equals(hostText, expectedHostText))
                return false;

            int port;
            string portText;
            if (!headers.TryGetString("Port", out portText) || !int.TryParse(portText, out port) || port <= 0 || port > ushort.MaxValue)
                return false;

            string[] infoHashesHex;
            if (!headers.TryGetStrings("Infohash", out infoHashesHex) || infoHashesHex.Length == 0)
                return false;

            // Parse all info hashes
            InfoHash[] infoHashes;
            try
            {
                infoHashes = new InfoHash[infoHashesHex.Length];
                for (int i = 0; i < infoHashesHex.Length; i++)
                {
                    if (infoHashesHex[i].Length != 40)
                        return false;

                    infoHashes[i] = new InfoHash(infoHashesHex[i]);
                }
            }
            catch
            {
                return false;
            }

            // Check if we are the one who sent this
            string cookieText;
            if (headers.TryGetString("Cookie", out cookieText) && string.Equals(cookieText, cookie))
                return false;

            Log.LogInfo("[LocalPeerDiscovery][{0}] Received a local broadcast for {1} torrents.", endPoint.Address, infoHashes.Length);

            var peerEndPoint = new IPEndPoint(endPoint.Address, port);
            for (int i = 0; i < infoHashes.Length; i++)
            {
                var infoHash = infoHashes[i];
                var torrent = TorrentRegistry.FindTorrentByInfoHash(infoHash);
                if (torrent == null || torrent.IsPrivate)
                    continue;

                var peerInfo = new PeerInfo(peerEndPoint);
                torrent.AddPeer(peerInfo);
            }
            return true;
        }

        private static bool TryParseMessage(string broadcastMessage, out HeaderCollection headers)
        {
            headers = null;
            if (string.IsNullOrEmpty(broadcastMessage))
                return false;

            string[] lines = broadcastMessage.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length < 7)
            {
                return false;
            }
            else if (!string.Equals(lines[0], MessageRequestLine) || lines[lines.Length - 3].Length != 0 ||
                     lines[lines.Length - 2].Length != 0 || lines[lines.Length - 1].Length != 0)
            {
                return false;
            }

            headers = new HeaderCollection();
            for (int i = 1; i < lines.Length - 3; i++)
            {
                string line = lines[i];
                int colonIndex = line.IndexOf(':');
                if (colonIndex == -1 || colonIndex == (line.Length - 1) || line[colonIndex + 1] != ' ')
                    return false;

                string headerName = line.Substring(0, colonIndex);
                string headerValue = line.Substring(colonIndex + 2);
                headers.Add(headerName, headerValue);
            }
            return true;
        }
        #endregion
    }
}
