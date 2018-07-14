using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TorrentSwifter.Collections;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;
using TorrentSwifter.Peers;
using TorrentSwifter.Trackers;

namespace TorrentSwifter.Torrents
{
    // TODO: Announce complete to trackers when we have finished downloading

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

        private bool hasVerifiedIntegrity = false;
        private bool isStarted = false;
        private bool isStopped = true;
        private bool isVerifyingIntegrity = false;
        private bool isSeeding = false;

        private BitField bitField = null;
        private TorrentPiece[] pieces = null;
        private TorrentFile[] files = null;
        private long bytesLeftToDownload = 0L;

        private long sessionDownloadedBytes = 0L;
        private long sessionUploadedBytes = 0L;

        private List<TrackerGroup> trackerGroups = new List<TrackerGroup>();

        private List<Peer> peers = new List<Peer>();
        private Dictionary<PeerID, Peer> peersByID = new Dictionary<PeerID, Peer>();
        private object peersSyncObj = new object();

        private int isProcessingIncomingPieceRequests = 0;
        private int isProcessingOutgoingPieceRequests = 0;
        private ConcurrentQueue<IncomingPieceRequest> incomingPieceRequests = new ConcurrentQueue<IncomingPieceRequest>();
        private ConcurrentQueue<OutgoingPieceRequest> outgoingPieceRequests = new ConcurrentQueue<OutgoingPieceRequest>();
        private ConcurrentList<OutgoingPieceRequest> pendingOutgoingPieceRequests = new ConcurrentList<OutgoingPieceRequest>();
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
        /// Gets if this torrent is being seeded (only uploaded).
        /// </summary>
        public bool IsSeeding
        {
            get { return isSeeding; }
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
                else if (isSeeding)
                    return TorrentState.Seeding;
                else
                    return TorrentState.Downloading;
            }
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
        /// Gets the amount of bytes left to download.
        /// </summary>
        public long BytesLeftToDownload
        {
            get { return bytesLeftToDownload; }
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
            this.peerID = PeerHelper.GetNewPeerID();
            this.downloadPath = Path.GetFullPath(downloadPath);
            this.blockSize = blockSize;
            this.totalSize = metaData.TotalSize;
            this.bytesLeftToDownload = totalSize;

            InitializePieces();
            InitializeFiles();
            InitializeTrackers();

            // TODO: Initialize cached data, like integrity etc?
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
                return;

            isStarted = true;
            isStopped = false;
            TorrentRegistry.RegisterTorrent(this);

            // Reset the session download & upload statistics
            sessionDownloadedBytes = 0L;
            sessionUploadedBytes = 0L;

            if (!hasVerifiedIntegrity)
            {
                VerifyIntegrity();
            }

            var updateTask = UpdateLoop();
            updateTask.CatchExceptions();
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

            // TODO: Perform any unitialization here
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
            VerifyIntegrity();
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
                pieces[i] = new TorrentPiece(i, pieceOffset, currentPieceSize, blockSize);
            }

