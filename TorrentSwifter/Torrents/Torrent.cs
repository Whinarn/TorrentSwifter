using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TorrentSwifter.Collections;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;
using TorrentSwifter.Managers;
using TorrentSwifter.Peers;
using TorrentSwifter.Preferences;
using TorrentSwifter.Torrents.PieceSelection;
using TorrentSwifter.Torrents.RateLimiter;
using TorrentSwifter.Trackers;

namespace TorrentSwifter.Torrents
{
    // TODO: Add support to pause torrents
    // TODO: Add support to set if torrents should go over to endgame mode automatically once almost completed

    /// <summary>
    /// A torrent.
    /// </summary>
    public sealed class Torrent
    {
        #region Consts
        private const int MinBlockSize = 1 * (int)SizeHelper.KiloByte;
        private const int MaxBlockSize = 16 * (int)SizeHelper.KiloByte;
        private const int DefaultBlockSize = 16 * (int)SizeHelper.KiloByte;
        #endregion

        #region Fields
        private readonly TorrentMetaData metaData;
        private readonly InfoHash infoHash;
        private readonly PeerID peerID;
        private readonly string downloadPath;
        private readonly int blockSize;
        private readonly long totalSize;
        private readonly bool isPrivate;

        private bool hasVerifiedIntegrity = false;
        private bool isStarted = false;
        private bool isStopped = true;
        private bool isVerifyingIntegrity = false;
        private bool isCompleted = false;

        private BitField bitField = null;
        private TorrentPiece[] pieces = null;
        private TorrentFile[] files = null;
        private long bytesLeftToDownload = 0L;

        private IPieceSelector pieceSelector = DefaultPieceSelector;

        private long sessionDownloadedBytes = 0L;
        private long sessionUploadedBytes = 0L;
        private RateMeasurer sessionDownloadRate = new RateMeasurer();
        private RateMeasurer sessionUploadRate = new RateMeasurer();

        private IRateLimiter downloadRateLimiter = null;
        private IRateLimiter uploadRateLimiter = null;
        private BandwidthLimiter downloadBandwidthLimiter = null;
        private BandwidthLimiter uploadBandwidthLimiter = null;

        private List<TrackerGroup> trackerGroups = new List<TrackerGroup>();

        private List<Peer> peers = new List<Peer>();
        private Dictionary<PeerID, Peer> peersByID = new Dictionary<PeerID, Peer>();
        private object peersSyncObj = new object();

        private int isProcessingIncomingPieceRequests = 0;
        private int isProcessingOutgoingPieceRequests = 0;
        private ConcurrentQueue<IncomingPieceRequest> incomingPieceRequests = new ConcurrentQueue<IncomingPieceRequest>();
        private ConcurrentQueue<OutgoingPieceRequest> outgoingPieceRequests = new ConcurrentQueue<OutgoingPieceRequest>();
        private ConcurrentList<OutgoingPieceRequest> pendingOutgoingPieceRequests = new ConcurrentList<OutgoingPieceRequest>();
        private List<Peer> tempRequestPiecePeers = new List<Peer>();

        private static readonly IPieceSelector DefaultPieceSelector = new AvailableThenRarestFirstPieceSelector();
        #endregion

        #region Events
        /// <summary>
        /// An event that occurs once a piece has been downloaded and verified.
        /// </summary>
        public event EventHandler<PieceEventArgs> PieceVerified;
        /// <summary>
        /// An event that occurs once an integrity check has completed.
        /// </summary>
        public event EventHandler IntegrityCheckCompleted;
        /// <summary>
        /// An event that occurs once the entire torrent has completed downloading.
        /// </summary>
        public event EventHandler Completed;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the torrent meta-data information.
        /// </summary>
        public TorrentMetaData MetaData
        {
            get { return metaData; }
        }

        /// <summary>
        /// Gets the info hash of this torrent.
        /// </summary>
        public InfoHash InfoHash
        {
            get { return infoHash; }
        }

        /// <summary>
        /// Gets the peer ID for us with this torrent.
        /// </summary>
        public PeerID PeerID
        {
            get { return peerID; }
        }

        /// <summary>
        /// Gets if this torrent is private.
        /// </summary>
        public bool IsPrivate
        {
            get { return isPrivate; }
        }

        /// <summary>
        /// Gets the path to download to.
        /// </summary>
        public string DownloadPath
        {
            get { return downloadPath; }
        }

        /// <summary>
        /// Gets the count of pieces in this torrent.
        /// </summary>
        public int PieceCount
        {
            get { return metaData.PieceCount; }
        }

        /// <summary>
        /// Gets the size of each piece.
        /// </summary>
        public int PieceSize
        {
            get { return metaData.PieceSize; }
        }

        /// <summary>
        /// Gets the size of each block.
        /// </summary>
        public int BlockSize
        {
            get { return blockSize; }
        }

        /// <summary>
        /// Gets the total size of this torrent.
        /// </summary>
        public long TotalSize
        {
            get { return totalSize; }
        }

