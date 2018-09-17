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

// Specification: http://www.bittorrent.org/beps/bep_0015.html

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;
using TorrentSwifter.Network;
using TorrentSwifter.Peers;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// An UDP torrent tracker.
    /// </summary>
    public sealed class UdpTracker : Tracker
    {
        #region Consts
        private const int MaxUDPPacketSize = 65507; // 65535 - 8 (UDP header) - 20 (IP header)
        private const long ProtocolMagic = 0x41727101980L; // This value is based on the specification

        private const int RetryAttempts = 5;
        #endregion

        #region Enums
        private enum TrackerUdpAction : int
        {
            Connect = 0,
            Announce = 1,
            Scrape = 2,
            Error = 3
        }

        [Flags]
        private enum UdpExtensions : ushort
        {
            None = 0x0000,
            Authentication = 0x0001,
            RequestString = 0x0002
        }
        #endregion

        #region Classes
        private class TrackerRequest : Packet
        {
            private readonly TrackerUdpAction action;
            private readonly int transactionID;
            private readonly DateTime date;
            private readonly bool isIPv6;
            private IPEndPoint expectedResponseEP = null;
            private Packet responsePacket = null;

            public TrackerUdpAction Action
            {
                get { return action; }
            }

            public int TransactionID
            {
                get { return transactionID; }
            }

            public DateTime Date
            {
                get { return date; }
            }

            public int AgeInSeconds
            {
                get { return (int)DateTime.Now.Subtract(date).TotalSeconds; }
            }

            public bool IsIPv6
            {
                get { return isIPv6; }
            }

            public IPEndPoint ExpectedResponseEndPoint
            {
                get { return expectedResponseEP; }
            }

            public bool HasResponse
            {
                get { return (responsePacket != null); }
            }

            public Packet ResponsePacket
            {
                get { return responsePacket; }
                set { responsePacket = value; }
            }

            public TrackerRequest(long connectionID, TrackerUdpAction action, int transactionID, int length, bool isIPv6, IPEndPoint expectedResponseEP)
                : base(length)
            {
                this.action = action;
                this.transactionID = transactionID;
                this.isIPv6 = isIPv6;
                this.expectedResponseEP = expectedResponseEP;
                date = DateTime.Now;

                WriteInt64(connectionID);
                WriteInt32((int)action);
                WriteInt32(transactionID);
            }

            public bool IsValidEndpoint(IPEndPoint endPoint)
            {
                if (expectedResponseEP == null)
                    return true;
                else if (endPoint == null)
                    return false;
                else if (!expectedResponseEP.Address.Equals(endPoint.Address))
                    return false;

                // TODO: Do we have to care about the port? Should we care? Is this bad?
                return (expectedResponseEP.Port == endPoint.Port);
            }
        }

        private class TrackerException : Exception
        {
            private readonly TrackerStatus status;

            public TrackerStatus Status
            {
                get { return status; }
            }

            public TrackerException(TrackerStatus status, string message)
                : base(message)
            {
                this.status = status;
            }
        }
        #endregion

        #region Fields
        private Socket socket = null;
        private bool isListening = false;
        private readonly int key;
        private readonly string authUser;
        private readonly byte[] authPassHash;
        private readonly string requestString;

        private bool isConnecting = false;
        private bool isConnected = false;
        private bool isConnectedV4 = false;
        private bool isConnectedV6 = false;
        private long connectionIDv4 = 0L;
        private long connectionIDv6 = 0L;
        private IPEndPoint trackerEndpointV4 = null;
        private IPEndPoint trackerEndpointV6 = null;

        private byte[] receiveBuffer = new byte[MaxUDPPacketSize];

        private Dictionary<int, TrackerRequest> pendingRequests = new Dictionary<int, TrackerRequest>();
        private List<CancellationTokenSource> cancellationTokens = new List<CancellationTokenSource>();

        private static readonly IPEndPoint anyEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
        #endregion

        #region Properties
        /// <summary>
        /// Gets if we can announce to this tracker.
        /// </summary>
        public override bool CanAnnounce
        {
            get { return true; }
        }

        /// <summary>
        /// Gets if this tracker can be scraped.
        /// </summary>
        public override bool CanScrape
        {
            get { return true; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new UDP tracker.
        /// </summary>
        /// <param name="uri">The tracker URI.</param>
        public UdpTracker(Uri uri)
            : base(uri)
        {
            if (!string.Equals(uri.Scheme, "udp", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The URI-scheme is not UDP.", "uri");

            authUser = GetAuthUserName(uri);
            authPassHash = GetAuthPasswordHash(uri);
            requestString = GetRequestString(uri);

            socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(anyEndPoint);

            isListening = true;
            StartReceivingPackets();

            key = DateTime.Now.GetHashCode() ^ RandomHelper.Next();
        }
        #endregion

        #region Disposing
        /// <summary>
        /// Called when this tracker is being disposed of.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected override void Dispose(bool disposing)
        {
            isListening = false;
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
            lock (cancellationTokens)
            {
                foreach (var tokenSource in cancellationTokens)
                {
                    if (!tokenSource.IsCancellationRequested)
                    {
                        tokenSource.Cancel();
                    }
                }
                cancellationTokens.Clear();
            }
        }
        #endregion

        #region Announce & Scrape
        /// <summary>
        /// Makes an announce request to this tracker.
        /// </summary>
        /// <param name="request">The announce request object.</param>
        /// <returns>The announce response.</returns>
        public override async Task<AnnounceResponse> Announce(AnnounceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            try
            {
                failureMessage = null;
                warningMessage = null;
                if (!await WaitForConnection().ConfigureAwait(false))
                {
                    status = TrackerStatus.Offline;
                    if (failureMessage == null)
                    {
                        failureMessage = "Failed to connect with tracker.";
                    }
                    return null;
                }

                var responseV4 = DoAnnounceRequest(request, false);
                var responseV6 = DoAnnounceRequest(request, true);

                var responses = await Task.WhenAll(responseV4, responseV6).ConfigureAwait(false);
                var announceResponse = JoinResponses(responses);
                if (announceResponse == null)
                {
                    if (failureMessage == null)
                    {
                        status = TrackerStatus.InvalidResponse;
                        failureMessage = "The tracker returned an invalid announce response.";
                    }
                    return null;
                }

                failureMessage = announceResponse.FailureReason;
                warningMessage = announceResponse.WarningMessage;
                status = TrackerStatus.OK;
                return announceResponse;
            }
            catch (TrackerException ex)
            {
                status = ex.Status;
                failureMessage = string.Format("Failed to perform announce request: {0}", ex.Message);
                return null;
            }
            catch (SocketException ex)
            {
                status = TrackerStatus.Offline;
                failureMessage = string.Format("Failed to perform announce request: {0}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                status = TrackerStatus.InvalidResponse;
                failureMessage = string.Format("Exception performing announce request: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Makes a scrape request to this tracker.
        /// </summary>
        /// <param name="infoHashes">The optional array of info hashes. Can be null or empty.</param>
        /// <returns>The announce response.</returns>
        public override async Task<ScrapeResponse> Scrape(InfoHash[] infoHashes)
        {
            if (infoHashes == null)
                throw new ArgumentNullException("infoHashes");

            try
            {
                failureMessage = null;
                if (!await WaitForConnection().ConfigureAwait(false))
                {
                    status = TrackerStatus.Offline;
                    if (failureMessage == null)
                    {
                        failureMessage = "Failed to connect with tracker.";
                    }
                    return null;
                }

                var responseV4 = DoScrapeRequest(infoHashes, false);
                var responseV6 = DoScrapeRequest(infoHashes, true);

                var firstResponseTask = await Task.WhenAny(responseV4, responseV6).ConfigureAwait(false);
                var scrapeResponse = firstResponseTask.Result;
                if (scrapeResponse == null)
                {
                    if (firstResponseTask == responseV4)
                    {
                        await responseV6;
                        scrapeResponse = responseV6.Result;
                    }
                    else
                    {
                        await responseV4;
                        scrapeResponse = responseV4.Result;
                    }
                }

                if (scrapeResponse == null && failureMessage == null)
                {
                    failureMessage = "The tracker returned an invalid scrape response.";
                }

                return scrapeResponse;
            }
            catch (TrackerException ex)
            {
                failureMessage = string.Format("Failed to perform announce request: {0}", ex.Message);
                return null;
            }
            catch (SocketException ex)
            {
                failureMessage = string.Format("Failed to perform scrape request: {0}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                failureMessage = string.Format("Exception performing scrape request: {0}", ex.Message);
                return null;
            }
        }
        #endregion

        #region Connection
        private async Task<bool> WaitForConnection()
        {
            if (isConnecting)
            {
                // Wait for a connection
                var cancellationTokenSource = new CancellationTokenSource();
                lock (cancellationTokens)
                {
                    cancellationTokens.Add(cancellationTokenSource);
                }

                try
                {
                    var cancellationToken = cancellationTokenSource.Token;
                    while (isConnecting)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(20);
                    }
                }
                catch (OperationCanceledException)
                {
                    // NOTE: If this happens, then we don't "own" the connection process, so we don't set status or failure message.
                    //       Because the "owner" of the process will.
                    return false;
                }
                finally
                {
                    lock (cancellationTokens)
                    {
                        cancellationTokens.Remove(cancellationTokenSource);
                    }
                }
            }
            else if (!isConnected)
            {
                if (!await Connect())
                    return false;
            }

            return isConnected;
        }

        private async Task<bool> Connect()
        {
            isConnecting = true;
            isConnected = false;
            trackerEndpointV4 = null;
            trackerEndpointV6 = null;
            failureMessage = null;
            warningMessage = null;

            var ipAddresses = await Dns.GetHostAddressesAsync(Uri.Host);
            if (ipAddresses == null || ipAddresses.Length == 0)
            {
                failureMessage = string.Format("Unable to resolve host: {0}", Uri.Host);
                isConnected = false;
                isConnecting = false;
                return false;
            }

            trackerEndpointV4 = GetIPEndpoint(ipAddresses, AddressFamily.InterNetwork, Uri.Port);
            trackerEndpointV6 = GetIPEndpoint(ipAddresses, AddressFamily.InterNetworkV6, Uri.Port);
            bool enabledV4 = (trackerEndpointV4 != null);
            bool enabledV6 = (trackerEndpointV6 != null);
            if (!enabledV4 && !enabledV6)
            {
                failureMessage = string.Format("Unable to resolve IP for host: {0}", Uri.Host);
                isConnected = false;
                isConnecting = false;
                return false;
            }

            try
            {
                // Try connecting with both IPv4 and IPv6
                var connectTaskV4 = ConnectV4();
                var connectTaskV6 = ConnectV6();

                var completedTask = await Task.WhenAny(connectTaskV4, connectTaskV6);
                if (!completedTask.Result)
                {
                    await Task.WhenAll(connectTaskV4, connectTaskV6);
                    if (!isConnectedV4 && !isConnectedV6)
                    {
                        status = TrackerStatus.Offline;
                        isConnected = false;
                        isConnecting = false;
                        return false;
                    }
                }

                isConnected = true;
                isConnecting = false;
                return true;
            }
            catch (Exception ex)
            {
                status = TrackerStatus.Offline;
                failureMessage = string.Format("Failed to connecting: {0}", ex.Message);
                isConnected = false;
                isConnecting = false;
                return false;
            }
        }

        private async Task<bool> ConnectV4()
        {
            connectionIDv4 = ProtocolMagic;
            isConnectedV4 = false;

            if (trackerEndpointV4 == null)
                return false;

            try
            {
                var connectionRequest = CreateRequest(TrackerUdpAction.Connect, 16, false);
                await SendRequest(connectionRequest, trackerEndpointV4);

                var responsePacket = await WaitForResponse(connectionRequest, 16);
                if (responsePacket == null)
                {
                    status = TrackerStatus.InvalidResponse;
                    failureMessage = "The tracker returned an invalid connect response.";
                    isConnectedV4 = false;
                    return false;
                }

                connectionIDv4 = responsePacket.ReadInt64();
                isConnectedV4 = true;
                return true;
            }
            catch (Exception ex)
            {
                status = TrackerStatus.Offline;
                failureMessage = string.Format("Failed to connecting: {0}", ex.Message);
                isConnectedV4 = false;
                return false;
            }
        }

        private async Task<bool> ConnectV6()
        {
            connectionIDv6 = ProtocolMagic;
            isConnectedV6 = false;

            if (trackerEndpointV6 == null)
                return false;

            try
            {
                var connectionRequest = CreateRequest(TrackerUdpAction.Connect, 16, true);
                await SendRequest(connectionRequest, trackerEndpointV6);

                var responsePacket = await WaitForResponse(connectionRequest, 16);
                if (responsePacket == null)
                {
                    status = TrackerStatus.InvalidResponse;
                    failureMessage = "The tracker returned an invalid connect response.";
                    isConnectedV6 = false;
                    return false;
                }

                connectionIDv6 = responsePacket.ReadInt64();
                isConnectedV6 = true;
                return true;
            }
            catch (Exception ex)
            {
                status = TrackerStatus.Offline;
                failureMessage = string.Format("Failed to connect: {0}", ex.Message);
                isConnectedV6 = false;
                return false;
            }
        }

        private TrackerRequest CreateRequest(TrackerUdpAction action, int length, bool isIPv6)
        {
            long connectionID = (isIPv6 ? connectionIDv6 : connectionIDv4);
            var endpoint = (isIPv6 ? trackerEndpointV6 : trackerEndpointV4);
            int transactionID = GetNextTransactionID();
            return new TrackerRequest(connectionID, action, transactionID, length, isIPv6, endpoint);
        }

        private async Task SendRequest(TrackerRequest request)
        {
            if (request.IsIPv6)
            {
                if (trackerEndpointV6 == null)
                    return;

                await SendRequest(request, trackerEndpointV6);
            }
            else
            {
                if (trackerEndpointV4 == null)
                    return;

                await SendRequest(request, trackerEndpointV4);
            }
        }

        private async Task SendRequest(TrackerRequest request, IPEndPoint endpoint)
        {
            // We add the new request to pending requests, so that we can easily find the source for incoming responses
            lock (pendingRequests)
            {
                pendingRequests[request.TransactionID] = request;
            }

            // TODO: Send twice?

            var result = socket.BeginSendTo(request.Data, 0, request.Length, SocketFlags.None, endpoint, null, socket);
            int sentByteCount = await Task<int>.Factory.FromAsync(result, socket.EndSend);
            Stats.IncreaseUploadedBytes(sentByteCount);
        }

        private async Task<Packet> WaitForResponse(TrackerRequest request, int minimumResponseLength)
        {
            if (request == null)
                return null;

            int retryCount = 0;
            Packet responsePacket = null;
            try
            {
                while (retryCount < RetryAttempts && (isConnected || request.Action == TrackerUdpAction.Connect))
                {
                    var waitForPacketCancelToken = new CancellationTokenSource();
                    try
                    {
                        int timeout = (15 * (int)Math.Pow(2, retryCount)) * 1000;
                        lock (cancellationTokens)
                        {
                            cancellationTokens.Add(waitForPacketCancelToken);
                        }
                        waitForPacketCancelToken.CancelAfter(timeout);
                        responsePacket = await WaitForPacketArrival(request, waitForPacketCancelToken.Token);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        ++retryCount;

                        // Try sending the request again
                        await SendRequest(request);
                    }
                    finally
                    {
                        // We remove the cancellation token
                        lock (cancellationTokens)
                        {
                            cancellationTokens.Remove(waitForPacketCancelToken);
                        }
                    }
                }
            }
            finally
            {
                // We remove the request from pending requests after this
                lock (pendingRequests)
                {
                    pendingRequests.Remove(request.TransactionID);
                }
            }

            if (responsePacket == null)
            {
                if (!isConnected && request.Action != TrackerUdpAction.Connect)
                {
                    throw new TrackerException(TrackerStatus.Offline, "The request was cancelled, because we are no longer connected to the tracker.");
                }
                else if (retryCount >= RetryAttempts)
                {
                    isConnected = false;
                    isConnectedV4 = false;
                    isConnectedV6 = false;

                    throw new TrackerException(TrackerStatus.Offline, "The request timed out.");
                }
            }

            // We make sure that there is a packet and that it's large enough to fit the header
            if (responsePacket == null || responsePacket.Length < 8)
                return null;

            var responseAction = (TrackerUdpAction)responsePacket.ReadInt32();
            int responseTransactionID = responsePacket.ReadInt32();
            if (responseTransactionID != request.TransactionID)
                return null;

            if (responseAction == TrackerUdpAction.Error)
            {
                // The response was an error
                int errorMessageLength = responsePacket.Length - responsePacket.Offset;
                string errorMessage = responsePacket.ReadString(errorMessageLength);
                throw new TrackerException(TrackerStatus.OK, errorMessage);
            }
            else if (responseAction != request.Action)
            {
                // The response action is different
                return null;
            }

            // We make sure that the response packet is of correct size
            minimumResponseLength = Math.Max(minimumResponseLength, 8); // The minimum response length can't be lower than 8 bytes
            if (responsePacket.Length < minimumResponseLength)
                return null;

            return responsePacket;
        }

        private async Task<Packet> WaitForPacketArrival(TrackerRequest request, CancellationToken cancellationToken)
        {
            Packet responsePacket = null;

            while (true)
            {
                responsePacket = request.ResponsePacket;
                if (responsePacket != null)
                    break;

                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(20);
            }

            return responsePacket;
        }

        private void StartReceivingPackets()
        {
            if (!isListening)
                return;

            try
            {
                EndPoint remoteEndPoint = anyEndPoint;
                socket.BeginReceiveFrom(receiveBuffer, 0, MaxUDPPacketSize, SocketFlags.None, ref remoteEndPoint, OnReceivedPacket, socket);
            }
            catch (ObjectDisposedException)
            {
                // NOTE: We can silence the disposed exceptions because that means that we have stopped the tracker anyways
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
            }
        }

        private void OnReceivedPacket(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            if (socket == null)
                return;

            try
            {
                EndPoint endpoint = anyEndPoint;
                int receivedByteCount = socket.EndReceiveFrom(ar, ref endpoint);
                if (receivedByteCount > 0)
                {
                    Stats.IncreaseDownloadedBytes(receivedByteCount);
                }
                if (receivedByteCount >= 8)
                {
                    byte[] receivedData = new byte[receivedByteCount];
                    Buffer.BlockCopy(receiveBuffer, 0, receivedData, 0, receivedByteCount);

                    Packet receivedPacket = new Packet(receivedData, receivedData.Length);
                    receivedPacket.Offset = 4;

                    int transactionID = receivedPacket.ReadInt32();
                    receivedPacket.Offset = 0;

                    TrackerRequest trackerRequest;
                    lock (pendingRequests)
                    {
                        if (pendingRequests.TryGetValue(transactionID, out trackerRequest))
                        {
                            // Make sure that we received this from the correct endpoint
                            var ipEndPoint = (endpoint as IPEndPoint);
                            if (trackerRequest.IsValidEndpoint(ipEndPoint))
                            {
                                pendingRequests.Remove(transactionID);
                            }
                            else
                            {
                                Log.LogWarning("[UDP Tracker] Received a response to transaction [{0}] from invalid endpoint: {1} != {2}",
                                    transactionID, endpoint, trackerRequest.ExpectedResponseEndPoint);
                                trackerRequest = null;
                            }
                        }
                        else
                        {
                            trackerRequest = null;
                        }
                    }

                    if (trackerRequest != null)
                    {
                        trackerRequest.ResponsePacket = receivedPacket;
                    }
                    else
                    {
                        Log.LogWarning("[UDP Tracker] Received a response with missing transaction: {0}", transactionID);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
            }
            finally
            {
                StartReceivingPackets();
            }
        }

        private int GetNextTransactionID()
        {
            int transactionID = 0;

            // TODO: Can this be improved to reduce the risk of conflicts?
            while (true)
            {
                transactionID = DateTime.Now.GetHashCode() ^ RandomHelper.Next();

                // Make sure that we are not using this transaction ID already
                lock (pendingRequests)
                {
                    if (!pendingRequests.ContainsKey(transactionID))
                    {
                        break;
                    }
                }
            }

            return transactionID;
        }
        #endregion

        #region Response Handling
        private AnnounceResponse HandleAnnounceResponseV4(Packet packet)
        {
            int interval = packet.ReadInt32();
            int leecherCount = packet.ReadInt32();
            int seederCount = packet.ReadInt32();

            this.completeCount = seederCount;
            this.incompleteCount = leecherCount;
            this.minInterval = TimeSpan.FromSeconds(interval);

            if (this.interval < this.minInterval)
            {
                this.interval = this.minInterval;
            }

            int remainingPacketSize = packet.Length - packet.Offset;
            if (remainingPacketSize <= 0)
            {
                return new AnnounceResponse(this, null, null, null);
            }
            else if ((remainingPacketSize % 6) != 0)
            {
                // The announce response is invalid
                return null;
            }

            int peerCount = (remainingPacketSize / 6);
            var peers = new PeerInfo[peerCount];
            for (int i = 0; i < peerCount; i++)
            {
                uint peerIPInteger = packet.ReadUInt32();
                int peerPort = packet.ReadUInt16();

                peerIPInteger = SwapBytes(peerIPInteger);
                var ipAddress = new IPAddress(peerIPInteger);
                var peerEndPoint = new IPEndPoint(ipAddress, peerPort);
                peers[i] = new PeerInfo(peerEndPoint);
            }

            return new AnnounceResponse(this, null, null, peers);
        }

        private AnnounceResponse HandleAnnounceResponseV6(Packet packet)
        {
            int interval = packet.ReadInt32();
            int leecherCount = packet.ReadInt32();
            int seederCount = packet.ReadInt32();

            this.completeCount = seederCount;
            this.incompleteCount = leecherCount;
            this.minInterval = TimeSpan.FromSeconds(interval);

            if (this.interval < this.minInterval)
            {
                this.interval = this.minInterval;
            }

            int remainingPacketSize = packet.Length - packet.Offset;
            if (remainingPacketSize <= 0)
            {
                return new AnnounceResponse(this, null, null, null);
            }
            else if ((remainingPacketSize % 18) != 0)
            {
                // The announce response is invalid
                return new AnnounceResponse(this, null, null, null);
            }

            int peerCount = (remainingPacketSize / 18);
            var peers = new PeerInfo[peerCount];
            for (int i = 0; i < peerCount; i++)
            {
                byte[] peerIPBytes = packet.ReadBytes(16);
                int peerPort = packet.ReadUInt16();

                var ipAddress = new IPAddress(peerIPBytes);
                var peerEndPoint = new IPEndPoint(ipAddress, peerPort);
                peers[i] = new PeerInfo(peerEndPoint);
            }

            return new AnnounceResponse(this, null, null, peers);
        }

        private ScrapeResponse HandleScrapeResponse(Packet packet, InfoHash[] infoHashes)
        {
            int remainingPacketSize = packet.Length - packet.Offset;
            if (remainingPacketSize <= 0)
            {
                return new ScrapeResponse(this, new ScrapeResponse.TorrentInfo[0]);
            }
            else if ((remainingPacketSize % 12) != 0)
            {
                // The scrape response is invalid
                return null;
            }

            int torrentCount = (remainingPacketSize / 12);
            if (torrentCount != infoHashes.Length)
                return null;

            var torrents = new ScrapeResponse.TorrentInfo[torrentCount];
            for (int i = 0; i < torrentCount; i++)
            {
                var infoHash = infoHashes[i];
                int seederCount = packet.ReadInt32();
                int completedCount = packet.ReadInt32();
                int leecherCount = packet.ReadInt32();
                torrents[i] = new ScrapeResponse.TorrentInfo(infoHash, seederCount, leecherCount, completedCount, null);
            }

            return new ScrapeResponse(this, torrents);
        }
        #endregion

        #region Create Requests
        private async Task<AnnounceResponse> DoAnnounceRequest(AnnounceRequest request, bool isIPv6)
        {
            if ((!isIPv6 && trackerEndpointV4 == null) || (isIPv6 && trackerEndpointV6 == null))
                return null;

            UdpExtensions extensions = UdpExtensions.None;
            int extensionSize = 0;
            if (!string.IsNullOrEmpty(authUser))
            {
                extensions |= UdpExtensions.Authentication;
                extensionSize += 1 + authUser.Length + 8;
            }
            if (!string.IsNullOrEmpty(requestString))
            {
                extensions |= UdpExtensions.RequestString;
                extensionSize += 1 + requestString.Length;
            }

            int ipInteger = (!isIPv6 ? GetIPAsInteger(request.IP) : 0);
            var announceRequest = CreateRequest(TrackerUdpAction.Announce, 100 + extensionSize, isIPv6);
            announceRequest.Write(request.InfoHash.Hash, 0, 20);
            announceRequest.Write(request.PeerID.ID, 0, 20);
            announceRequest.WriteInt64(request.BytesDownloaded);
            announceRequest.WriteInt64(request.BytesLeft);
            announceRequest.WriteInt64(request.BytesUploaded);
            announceRequest.WriteInt32((int)request.TrackerEvent);
            announceRequest.WriteInt32(ipInteger);
            announceRequest.WriteInt32(key);
            announceRequest.WriteInt32((request.DesiredPeerCount > 0 ? request.DesiredPeerCount : -1));
            announceRequest.WriteUInt16((ushort)request.Port);
            announceRequest.WriteUInt16((ushort)extensions);

            if ((extensions & UdpExtensions.Authentication) != 0)
            {
                announceRequest.WriteByte((byte)authUser.Length);
                announceRequest.WriteString(authUser);

                byte[] passwordHash = GenerateAuthPasswordHash(announceRequest, authPassHash);
                announceRequest.Write(passwordHash, 0, 8);
            }

            if ((extensions & UdpExtensions.RequestString) != 0)
            {
                announceRequest.WriteByte((byte)requestString.Length);
                announceRequest.WriteString(requestString);
            }

            await SendRequest(announceRequest);

            var responsePacket = await WaitForResponse(announceRequest, 20);
            if (responsePacket == null)
            {
                status = TrackerStatus.Offline;
                failureMessage = "Timed out making announce request.";
                return null;
            }

            AnnounceResponse announceResponse;
            if (isIPv6)
                announceResponse = HandleAnnounceResponseV6(responsePacket);
            else
                announceResponse = HandleAnnounceResponseV4(responsePacket);

            return announceResponse;
        }

        private async Task<ScrapeResponse> DoScrapeRequest(InfoHash[] infoHashes, bool isIPv6)
        {
            if ((!isIPv6 && trackerEndpointV4 == null) || (isIPv6 && trackerEndpointV6 == null))
                return null;

            var scrapeRequest = CreateRequest(TrackerUdpAction.Scrape, 16 + (20 * infoHashes.Length), isIPv6);
            for (int i = 0; i < infoHashes.Length; i++)
            {
                var hashBytes = infoHashes[i].Hash;
                scrapeRequest.Write(hashBytes, 0, 20);
            }

            await SendRequest(scrapeRequest);

            var responsePacket = await WaitForResponse(scrapeRequest, 8 + (12 * infoHashes.Length));
            if (responsePacket == null)
            {
                failureMessage = "Timed out making scrape request.";
                return null;
            }

            var scrapeResponse = HandleScrapeResponse(responsePacket, infoHashes);
            return scrapeResponse;
        }
        #endregion

        #region Helper Methods
        private static string GetAuthUserName(Uri uri)
        {
            string userInfo = uri.UserInfo;
            if (string.IsNullOrEmpty(userInfo))
                return null;

            int colonIndex = userInfo.IndexOf(':');
            string userName = null;
            if (colonIndex != -1)
            {
                userName = userInfo.Substring(0, colonIndex);
            }
            else
            {
                userName = UriHelper.UrlDecodeText(userInfo);
            }

            userName = UriHelper.UrlDecodeText(userName);
            if (userName.Length <= 255)
            {
                return userName;
            }
            else
            {
                return null;
            }
        }

        private static string GetAuthPassword(Uri uri)
        {
            string userInfo = uri.UserInfo;
            if (string.IsNullOrEmpty(userInfo))
                return null;

            int colonIndex = userInfo.IndexOf(':');
            if (colonIndex != -1)
            {
                string password = userInfo.Substring(colonIndex + 1);
                return UriHelper.UrlDecodeText(password);
            }
            else
            {
                return string.Empty;
            }
        }

        private static byte[] GetAuthPasswordHash(Uri uri)
        {
            string password = GetAuthPassword(uri);
            if (password == null)
                return null;

            byte[] passwordBytes = System.Text.Encoding.ASCII.GetBytes(password);
            return HashHelper.ComputeSHA1(passwordBytes, 0, passwordBytes.Length);
        }

        private static string GetRequestString(Uri uri)
        {
            string pathAndQuery = uri.PathAndQuery;
            if (string.IsNullOrEmpty(pathAndQuery))
                return null;
            else if (pathAndQuery.Length <= 255) // We can pass both the path and query
                return pathAndQuery;

            string path = uri.AbsolutePath;
            if (string.IsNullOrEmpty(path))
                return null;
            else if (path.Length <= 255) // We can pass only the path
                return path;

            // Nothing was short enough to pass along
            return null;
        }

        private static int GetIPAsInteger(IPAddress ipAddress)
        {
            if (ipAddress == null)
                return 0;
            else if (ipAddress.AddressFamily != AddressFamily.InterNetwork) // Only IPv4 addresses are allowed
                return 0;

            var addressBytes = ipAddress.GetAddressBytes();
            return ((addressBytes[0] << 24) | (addressBytes[1] << 16) | (addressBytes[2] << 8) | addressBytes[3]);
        }

        private static byte[] GenerateAuthPasswordHash(Packet packet, byte[] passwordHash)
        {
            int packetSize = packet.Length;
            byte[] packetData = packet.Data;

            using (var sha1 = SHA1.Create())
            {
                int offset = 0;
                while (offset < packetSize)
                {
                    offset += sha1.TransformBlock(packetData, offset, packetSize - offset, packetData, offset);
                }

                offset = 0;
                while (offset < passwordHash.Length)
                {
                    offset += sha1.TransformBlock(passwordHash, offset, passwordHash.Length - offset, passwordHash, offset);
                }

                sha1.TransformFinalBlock(packetData, 0, 0);
                return sha1.Hash;
            }
        }

        private static IPEndPoint GetIPEndpoint(IPAddress[] ipAddresses, AddressFamily addressFamily, int port)
        {
            if (ipAddresses == null || ipAddresses.Length == 0)
                return null;

            if (addressFamily == AddressFamily.InterNetwork)
            {
                if (!Socket.OSSupportsIPv4)
                    return null;
            }
            else if (addressFamily == AddressFamily.InterNetworkV6)
            {
                if (!Socket.OSSupportsIPv6)
                    return null;
            }
            else
            {
                // The address family is not supported
                return null;
            }

            IPEndPoint endpoint = null;

            for (int i = 0; i < ipAddresses.Length; i++)
            {
                if (ipAddresses[i].AddressFamily == addressFamily)
                {
                    var ipAddress = ipAddresses[i];
                    if (addressFamily == AddressFamily.InterNetwork)
                    {
                        // We require all IPs to be of IPv6 format
                        ipAddress = ipAddress.MapToIPv6();
                    }
                    endpoint = new IPEndPoint(ipAddress, port);
                    break;
                }
            }

            return endpoint;
        }

        private static AnnounceResponse JoinResponses(AnnounceResponse[] responses)
        {
            if (responses == null || responses.Length == 0)
                return null;

            AnnounceResponse response = null;
            for (int i = 0; i < responses.Length; i++)
            {
                if (responses[i] != null)
                {
                    if (response != null)
                    {
                        response.Merge(responses[i]);
                    }
                    else
                    {
                        response = responses[i];
                    }
                }
            }

            return response;
        }

        private uint SwapBytes(uint x)
        {
            return ((x & 0x000000FF) << 24) +
                   ((x & 0x0000FF00) << 8) +
                   ((x & 0x00FF0000) >> 8) +
                   ((x & 0xFF000000) >> 24);
        }
        #endregion
    }
}
