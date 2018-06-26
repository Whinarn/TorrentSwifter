﻿using System;
using System.Collections.Generic;
using System.Net;
using TorrentSwifter.Helpers;
using TorrentSwifter.Peers;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// An tracker announce request.
    /// </summary>
    public sealed class AnnounceRequest
    {
        #region Fields
        private readonly InfoHash infoHash;
        private readonly PeerID peerID;
        private int port;
        private long bytesUploaded = 0L;
        private long bytesDownloaded = 0L;
        private long bytesLeft = 0L;
        private TrackerEvent trackerEvent = TrackerEvent.None;
        private IPAddress ip = null;
        private int desiredPeerCount = -1;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the info hash of the announce request.
        /// </summary>
        public InfoHash InfoHash
        {
            get { return infoHash; }
        }

        /// <summary>
        /// Gets the peer ID of the announce request.
        /// </summary>
        public PeerID PeerID
        {
            get { return peerID; }
        }

        /// <summary>
        /// Gets or sets the port we are listening for connections on.
        /// </summary>
        public int Port
        {
            get { return port; }
            set
            {
                if (value <= 0 || value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException("value");

                port = value;
            }
        }

        /// <summary>
        /// Gets or sets the total amount of uploaded bytes this session.
        /// </summary>
        public long BytesUploaded
        {
            get { return bytesUploaded; }
            set { bytesUploaded = value; }
        }

        /// <summary>
        /// Gets or sets the total amount of downloaded bytes this session.
        /// </summary>
        public long BytesDownloaded
        {
            get { return bytesDownloaded; }
            set { bytesDownloaded = value; }
        }

        /// <summary>
        /// Gets or sets the amount of bytes left to download.
        /// </summary>
        public long BytesLeft
        {
            get { return bytesLeft; }
            set { bytesLeft = value; }
        }

        /// <summary>
        /// Gets or sets the tracker event to announce.
        /// </summary>
        public TrackerEvent TrackerEvent
        {
            get { return trackerEvent; }
            set { trackerEvent = value; }
        }

        /// <summary>
        /// Gets or sets the IP-address to report to the tracker.
        /// If this is null, then the tracker will assume your external IP.
        /// Bare in mind that some trackers might ignore this value however.
        /// </summary>
        public IPAddress IP
        {
            get { return ip; }
            set { ip = value; }
        }

        /// <summary>
        /// Gets or sets the desired peer count from the tracker.
        /// Zero or below will return the default count from the tracker, which is most commonly 50.
        /// </summary>
        public int DesiredPeerCount
        {
            get { return desiredPeerCount; }
            set { desiredPeerCount = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new tracker announce request.
        /// </summary>
        /// <param name="infoHash">The info hash.</param>
        /// <param name="peerID">The peer ID.</param>
        /// <param name="port">The port number used to listen for client connections on.</param>
        public AnnounceRequest(InfoHash infoHash, PeerID peerID, int port)
        {
            if (infoHash.Hash == null)
                throw new ArgumentException("The info hash is invalid.", "infoHash");
            else if (peerID.ID == null)
                throw new ArgumentException("The peer ID is invalid.", "infoHash");
            else if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("port");

            this.infoHash = infoHash;
            this.peerID = peerID;
            this.port = port;
        }
        #endregion

        #region Internal Methods
        internal Uri GetUri(Uri announceUri, string key, string trackerID)
        {
            var parameters = new Dictionary<string, object>(13);
            parameters.Add("info_hash", infoHash.ToUrlEncodedString());
            parameters.Add("peer_id", peerID.ToUrlEncodedString());
            parameters.Add("port", port);
            parameters.Add("uploaded", bytesUploaded);
            parameters.Add("downloaded", bytesDownloaded);
            parameters.Add("left", bytesLeft);
            parameters.Add("compact", "1");
            parameters.Add("supportcrypto", "1");

            if (trackerEvent != TrackerEvent.None)
            {
                parameters.Add("event", trackerEvent.ToString().ToLowerInvariant());
            }
            if (ip != null)
            {
                parameters.Add("ip", UriHelper.UrlEncodeText(ip.ToString()));
            }
            if (desiredPeerCount > 0)
            {
                parameters.Add("numwant", desiredPeerCount);
            }
            if (!string.IsNullOrEmpty(key))
            {
                parameters.Add("key", key);
            }
            if (!string.IsNullOrEmpty(trackerID))
            {
                parameters.Add("trackerid", trackerID);
            }

            return UriHelper.AppendQueryString(announceUri, parameters);
        }
        #endregion
    }
}