        /// <summary>
        /// Gets if this torrent has been started.
        /// </summary>
        public bool IsStarted
        {
            get { return isStarted; }
        }

        /// <summary>
        /// Gets if this torrent has been stopped entirely.
        /// </summary>
        public bool IsStopped
        {
            get { return isStopped; }
        }

        /// <summary>
        /// Gets if this torrent has been stopped entirely.
        /// </summary>
        public bool IsStoppedOrStopping
        {
            get { return (isStopped || !isStarted); }
        }

        /// <summary>
        /// Gets if this torrent has completed downloading.
        /// </summary>
        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        /// <summary>
        /// Gets the bit-field for this torrent.
        /// </summary>
        public BitField BitField
        {
            get { return bitField; }
        }

        /// <summary>
        /// Gets the state of this torrent.
        /// </summary>
        public TorrentState State
        {
            get
            {
                if (!isStarted)
                    return TorrentState.Inactive;
                else if (isVerifyingIntegrity)
                    return TorrentState.IntegrityChecking;
                else if (isCompleted)
                    return TorrentState.Seeding;
                else
                    return TorrentState.Downloading;
            }
        }

        /// <summary>
        /// Gets or sets the currently used piece selector for downloading.
        /// </summary>
        public IPieceSelector PieceSelector
        {
            get { return pieceSelector; }
            set { pieceSelector = value ?? DefaultPieceSelector; }
        }

        /// <summary>
        /// Gets the amount of bytes downloaded this session.
        /// </summary>
        public long SessionDownloadedBytes
        {
            get { return sessionDownloadedBytes; }
        }

        /// <summary>
        /// Gets the amount of bytes uploaded this session.
        /// </summary>
        public long SessionUploadedBytes
        {
            get { return sessionUploadedBytes; }
        }

        /// <summary>
        /// Gets the average download rate this session.
        /// </summary>
        public long SessionDownloadRate
        {
            get { return sessionDownloadRate.AverageRate; }
        }

        /// <summary>
        /// Gets the average upload rate this session.
        /// </summary>
        public long SessionUploadRate
        {
            get { return sessionUploadRate.AverageRate; }
        }

        /// <summary>
        /// Gets or sets the download bandwidth limit in bytes per second for this torrent.
        /// </summary>
        public long DownloadBandwidthLimit
        {
            get { return downloadBandwidthLimiter.RateLimit; }
            set { downloadBandwidthLimiter.RateLimit = value; }
        }

        /// <summary>
        /// Gets or sets the upload bandwidth limit in bytes per second for this torrent.
        /// </summary>
        public long UploadBandwidthLimit
        {
            get { return uploadBandwidthLimiter.RateLimit; }
            set { uploadBandwidthLimiter.RateLimit = value; }
        }

        /// <summary>
        /// Gets the amount of bytes left to download.
        /// </summary>
        public long BytesLeftToDownload
        {
            get { return bytesLeftToDownload; }
        }