            bitField = new BitField(pieceCount);
        }

        private void InitializeFiles()
        {
            var metaDataFiles = metaData.Files;
            files = new TorrentFile[metaDataFiles.Length];

            if (metaDataFiles.Length == 1)
            {
                var file = metaDataFiles[0];
                files[0] = new TorrentFile(file.Path, downloadPath, file.Size, 0L);

                if (Preferences.Torrent.AllocateFullFileSizes)
                {
                    IOHelper.CreateAllocatedFile(downloadPath, file.Size);
                }
                else
                {
                    IOHelper.CreateEmptyFile(downloadPath);
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

                    if (Preferences.Torrent.AllocateFullFileSizes)
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

        private void InitializeTrackers()
        {
            // TODO: Add support to load customized trackers if this is not the first time we launch this torrent

            var announces = metaData.Announces;
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

        private async Task<bool> VerifyPiece(int pieceIndex)
        {
            var piece = pieces[pieceIndex];
            var pieceHash = metaData.PieceHashes[pieceIndex];
            byte[] computedPieceHash = await GetPieceHash(pieceIndex);

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
            return isVerified;
        }

        private async Task<byte[]> ReadPiece(int pieceIndex)
        {
            var piece = pieces[pieceIndex];
            byte[] pieceData = new byte[piece.Size];
            long pieceOffset = ((long)pieceIndex * (long)PieceSize);
            int readByteCount = await ReadData(pieceOffset, pieceData, 0, pieceData.Length);
            if (readByteCount != pieceData.Length)
                return null;

            return pieceData;
        }

        private async Task<byte[]> GetPieceHash(int pieceIndex)
        {
            byte[] pieceData = await ReadPiece(pieceIndex);
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
            while (incomingPieceRequests.TryDequeue(out request))
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
                int readByteCount = await ReadData(offset, data, 0, length);
                if (readByteCount != length)
                {
                    Log.LogError("[Torrent] Read {0} bytes from piece {1} at offset {2} when {3} bytes were expected.", readByteCount, pieceIndex, begin, length);
                    continue;
                }

                await request.Peer.SendPieceData(pieceIndex, begin, data);
            }

            // Reset the flag that we are currently processing
            Interlocked.Exchange(ref isProcessingIncomingPieceRequests, 0);
        }

        private async Task ProcessOutgoingPieceRequests()
        {
            // Prevents this task to be processed concurrently
            if (Interlocked.CompareExchange(ref isProcessingOutgoingPieceRequests, 1, 0) != 0)
                return;

            OutgoingPieceRequest request;
            while (outgoingPieceRequests.TryDequeue(out request))
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
                    if (!await request.Peer.RequestPieceData(pieceIndex, blockIndex))
                    {
                        block.RemoveRequestPeer(peer);
                        pendingOutgoingPieceRequests.Remove(request);
                    }
                }
                catch (Exception ex)
                {
                    Log.LogErrorException(ex);
                    block.RemoveRequestPeer(peer);
                    pendingOutgoingPieceRequests.Remove(request);
                }
            }

            int pieceRequestTimeout = Preferences.Peer.PieceRequestTimeout;
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

            // Reset the flag that we are currently processing
            Interlocked.Exchange(ref isProcessingOutgoingPieceRequests, 0);
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

        private void CancelOutgoingPieceRequestsWithPeer(Peer peer)
        {
            foreach (var request in outgoingPieceRequests)
            {
                if (request.Peer == peer)
                {
                    request.Cancel();
                }
            }
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
        private void VerifyIntegrity()
        {
            if (isVerifyingIntegrity || hasVerifiedIntegrity || isStarted)
                return;

            isVerifyingIntegrity = true;
            Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        await VerifyPiece(i);
                    }

                    isSeeding = HasDownloadedAllPieces();
                    hasVerifiedIntegrity = true;

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
        private void UpdatePeers()
        {
            lock (peersSyncObj)
            {
                foreach (var peer in peers)
                {
                    peer.Update();
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
            // TODO: Implement!
        }
        #endregion

        #region Peers
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
            int removedCount = pendingOutgoingPieceRequests.RemoveAny((request) => request.Equals(peer, pieceIndex, blockIndex), 1);
            if (removedCount == 0)
                return;

            // Cancel other requests for the same block on other peers
            CancelOutgoingPieceRequestsForBlock(pieceIndex, blockIndex);

            // Write the data and verify the piece on another thread
            Task.Run(async () =>
            {
                long offset = piece.Offset + (blockIndex * blockSize);
                await WriteData(offset, data, 0, data.Length);
                block.IsDownloaded = true;
                block.RemoveRequestPeer(peer);

                if (piece.HasDownloadedAllBlocks())
                {
                    await VerifyPiece(piece.Index);
                }
            }).CatchExceptions();
        }
        #endregion

        #region Statistics
        internal void IncreaseSessionDownloadedBytes(long amount)
        {
            Interlocked.Add(ref sessionDownloadedBytes, amount);
        }

        internal void IncreaseSessionUploadedBytes(long amount)
        {
            Interlocked.Add(ref sessionUploadedBytes, amount);
        }
        #endregion

        #region Read & Write
        internal async Task<int> ReadData(long torrentOffset, byte[] buffer, int bufferOffset, int count)
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

        internal async Task WriteData(long torrentOffset, byte[] buffer, int bufferOffset, int count)
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
