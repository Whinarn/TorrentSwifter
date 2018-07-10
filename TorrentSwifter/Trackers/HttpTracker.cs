using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TorrentSwifter.Encodings;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;
using TorrentSwifter.Peers;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// An HTTP torrent tracker.
    /// </summary>
    public sealed class HttpTracker : Tracker
    {
        #region Fields
        private readonly Uri announceUri;
        private readonly Uri scrapeUri;
        private readonly string key;

        private HttpClient client = null;
        private string trackerID = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets if we can announce to this tracker.
        /// </summary>
        public override bool CanAnnounce
        {
            get { return (announceUri != null); }
        }

        /// <summary>
        /// Gets if this tracker can be scraped.
        /// </summary>
        public override bool CanScrape
        {
            get { return (scrapeUri != null); }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new HTTP tracker.
        /// </summary>
        /// <param name="uri">The tracker URI.</param>
        public HttpTracker(Uri uri)
            : base(uri)
        {
            if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The URI-scheme is not HTTP or HTTPS.", "uri");
            }

            this.announceUri = uri;
            this.scrapeUri = GetScrapeUri(uri);
            this.key = GenerateRandomKey();

            var assemblyVersion = AssemblyHelper.GetAssemblyVersion(typeof(HttpTracker));
            var userAgentProductInfo = new ProductInfoHeaderValue("TorrentSwifter", assemblyVersion.ToString());

            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(userAgentProductInfo);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string userInfo = uri.UserInfo;
                if (userInfo.IndexOf(':') != -1)
                {
                    byte[] userInfoBytes = Encoding.ASCII.GetBytes(userInfo);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(userInfoBytes));
                }
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

            // Check if announces are allowed
            if (announceUri == null)
                return null;

            try
            {
                Uri uri = request.GetUri(announceUri, key, trackerID);
                byte[] responseBytes = await client.GetByteArrayAsync(uri).ConfigureAwait(false);
                var info = BEncoding.Decode(responseBytes) as BEncoding.Dictionary;
                if (info == null)
                {
                    status = TrackerStatus.InvalidResponse;
                    failureMessage = "The tracker returned an invalid announce response.";
                    return null;
                }

                var announceResponse = HandleAnnounceResponse(info);
                if (announceResponse == null)
                {
                    status = TrackerStatus.InvalidResponse;
                    failureMessage = "The tracker returned an invalid announce response.";
                    return null;
                }

                status = TrackerStatus.OK;
                return announceResponse;
            }
            catch (HttpRequestException ex)
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

            // Check if scrapes are allowed
            if (scrapeUri == null)
                return null;

            try
            {
                var queryParameters = new KeyValuePair<string, object>[infoHashes.Length];
                for (int i = 0; i < infoHashes.Length; i++)
                {
                    queryParameters[i] = new KeyValuePair<string, object>("info_hash", infoHashes[i].ToUrlEncodedString());
                }

                var uri = UriHelper.AppendQueryString(scrapeUri, queryParameters);
                byte[] responseBytes = await client.GetByteArrayAsync(uri).ConfigureAwait(false);
                var info = BEncoding.Decode(responseBytes) as BEncoding.Dictionary;
                if (info == null)
                {
                    failureMessage = "The tracker returned an invalid scrape response.";
                    return null;
                }

                var scrapeResponse = HandleScrapeResponse(info);
                if (scrapeResponse == null)
                {
                    failureMessage = "The tracker returned an invalid scrape response.";
                    return null;
                }

                return scrapeResponse;
            }
            catch (HttpRequestException ex)
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

        #region Response Handling
        private AnnounceResponse HandleAnnounceResponse(BEncoding.Dictionary info)
        {
            string failureReason, warningMessage;
            if (info.TryGetString("failure reason", out failureReason))
            {
                this.failureMessage = failureReason;
            }
            if (info.TryGetString("warning message", out warningMessage))
            {
                this.warningMessage = warningMessage;
            }

            long completeCount, incompleteCount, downloadedCount, interval, minInterval;
            if (info.TryGetInteger("complete", out completeCount))
            {
                this.completeCount = (int)completeCount;
            }
            if (info.TryGetInteger("incomplete", out incompleteCount))
            {
                this.incompleteCount = (int)incompleteCount;
            }
            if (info.TryGetInteger("downloaded", out downloadedCount))
            {
                this.downloadedCount = (int)downloadedCount;
            }
            if (info.TryGetInteger("interval", out interval))
            {
                this.interval = TimeSpan.FromSeconds(interval);
            }
            if (info.TryGetInteger("min interval", out minInterval))
            {
                this.minInterval = TimeSpan.FromSeconds(minInterval);
                if (this.interval < this.minInterval)
                {
                    this.interval = this.minInterval;
                }
            }

            string trackerID;
            if (info.TryGetString("tracker id", out trackerID))
            {
                this.trackerID = trackerID;
            }

            PeerInfo[] peerInfos = null;
            byte[] compactPeerBytes;
            BEncoding.List peerList;
            if (info.TryGetByteArray("peers", out compactPeerBytes))
            {
                peerInfos = DecodePeers(compactPeerBytes);
            }
            else if (info.TryGetList("peers", out peerList))
            {
                peerInfos = DecodePeers(peerList);
            }

            return new AnnounceResponse(this, failureReason, warningMessage, peerInfos);
        }

        private ScrapeResponse HandleScrapeResponse(BEncoding.Dictionary info)
        {
            BEncoding.Dictionary files;
            if (!info.TryGetDictionary("files", out files))
                return null;

            var torrentInfos = new List<ScrapeResponse.TorrentInfo>(files.Count);
            foreach (var torrentInfo in files)
            {
                var torrentInfoDict = (torrentInfo.Value as BEncoding.Dictionary);
                if (torrentInfoDict == null)
                    continue;

                long completeCount, incompleteCount, downloadedCount;
                string torrentName;
                if (!torrentInfoDict.TryGetInteger("complete", out completeCount))
                {
                    completeCount = 0;
                }
                if (!torrentInfoDict.TryGetInteger("incomplete", out incompleteCount))
                {
                    incompleteCount = 0;
                }
                if (!torrentInfoDict.TryGetInteger("downloaded", out downloadedCount))
                {
                    downloadedCount = 0;
                }
                if (!torrentInfoDict.TryGetString("name", out torrentName))
                {
                    torrentName = null;
                }

                byte[] infoHashBytes = Encoding.UTF8.GetBytes(torrentInfo.Key); // TODO: Is this okay to do?
                var infoHash = new InfoHash(infoHashBytes);
                torrentInfos.Add(new ScrapeResponse.TorrentInfo(infoHash, (int)completeCount, (int)incompleteCount, (int)downloadedCount, torrentName));
            }

            return new ScrapeResponse(this, torrentInfos.ToArray());
        }
        #endregion

        #region Helper Methods
        private static PeerInfo[] DecodePeers(byte[] peerBytes)
        {
            if ((peerBytes.Length % 6) != 0)
                return null;

            int peerCount = (peerBytes.Length / 6);
            var peerInfos = new PeerInfo[peerCount];

            for (int i = 0; i < peerCount; i++)
            {
                int offset = (i * 6);
                string ipAddressStr = string.Format("{0}.{1}.{2}.{3}", peerBytes[offset], peerBytes[offset + 1], peerBytes[offset + 2], peerBytes[offset + 3]);
                var ipAddress = IPAddress.Parse(ipAddressStr);
                int port = ((peerBytes[offset + 4] << 8) | peerBytes[offset + 5]);
                var endPoint = new IPEndPoint(ipAddress, port);
                peerInfos[i] = new PeerInfo(endPoint);
            }

            return peerInfos;
        }

        private static PeerInfo[] DecodePeers(BEncoding.List peerList)
        {
            var peerInfoList = new List<PeerInfo>(peerList.Count);

            foreach (var peer in peerList)
            {
                if (peer is BEncoding.Dictionary)
                {
                    var peerDict = (peer as BEncoding.Dictionary);
                    byte[] peerIDBytes;
                    if (!peerDict.TryGetByteArray("peer id", out peerIDBytes) && !peerDict.TryGetByteArray("peer_id", out peerIDBytes))
                    {
                        peerIDBytes = null;
                    }

                    string peerIP;
                    long peerPort;
                    if (peerDict.TryGetString("ip", out peerIP) && peerDict.TryGetInteger("port", out peerPort) && peerPort > 0 && peerPort <= ushort.MaxValue)
                    {
                        IPAddress ipAddress;
                        if (IPAddress.TryParse(peerIP, out ipAddress))
                        {
                            var endPoint = new IPEndPoint(ipAddress, (int)peerPort);
                            if (peerIDBytes != null && peerIDBytes.Length == 20)
                            {
                                var peerID = new PeerID(peerIDBytes);
                                peerInfoList.Add(new PeerInfo(peerID, endPoint));
                            }
                            else
                            {
                                peerInfoList.Add(new PeerInfo(endPoint));
                            }
                        }
                        else
                        {
                            Log.LogError("[HTTP Tracker] Unable to parse peer IP: {0}", peerIP);
                        }
                    }
                }
                else if (peer is byte[])
                {
                    var peerBytes = peer as byte[];
                    var decodedPeers = DecodePeers(peerBytes);
                    if (decodedPeers != null)
                    {
                        peerInfoList.AddRange(decodedPeers);
                    }
                    else
                    {
                        Log.LogError("[HTTP Tracker] Unable to decode peer information from {0} bytes of compact data.", peerBytes.Length);
                    }
                }
                else
                {
                    Log.LogError("[HTTP Tracker] Unable decode get peer information with invalid BEncode type: {0}", (peer != null ? peer.GetType().Name : "<null>"));
                }
            }

            return peerInfoList.ToArray();
        }

        private static Uri GetScrapeUri(Uri announceUri)
        {
            string localPathPrefix = string.Empty;
            string localPath = announceUri.LocalPath;
            int lastSlashIndex = localPath.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {
                localPathPrefix = localPath.Substring(0, lastSlashIndex + 1);
                localPath = localPath.Substring(lastSlashIndex + 1);
            }

            string extension = string.Empty;
            int lastDotIndex = localPath.LastIndexOf('.');
            if (lastDotIndex != -1)
            {
                extension = localPath.Substring(lastDotIndex);
                localPath = localPath.Substring(0, lastDotIndex);
            }

            if (!string.Equals(localPath, "announce", StringComparison.Ordinal))
                return null;

            var uriBuilder = new UriBuilder(announceUri.Scheme, announceUri.Host);
            if (!announceUri.IsDefaultPort)
            {
                uriBuilder.Port = announceUri.Port;
            }

            uriBuilder.Path = string.Format("{0}scrape{1}", localPathPrefix, extension);
            uriBuilder.Query = announceUri.Query;
            return uriBuilder.Uri;
        }

        private static string GenerateRandomKey()
        {
            var guid = Guid.NewGuid();
            var guidBytes = guid.ToByteArray();
            return UriHelper.UrlEncodeText(guidBytes, 0, 8);
        }
        #endregion
    }
}
