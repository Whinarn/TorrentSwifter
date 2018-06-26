using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        private const long ProtocolMagic = 0x41727101980L;

        private const int ConnectionTimeout = 15000;
        private const int RequestTimeout = 15000;
        #endregion

        #region Enums
        private enum TrackerUdpAction : int
        {
            Connect = 0,
            Announce = 1,
            Scrape = 2,
            Error = 3
        }
        #endregion

        #region Classes
        private class TrackerRequest : Packet
        {
            private readonly TrackerUdpAction action;
            private readonly int transactionID;
            private readonly DateTime date;
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

            public bool HasResponse
            {
                get { return (responsePacket != null); }
            }

            public Packet ResponsePacket
            {
                get { return responsePacket; }
                set { responsePacket = value; }
            }

            public TrackerRequest(long connectionID, TrackerUdpAction action, int transactionID, int length)
                : base(length)
            {
                this.action = action;
                this.transactionID = transactionID;
                date = DateTime.Now;

                WriteInt64(connectionID);
                WriteInt32((int)action);
                WriteInt32(transactionID);
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
        private UdpClient client = null;
        private bool isListening = false;
        private readonly int key;

        private bool isConnecting = false;
        private bool isConnected = false;
        private long connectionID = 0L;

        private Dictionary<int, TrackerRequest> pendingRequests = new Dictionary<int, TrackerRequest>();

        private static Random random = new Random();
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

            client = new UdpClient(uri.Host, uri.Port);
            isListening = true;
            StartReceivingPackets();

            lock (random)
            {
                key = DateTime.Now.GetHashCode() ^ random.Next();
            }
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
            if (client != null)
            {
                client.Dispose();
                client = null;
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
                if (!await WaitForConnection().ConfigureAwait(false))
                {
                    status = TrackerStatus.Offline;
                    failureMessage = "Failed to connect with tracker.";
                    return null;
                }

                // TODO: Add support for IPv6 announces

                int ipInteger = GetIPAsInteger(request.IP);
                var announceRequest = CreateRequest(TrackerUdpAction.Announce, 98);
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
                await SendRequest(announceRequest);

                var responsePacket = await WaitForResponse(announceRequest, 20, RequestTimeout);
                if (responsePacket == null)
                {
                    status = TrackerStatus.Offline;
                    failureMessage = "Timed out making announce request.";
                    return null;
                }

                var announceResponse = HandleAnnounceResponseV4(responsePacket);
                if (announceResponse == null)
                {
                    status = TrackerStatus.InvalidResponse;
                    failureMessage = "The tracker returned an invalid announce response.";
                    return null;
                }

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
                if (!await WaitForConnection().ConfigureAwait(false))
                {
                    failureMessage = "Failed to connect with tracker.";
                    return null;
                }

                var scrapeRequest = CreateRequest(TrackerUdpAction.Scrape, 16 + (20 * infoHashes.Length));
                for (int i = 0; i < infoHashes.Length; i++)
                {
                    var hashBytes = infoHashes[i].Hash;
                    scrapeRequest.Write(hashBytes, 0, 20);
                }

                await SendRequest(scrapeRequest);

                var responsePacket = await WaitForResponse(scrapeRequest, 8 + (12 * infoHashes.Length), RequestTimeout);
                if (responsePacket == null)
                {
                    failureMessage = "Timed out making scrape request.";
                    return null;
                }

                var scrapeResponse = HandleScrapeResponse(responsePacket, infoHashes);
                if (scrapeResponse == null)
                {
                    failureMessage = "The tracker returned an invalid scrape response.";
                    return null;
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
                cancellationTokenSource.CancelAfter(ConnectionTimeout);
                var cancellationToken = cancellationTokenSource.Token;

                while (isConnecting)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(20);
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
            client.Connect(Uri.Host, Uri.Port);

            connectionID = ProtocolMagic;
            var connectionRequest = CreateRequest(TrackerUdpAction.Connect, 16);
            await SendRequest(connectionRequest);

            var responsePacket = await WaitForResponse(connectionRequest, 16, ConnectionTimeout);
            if (responsePacket == null)
            {
                isConnected = false;
                isConnecting = false;
                return false;
            }

            connectionID = responsePacket.ReadInt64();
            isConnected = true;
            isConnecting = false;
            return true;
        }

        private TrackerRequest CreateRequest(TrackerUdpAction action, int length)
        {
            int transactionID = GetNextTransactionID();
            return new TrackerRequest(connectionID, action, transactionID, length);
        }

        private async Task SendRequest(TrackerRequest request)
        {
            // We add the new request to pending requests, so that we can easily find the source for incoming responses
            lock (pendingRequests)
            {
                pendingRequests.Add(request.TransactionID, request);
            }

            // TODO: Send more than once?
            await client.SendAsync(request.Data, request.Length);
        }

        private async Task<Packet> WaitForResponse(TrackerRequest request, int minimumResponseLength, int timeout)
        {
            Packet responsePacket = null;
            try
            {
                var waitForPacketCancelToken = new CancellationTokenSource();
                waitForPacketCancelToken.CancelAfter(timeout);
                responsePacket = await WaitForPacketArrival(request, waitForPacketCancelToken.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TrackerException(TrackerStatus.Offline, "The request timed out.");
            }
            finally
            {
                // We remove the request from pending requests after this
                lock (pendingRequests)
                {
                    pendingRequests.Remove(request.TransactionID);
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
            client.BeginReceive(OnReceivedPacket, client);
        }

        private void OnReceivedPacket(IAsyncResult ar)
        {
            UdpClient client = ar.AsyncState as UdpClient;
            if (client == null)
                return;

            try
            {
                IPEndPoint endpoint = null;
                byte[] receivedData = client.EndReceive(ar, ref endpoint);
                if (receivedData != null && receivedData.Length >= 8)
                {
                    Packet receivedPacket = new Packet(receivedData, receivedData.Length);
                    receivedPacket.Offset = 4;

                    int transactionID = receivedPacket.ReadInt32();
                    receivedPacket.Offset = 0;

                    TrackerRequest trackerRequest;
                    lock (pendingRequests)
                    {
                        if (pendingRequests.TryGetValue(transactionID, out trackerRequest))
                        {
                            pendingRequests.Remove(transactionID);
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
                        // TODO: Log request with missing transaction?
                    }
                }
            }
            catch
            {
                // TODO: Log exception!
            }
            finally
            {
                if (isListening)
                {
                    client.BeginReceive(OnReceivedPacket, client);
                }
            }
        }

        private int GetNextTransactionID()
        {
            int transactionID = 0;

            // TODO: Can this be improved to reduce the risk of conflicts?
            while (true)
            {
                int randomNumber;
                lock (random)
                {
                    randomNumber = random.Next();
                }

                transactionID = DateTime.Now.GetHashCode() ^ randomNumber;

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

        #region Helper Methods
        private static int GetIPAsInteger(IPAddress ipAddress)
        {
            if (ipAddress == null)
                return 0;
            else if (ipAddress.AddressFamily != AddressFamily.InterNetwork) // Only IPv4 addresses are allowed
                return 0;

            var addressBytes = ipAddress.GetAddressBytes();
            return ((addressBytes[0] << 24) | (addressBytes[1] << 16) | (addressBytes[2] << 8) | addressBytes[3]);
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