        /// <summary>
        /// Gets the count of peers that we know of for this torrent.
        /// </summary>
        public int PeerCount
        {
            get
            {
                lock (peersSyncObj)
                {
                    return peers.Count;
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new torrent.
        /// </summary>
        /// <param name="metaData">The torrent meta-data information.</param>
        /// <param name="downloadPath">The download path for the torrent.</param>
        /// <param name="blockSize">The desired block size.</param>
        public Torrent(TorrentMetaData metaData, string downloadPath, int blockSize = DefaultBlockSize)
        {
            if (metaData == null)
                throw new ArgumentNullException("metaData");
            else if (blockSize < MinBlockSize)
                throw new ArgumentOutOfRangeException("blockSize", string.Format("The block size must be above or equals to {0} bytes ({1}).", MinBlockSize, SizeHelper.GetHumanReadableSize(MinBlockSize)));
            else if (blockSize > MaxBlockSize)
                throw new ArgumentOutOfRangeException("blockSize", string.Format("The block size must be below or equals to {0} bytes ({1}).", MaxBlockSize, SizeHelper.GetHumanReadableSize(MaxBlockSize)));
            else if (!MathHelper.IsPowerOfTwo(blockSize))
                throw new ArgumentException("The block size must be a power of two.", "blockSize");

            this.metaData = metaData;
            this.infoHash = metaData.InfoHash;
            this.isPrivate = MetaData.IsPrivate;
            this.peerID = PeerHelper.GetNewPeerID();
            this.downloadPath = Path.GetFullPath(downloadPath);
            this.blockSize = blockSize;
            this.totalSize = metaData.TotalSize;
            this.bytesLeftToDownload = totalSize;

            InitializePieces();
            InitializeFiles();
            InitializeTrackers();
            InitializeRateLimiters();
        }
        #endregion

        #region Public Methods
        #region Start & Stop
        /// <summary>
        /// Starts downloading and/or uploading of this torrent.
        /// </summary>
        public void Start()
        {
            if (isStarted || !isStopped)
                throw new InvalidOperationException("The torrent is still running. Make sure it has stopped completely first.");
            else if (!TorrentEngine.IsInitialized)
                throw new InvalidOperationException("The torrent engine must be initialized before starting torrents.");

            isStarted = true;
            isStopped = false;
            TorrentRegistry.RegisterTorrent(this);

            Log.LogInfo("[Torrent] Starting up Torrent.");

            // Reset the session download & upload statistics
            sessionDownloadedBytes = 0L;
            sessionUploadedBytes = 0L;
            sessionDownloadRate.Reset();
            sessionUploadRate.Reset();

            if (!hasVerifiedIntegrity)
            {
                VerifyIntegrity(true);
            }

            // Start the update loop
            var updateTask = UpdateLoop();
            updateTask.CatchExceptions();

            // Broadcast us to local neighbours
            LocalPeerDiscovery.Broadcast(this);
        }

        /// <summary>
        /// Stops downloading and uploading of this torrent.
        /// </summary>
        public void Stop()
        {
            if (!isStarted)
                return;

            isStarted = false;
            TorrentRegistry.UnregisterTorrent(this);

            AnnounceTrackers(TrackerEvent.Stopped);

            // Disconnect all peers connected to this torrent
            DisconnectAllPeers();

            Log.LogInfo("[Torrent] Stopped Torrent.");
        }
        #endregion

        #region Integrity
        /// <summary>
        /// Rechecks the integrity of this torrent.
        /// Note that this only works when the torrent is stopped.
        /// </summary>
        public void RecheckIntegrity()
        {
            if (isStarted)
                return;

            hasVerifiedIntegrity = false;
            VerifyIntegrity(false);
        }
        #endregion

        #region Pieces
        /// <summary>
        /// Returns a piece in this torrent by its index.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <returns>The piece.</returns>
        public TorrentPiece GetPiece(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= pieces.Length)
                throw new ArgumentOutOfRangeException("pieceIndex");

            return pieces[pieceIndex];
        }
        #endregion

        #region Peers
        /// <summary>
        /// Adds a peer to this torrent.
        /// </summary>
        /// <param name="peerInfo">The peer information.</param>
        public void AddPeer(PeerInfo peerInfo)
        {
            if (peerInfo.EndPoint == null)
                throw new ArgumentException("The peer end-point cannot be null.", "peerInfo");

            lock (peersSyncObj)
            {
                var peerEndPoint = peerInfo.EndPoint;
                var peerID = peerInfo.ID;

                Peer peer;
                if (peerID.HasValue && peersByID.TryGetValue(peerID.Value, out peer))
                {
                    peer.UpdateEndPoint(peerEndPoint);
                }
                else
                {
                    peer = new Peer(this, peerEndPoint);
                    peers.Add(peer);
                    if (peerID.HasValue)
                    {
                        peer.ID = peerID.Value;
                        peersByID.Add(peerID.Value, peer);
                    }
                }
            }
        }

        /// <summary>
        /// Adds multiple peers to this torrent.
        /// </summary>
        /// <param name="peerInfos">The array of peer information.</param>
        public void AddPeers(PeerInfo[] peerInfos)
        {
            if (peerInfos == null)
                throw new ArgumentNullException("peerInfos");

            for (int i = 0; i < peerInfos.Length; i++)
            {
                var peerInfo = peerInfos[i];
                AddPeer(peerInfo);
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Returns the human readable size string of a specific size.
        /// </summary>
        /// <param name="size">The size in bytes.</param>
        /// <returns>The human readable size string.</returns>
        public static string GetHumanReadableSize(long size)
        {
            return SizeHelper.GetHumanReadableSize(size);
        }

        /// <summary>
        /// Returns the human readable speed string of a specific speed.
        /// </summary>
        /// <param name="speed">The speed in bytes.</param>
        /// <returns>The human readable speed string.</returns>
        public static string GetHumanReadableSpeed(long speed)
        {
            return SizeHelper.GetHumanReadableSpeed(speed);
        }
        #endregion
        #endregion

        #region Private Methods
        #region Initialization
        private void InitializePieces()
        {
            long totalSize = metaData.TotalSize;
            int pieceSize = metaData.PieceSize;
            int pieceCount = metaData.PieceCount;
            pieces = new TorrentPiece[pieceCount];

            int lastPieceSize = (int)(totalSize % pieceSize);
            if (lastPieceSize == 0)
                lastPieceSize = pieceSize;

            for (int i = 0; i < pieceCount; i++)
            {
                bool isLastPiece = (i == (pieceCount - 1));
                int currentPieceSize = (isLastPiece ? lastPieceSize : pieceSize);
                long pieceOffset = ((long)i * (long)PieceSize);
                pieces[i] = new TorrentPiece(this, i, pieceOffset, currentPieceSize, blockSize);
            }

            bitField = new BitField(pieceCount);
        }

        private void InitializeFiles()
        {
            var metaDataFiles = metaData.Files;
            files = new TorrentFile[metaDataFiles.Length];

            if (metaDataFiles.Length == 1)
            {
                var metaDataFile = metaDataFiles[0];
                string filePath = downloadPath;
                if (Directory.Exists(filePath))
                {
                    filePath = IOHelper.GetTorrentFilePath(filePath, metaDataFile);
                }
                
                files[0] = new TorrentFile(metaDataFile.Path, filePath, metaDataFile.Size, 0L);

                if (!File.Exists(filePath))
                {
                    if (Prefs.Torrent.AllocateFullFileSizes)
                    {
                        IOHelper.CreateAllocatedFile(filePath, metaDataFile.Size);
                    }
                    else
                    {
                        IOHelper.CreateEmptyFile(filePath);
                    }
                }
            }
            else if (metaDataFiles.Length > 1)
            {
                long currentFileOffset = 0L;

                for (int i = 0; i < metaDataFiles.Length; i++)
                {
                    var metaDataFile = metaDataFiles[i];
                    string filePath = IOHelper.GetTorrentFilePath(downloadPath, metaDataFile);
                    files[i] = new TorrentFile(metaDataFile.Path, filePath, metaDataFile.Size, currentFileOffset);
                    currentFileOffset += metaDataFile.Size;

                    if (!File.Exists(filePath))
                    {
                        if (Prefs.Torrent.AllocateFullFileSizes)
                        {
                            IOHelper.CreateAllocatedFile(filePath, metaDataFile.Size);
                        }
                        else
                        {
                            IOHelper.CreateEmptyFile(filePath);
                        }
                    }
                }
            }
        }

        private void InitializeTrackers()
        {
            // TODO: Add support to load customized trackers if this is not the first time we launch this torrent

            var announces = metaData.Announces;
            if (announces == null)
                return;

            foreach (var announceItem in announces)
            {
                var trackerGroup = new TrackerGroup(this);
                foreach (string url in announceItem.Urls)
                {
                    Uri uri;
                    if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                    {
                        var tracker = Tracker.Create(uri);
                        if (tracker != null)
                        {
                            trackerGroup.AddTracker(tracker);
                        }
                        else
                        {
                            Log.LogWarning("Unsupported tracker URI: {0}", uri);
                        }
                    }
                    else
                    {
                        Log.LogWarning("Unsupported tracker URI: {0}", url);
                    }
                }

                trackerGroup.Shuffle();
                trackerGroups.Add(trackerGroup);
            }
        }

        private void InitializeRateLimiters()
        {
            // TODO: Get saved download and upload limits if this torrent is resumes

            downloadBandwidthLimiter = new BandwidthLimiter(sessionDownloadRate, 0L);
            uploadBandwidthLimiter = new BandwidthLimiter(sessionUploadRate, 0L);

            var downloadRateLimiter = new RateLimiterGroup();
            downloadRateLimiter.Add(Stats.downloadRateLimiter);
            downloadRateLimiter.Add(downloadBandwidthLimiter);
            downloadRateLimiter.Add(new DiskWriteLimiter());

            var uploadRateLimiter = new RateLimiterGroup();
            uploadRateLimiter.Add(Stats.uploadRateLimiter);
            uploadRateLimiter.Add(uploadBandwidthLimiter);
            uploadRateLimiter.Add(new DiskReadLimiter());

            this.downloadRateLimiter = downloadRateLimiter;
            this.uploadRateLimiter = uploadRateLimiter;
        }
        #endregion

        #region Pieces
        private bool HasDownloadedAllPieces()
        {
            bool result = true;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!pieces[i].IsVerified)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        private void CheckIfCompletedDownload()
        {
            if (isCompleted)
                return;

            if (HasDownloadedAllPieces())
            {
                isCompleted = true;
                bytesLeftToDownload = 0L;

                CancelAllOutgoingPieceRequests();
                AnnounceTrackers(TrackerEvent.Completed);

                Log.LogInfo("[Torrent] The torrent has completed downloading.");

                Completed.SafeInvoke(this, EventArgs.Empty);
            }
        }

        private async Task VerifyPiece(TorrentPiece piece)
        {
            if (piece.IsVerifying)
                return;

            piece.IsVerifying = true;
            try
            {
                int pieceIndex = piece.Index;
                var pieceHash = metaData.PieceHashes[pieceIndex];
                byte[] computedPieceHash = await GetPieceHash(piece);

                bool isVerified = (computedPieceHash != null && pieceHash.Equals(computedPieceHash));
                if (piece.IsVerified != isVerified)
                {
                    piece.IsVerified = isVerified;
                    bitField.Set(pieceIndex, isVerified);

                    if (isVerified)
                    {
                        Interlocked.Add(ref bytesLeftToDownload, -piece.Size); // subtract

                        var eventArgs = new PieceEventArgs(pieceIndex);
                        PieceVerified.SafeInvoke(this, eventArgs);
                    }
                    else
                    {
                        Interlocked.Add(ref bytesLeftToDownload, piece.Size);
                    }
                }
            }
            finally
            {
                piece.IsVerifying = false;
            }
        }

        private async Task<byte[]> ReadPiece(TorrentPiece piece)
        {
            byte[] pieceData = new byte[piece.Size];
            long pieceOffset = piece.Offset;

            int readByteCount = await DiskManager.ReadAsync(this, pieceOffset, pieceData, 0, pieceData.Length);
            if (readByteCount != pieceData.Length)
                return null;

            return pieceData;
        }

        private async Task<byte[]> GetPieceHash(TorrentPiece piece)
        {
            byte[] pieceData = await ReadPiece(piece);
            if (pieceData == null)
                return null;

            return HashHelper.ComputeSHA1(pieceData);
        }
        #endregion

        #region Piece Requests
        private async Task ProcessIncomingPieceRequests()
        {
            // Prevents this task to be processed concurrently
            if (Interlocked.CompareExchange(ref isProcessingIncomingPieceRequests, 1, 0) != 0)
                return;

            IncomingPieceRequest request;
            long currentExtraRate = blockSize;
            while (downloadRateLimiter.TryProcess(currentExtraRate) && incomingPieceRequests.TryDequeue(out request))
            {
                if (request.IsCancelled || !request.Peer.IsConnected)
                    continue;

                int pieceIndex = request.PieceIndex;
                int begin = request.Begin;
                int length = request.Length;

                var piece = pieces[pieceIndex];
                if (!piece.IsVerified)
                    continue;

                long offset = piece.Offset + begin;
                byte[] data = new byte[length];
                int readByteCount = await DiskManager.ReadAsync(this, offset, data, 0, length);
                if (readByteCount != length)
                {
                    Log.LogError("[Torrent] Read {0} bytes from piece {1} at offset {2} when {3} bytes were expected.", readByteCount, pieceIndex, begin, length);
                    continue;
                }

                await request.Peer.SendPieceData(pieceIndex, begin, data);
                currentExtraRate += length;
            }

            // Reset the flag that we are currently processing
            Interlocked.Exchange(ref isProcessingIncomingPieceRequests, 0);
        }

        private async Task ProcessOutgoingPieceRequests()
        {
            // Prevents this task to be processed concurrently
            if (Interlocked.CompareExchange(ref isProcessingOutgoingPieceRequests, 1, 0) != 0)
                return;

            TimeoutOutgoingPieceRequests();
            RequestMorePieces();

            OutgoingPieceRequest request;
            long currentExtraRate = blockSize;
            while (uploadRateLimiter.TryProcess(currentExtraRate) && outgoingPieceRequests.TryDequeue(out request))
            {
                if (request.IsCancelled || !request.Peer.IsConnected)
                    continue;

                var peer = request.Peer;
                int pieceIndex = request.PieceIndex;
                int blockIndex = request.BlockIndex;

                var piece = pieces[pieceIndex];
                if (piece.IsVerified)
                    continue;

                var block = piece.GetBlock(blockIndex);
                if (block.IsDownloaded)
                    continue;

                try
                {
                    request.OnSent();
                    block.AddRequestPeer(peer);
                    pendingOutgoingPieceRequests.Add(request);
                    Log.LogDebug("[Peer][{0}] We are sending a piece request. Index: {1}, Block: {2}, Offset: {3}, Length: {4}", peer.EndPoint, pieceIndex, blockIndex, (blockIndex * blockSize), block.Size);

                    if (await peer.RequestPieceData(pieceIndex, blockIndex))
                    {
                        currentExtraRate += block.Size;
                    }
                    else
                    {
                        request.OnCancelSent();
                        block.RemoveRequestPeer(peer);
                        pendingOutgoingPieceRequests.Remove(request);
                        peer.UnregisterPieceRequest(request);
                    }
                }
                catch (Exception ex)
                {
                    Log.LogErrorException(ex);
                    block.RemoveRequestPeer(peer);
                    pendingOutgoingPieceRequests.Remove(request);
                }
            }

            // Reset the flag that we are currently processing
            Interlocked.Exchange(ref isProcessingOutgoingPieceRequests, 0);
        }

        private void TimeoutOutgoingPieceRequests()
        {
            int pieceRequestTimeout = Prefs.Peer.PieceRequestTimeout;
            pendingOutgoingPieceRequests.RemoveAny((pendingRequest) =>
            {
                if (pendingRequest.IsCancelled)
                    return true;

                if (pieceRequestTimeout > 0 && pendingRequest.RequestAge >= pieceRequestTimeout)
                {
                    // TODO: Add penalty points to the peer so that we can disconnect it if there are too many?
                    pendingRequest.Cancel();
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        private void RequestMorePieces()
        {
            var peerList = tempRequestPiecePeers;
            var rankedPieces = pieceSelector.GetRankedPieces(this, pieces);
            foreach (var piece in rankedPieces)
            {
                GetPeersWithPiece(piece.Index, true, peerList);
                if (peerList.Count == 0)
                    continue;

                int blockCount = piece.BlockCount;
                for (int blockIndex = 0; blockIndex < blockCount && peerList.Count > 0; blockIndex++)
                {
                    var block = piece.GetBlock(blockIndex);
                    if (block.IsDownloaded)
                        continue;
                    else if (block.IsRequested) // TODO: Allow for more requests for the same block after a certain time, but not to the same peer more than once
                        continue;

                    // Get a random peer from the list and check if we can still request pieces from the peer
                    var peer = RandomHelper.GetRandomFromList(peerList);
                    if (!peer.CanRequestPiecesFrom)
                    {
                        peerList.Remove(peer);
                        continue;
                    }

                    var request = new OutgoingPieceRequest(this, peer, piece.Index, blockIndex);
                    outgoingPieceRequests.Enqueue(request);
                    peer.RegisterPieceRequest(request);

                    // Remove the peer from the list if we can send no more requests
                    if (!peer.CanRequestPiecesFrom)
                    {
                        peerList.Remove(peer);
                    }
                }
            }
        }

        private IncomingPieceRequest FindIncomingPieceRequest(Peer peer, int pieceIndex, int begin, int length)
        {
            IncomingPieceRequest result = null;
            foreach (var request in incomingPieceRequests)
            {
                if (request.Equals(peer, pieceIndex, begin, length))
                {
                    result = request;
                    break;
                }
            }
            return result;
        }

        private void CancelIncomingPieceRequestsWithPeer(Peer peer)
        {
            foreach (var request in incomingPieceRequests)
            {
                if (request.Peer == peer)
                {
                    request.IsCancelled = true;
                }
            }
        }

        private void CancelAllOutgoingPieceRequests()
        {
            foreach (var request in outgoingPieceRequests)
            {
                request.Cancel();
            }

            pendingOutgoingPieceRequests.RemoveAny((request) =>
            {
                request.Cancel();
                return true;
            });
        }

        private void CancelOutgoingPieceRequestsWithPeer(Peer peer)
        {
            foreach (var request in outgoingPieceRequests)
            {
                if (request.Peer == peer)
                {
                    request.Cancel();
                }
            }

            pendingOutgoingPieceRequests.RemoveAny((request) =>
            {
                if (request.Peer == peer)
                {
                    request.Cancel();
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        private void CancelOutgoingPieceRequestsForBlock(int pieceIndex, int blockIndex)
        {
            foreach (var request in outgoingPieceRequests)
            {
                if (request.Equals(pieceIndex, blockIndex))
                {
                    request.Cancel();
                }
            }

            pendingOutgoingPieceRequests.RemoveAny((request) =>
            {
                if (request.Equals(pieceIndex, blockIndex))
                {
                    request.Cancel();
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }
        #endregion

        #region Integrity
        private void VerifyIntegrity(bool allowWhenStarted)
        {
            if (isVerifyingIntegrity || hasVerifiedIntegrity || (!allowWhenStarted && isStarted))
                return;

            isVerifyingIntegrity = true;
            Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        var piece = pieces[i];
                        await VerifyPiece(piece);
                    }

                    isCompleted = HasDownloadedAllPieces();
                    hasVerifiedIntegrity = true;

                    if (isCompleted)
                    {
                        Log.LogInfo("[Torrent] The torrent was completed at launch and has now entered seeding mode.");
                    }
                    else
                    {
                        Log.LogDebug("[Torrent] The torrent was not completed at launch and has now entered leecher mode.");
                    }

                    IntegrityCheckCompleted.SafeInvoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log.LogErrorException(ex);

                    // Stop the torrent if it's active, because of the failure
                    Stop();
                }
                finally
                {
                    isVerifyingIntegrity = false;
                }
            }).CatchExceptions();
        }
        #endregion

        #region Trackers
        private void UpdateTrackers()
        {
            foreach (var trackerGroup in trackerGroups)
            {
                trackerGroup.Update();
            }
        }

        private void AnnounceTrackers(TrackerEvent trackerEvent)
        {
            // We only care about completed and stopped events here
            if (trackerEvent != TrackerEvent.Completed && trackerEvent != TrackerEvent.Stopped)
                return;

            foreach (var trackerGroup in trackerGroups)
            {
                var announceTask = trackerGroup.Announce(trackerEvent);
                announceTask.ContinueWith((task) =>
                {
                    if (!task.IsFaulted)
                    {
                        var announceResponse = task.Result;
                        if (announceResponse != null)
                        {
                            ProcessAnnounceResponse(announceResponse);
                        }
                    }
                    else
                    {
                        Log.LogErrorException(task.Exception);
                    }
                });
            }
        }
        #endregion

        #region Peers
        private void GetPeersWithPiece(int pieceIndex, bool requestPiecesFrom, List<Peer> peerList)
        {
            peerList.Clear();
            lock (peersSyncObj)
            {
                foreach (var peer in peers)
                {
                    if (requestPiecesFrom && !peer.CanRequestPiecesFrom)
                        continue;

                    if (peer.IsCompleted)
                    {
                        peerList.Add(peer);
                    }
                    else
                    {
                        var peerBitField = peer.BitField;
                        if (peerBitField != null && peerBitField.Get(pieceIndex))
                        {
                            peerList.Add(peer);
                        }
                    }
                }
            }
        }

        private void UpdatePeers()
        {
            lock (peersSyncObj)
            {
                foreach (var peer in peers)
                {
                    try
                    {
                        peer.Update();
                    }
                    catch (Exception ex)
                    {
                        Log.LogErrorException(ex);
                    }
                }
            }
        }

        private void DisconnectAllPeers()
        {
            lock (peersSyncObj)
            {
                foreach (var peer in peers)
                {
                    peer.Disconnect();
                }
            }
        }
        #endregion

        #region Update Loop
        private async Task UpdateLoop()
        {
            try
            {
                while (isStarted)
                {
                    try
                    {
                        sessionDownloadRate.Update();
                        sessionUploadRate.Update();

                        if (!isVerifyingIntegrity && hasVerifiedIntegrity)
                        {
                            UpdateTrackers();
                            UpdatePeers();

                            await ProcessIncomingPieceRequests();
                            await ProcessOutgoingPieceRequests();
                        }

                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        Log.LogErrorException(ex);
                    }
                }
            }
            finally
            {
                isStopped = true;
            }
        }
        #endregion
        #endregion

        #region Internal Methods
        #region Trackers
        internal void ProcessAnnounceResponse(AnnounceResponse announceResponse)
        {
            var peerInfos = announceResponse.Peers;
            if (peerInfos == null)
                return;

            Log.LogInfo("[Torrent] Received {0} peers from a tracker.", peerInfos.Length);

            AddPeers(peerInfos);
        }
        #endregion

        #region Peers
        internal void RegisterPeerWithID(PeerID peerID, Peer peer)
        {
            lock (peersSyncObj)
            {
                peersByID[peerID] = peer;
            }
        }

        internal Peer OnPeerHandshaked(PeerID peerID, PeerConnection connection)
        {
            lock (peersSyncObj)
            {
                Peer peer;
                if (peersByID.TryGetValue(peerID, out peer))
                {
                    peer.ReplaceConnection(connection);
                }
                else
                {
                    peer = new Peer(this, connection);
                    peer.ID = peerID;
                    peers.Add(peer);
                    peersByID.Add(peerID, peer);
                }
                return peer;
            }
        }

        internal void OnPeerDisconnected(Peer peer)
        {
            CancelIncomingPieceRequestsWithPeer(peer);
            CancelOutgoingPieceRequestsWithPeer(peer);
        }

        internal void OnPeerChokingUs(Peer peer)
        {
            CancelOutgoingPieceRequestsWithPeer(peer);
        }

        internal int GetPeerCountWithPiece(int pieceIndex)
        {
            int peerCount = 0;
            lock (peersSyncObj)
            {
                foreach (var peer in peers)
                {
                    if (peer.IsCompleted)
                    {
                        ++peerCount;
                    }
                    else
                    {
                        var peerBitField = peer.BitField;
                        if (peerBitField != null && peerBitField.Get(pieceIndex))
                        {
                            ++peerCount;
                        }
                    }
                }
            }
            return peerCount;
        }
        #endregion

        #region Pieces
        internal void OnPieceBlockRequested(Peer peer, int pieceIndex, int begin, int length)
        {
            var existingRequest = FindIncomingPieceRequest(peer, pieceIndex, begin, length);
            if (existingRequest == null)
            {
                var newRequest = new IncomingPieceRequest(peer, pieceIndex, begin, length);
                incomingPieceRequests.Enqueue(newRequest);

                var processTask = ProcessIncomingPieceRequests();
                processTask.CatchExceptions();
            }
        }

        internal void OnPieceBlockCancelled(Peer peer, int pieceIndex, int begin, int length)
        {
            var existingRequest = FindIncomingPieceRequest(peer, pieceIndex, begin, length);
            if (existingRequest != null)
            {
                existingRequest.IsCancelled = true;

                var processTask = ProcessIncomingPieceRequests();
                processTask.CatchExceptions();
            }
        }

        internal void OnReceivedPieceBlock(Peer peer, int pieceIndex, int blockIndex, byte[] data)
        {
            var piece = pieces[pieceIndex];
            var block = piece.GetBlock(blockIndex);

            // Ignore blocks that we haven't requested
            if (!block.IsRequested)
                return;

            // Remove the pending outgoing request, and make sure that there was one
            OutgoingPieceRequest request;
            if (!pendingOutgoingPieceRequests.TryTake((req) => req.Equals(peer, pieceIndex, blockIndex), out request))
                return;

            block.IsDownloaded = true;
            block.HasWrittenToDisk = false;
            block.RemoveRequestPeer(peer);
            peer.UnregisterPieceRequest(request);

            // Cancel other requests for the same block on other peers
            CancelOutgoingPieceRequestsForBlock(pieceIndex, blockIndex);

            // Process more outgoing piece requests
            var processTask = ProcessOutgoingPieceRequests();
            processTask.CatchExceptions();

            long offset = piece.Offset + (blockIndex * blockSize);
            DiskManager.QueueWrite(this, offset, data, (writeSuccess, writeException) =>
            {
                if (writeSuccess)
                {
                    block.HasWrittenToDisk = true;

                    if (piece.HasDownloadedAllBlocks())
                    {
                        var verifyTask = VerifyPiece(piece);
                        verifyTask.CatchExceptions();
                        verifyTask.ContinueWith((task) =>
                        {
                            CheckIfCompletedDownload();
                        }, TaskContinuationOptions.OnlyOnRanToCompletion);
                    }
                }
                else
                {
                    block.HasWrittenToDisk = false;
                    block.IsDownloaded = false;

                    Log.LogErrorException(writeException);
                }
            });
        }
        #endregion

        #region Statistics
        internal void IncreaseSessionDownloadedBytes(long amount)
        {
            Interlocked.Add(ref sessionDownloadedBytes, amount);
            sessionDownloadRate.Add(amount);
        }

        internal void IncreaseSessionUploadedBytes(long amount)
        {
            Interlocked.Add(ref sessionUploadedBytes, amount);
            sessionUploadRate.Add(amount);
        }
        #endregion

        #region Read & Write
        internal int ReadData(long torrentOffset, byte[] buffer, int bufferOffset, int count)
        {
            if (torrentOffset < 0 || torrentOffset >= totalSize)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (count < 0 || (bufferOffset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");

            int totalBytesRead = 0;
            for (int i = 0; i < files.Length && count > 0; i++)
            {
                var file = files[i];
                if (torrentOffset >= file.Offset && torrentOffset < file.EndOffset)
                {
                    long localOffset = torrentOffset - file.Offset;
                    int localCount = (int)Math.Min(file.Size - localOffset, count);

                    if (!file.Exists)
                        break;

                    int readByteCount = file.Read(localOffset, buffer, bufferOffset, localCount);
                    if (readByteCount <= 0)
                        break;

                    torrentOffset += readByteCount;
                    bufferOffset += readByteCount;
                    count -= readByteCount;
                    totalBytesRead += readByteCount;
                }
            }
            return totalBytesRead;
        }

        internal async Task<int> ReadDataAsync(long torrentOffset, byte[] buffer, int bufferOffset, int count)
        {
            if (torrentOffset < 0 || torrentOffset >= totalSize)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (count < 0 || (bufferOffset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");

            int totalBytesRead = 0;
            for (int i = 0; i < files.Length && count > 0; i++)
            {
                var file = files[i];
                if (torrentOffset >= file.Offset && torrentOffset < file.EndOffset)
                {
                    long localOffset = torrentOffset - file.Offset;
                    int localCount = (int)Math.Min(file.Size - localOffset, count);

                    if (!file.Exists)
                        break;

                    int readByteCount = await file.ReadAsync(localOffset, buffer, bufferOffset, localCount);
                    if (readByteCount <= 0)
                        break;

                    torrentOffset += readByteCount;
                    bufferOffset += readByteCount;
                    count -= readByteCount;
                    totalBytesRead += readByteCount;
                }
            }
            return totalBytesRead;
        }

        internal void WriteData(long torrentOffset, byte[] buffer, int bufferOffset, int count)
        {
            if (torrentOffset < 0 || torrentOffset >= totalSize)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (count < 0 || (bufferOffset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");

            for (int i = 0; i < files.Length && count > 0; i++)
            {
                var file = files[i];
                if (torrentOffset >= file.Offset && torrentOffset < file.EndOffset)
                {
                    long localOffset = torrentOffset - file.Offset;
                    int localCount = (int)Math.Min(file.Size - localOffset, count);

                    IOHelper.CreateParentDirectoryIfItDoesntExist(file.FullPath);
                    file.Write(localOffset, buffer, bufferOffset, localCount);

                    torrentOffset += localCount;
                    bufferOffset += localCount;
                    count -= localCount;
                }
            }
        }

        internal async Task WriteDataAsync(long torrentOffset, byte[] buffer, int bufferOffset, int count)
        {
            if (torrentOffset < 0 || torrentOffset >= totalSize)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (count < 0 || (bufferOffset + count) > buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");

            for (int i = 0; i < files.Length && count > 0; i++)
            {
                var file = files[i];
                if (torrentOffset >= file.Offset && torrentOffset < file.EndOffset)
                {
                    long localOffset = torrentOffset - file.Offset;
                    int localCount = (int)Math.Min(file.Size - localOffset, count);

                    IOHelper.CreateParentDirectoryIfItDoesntExist(file.FullPath);
                    await file.WriteAsync(localOffset, buffer, bufferOffset, localCount);

                    torrentOffset += localCount;
                    bufferOffset += localCount;
                    count -= localCount;
                }
            }
        }
        #endregion
        #endregion
    }
}
