using System;
using System.Collections.Concurrent;
using System.Threading;
using TorrentSwifter.Logging;
using TorrentSwifter.Preferences;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Managers
{
    internal static class DiskManager
    {
        #region Delegates
        /// <summary>
        /// A callback for disk reads.
        /// </summary>
        /// <param name="success">If the disk read was successful.</param>
        /// <param name="readCount">The count of bytes read.</param>
        public delegate void DiskReadCallback(bool success, int readCount);

        /// <summary>
        /// A callback for disk writes.
        /// </summary>
        /// <param name="success">If the disk write was successful.</param>
        public delegate void DiskWriteCallback(bool success);
        #endregion

        #region Structs
        private struct DiskReadEntry
        {
            public readonly Torrent torrent;
            public readonly long torrentOffset;
            public readonly byte[] buffer;
            public readonly int bufferOffset;
            public readonly int readLength;
            public readonly DiskReadCallback callback;

            public DiskReadEntry(Torrent torrent, long torrentOffset, byte[] buffer, int bufferOffset, int readLength, DiskReadCallback callback)
            {
                this.torrent = torrent;
                this.torrentOffset = torrentOffset;
                this.buffer = buffer;
                this.bufferOffset = bufferOffset;
                this.readLength = readLength;
                this.callback = callback;
            }
        }

        private struct DiskWriteEntry
        {
            public readonly Torrent torrent;
            public readonly long torrentOffset;
            public readonly byte[] data;
            public readonly DiskWriteCallback callback;

            public DiskWriteEntry(Torrent torrent, long torrentOffset, byte[] data, DiskWriteCallback callback)
            {
                this.torrent = torrent;
                this.torrentOffset = torrentOffset;
                this.data = data;
                this.callback = callback;
            }
        }
        #endregion

        #region Fields
        private static bool isRunning = false;
        private static Thread[] readThreads = null;
        private static Thread[] writeThreads = null;

        private static ConcurrentQueue<DiskReadEntry> queuedReads = new ConcurrentQueue<DiskReadEntry>();
        private static ConcurrentQueue<DiskWriteEntry> queuedWrites = new ConcurrentQueue<DiskWriteEntry>();

        private static AutoResetEvent readResetEvent = new AutoResetEvent(false);
        private static AutoResetEvent writeResetEvent = new AutoResetEvent(false);
        #endregion

        #region Properties
        public static int QueuedReads
        {
            get { return queuedReads.Count; }
        }

        public static int QueuedWrites
        {
            get { return queuedWrites.Count; }
        }
        #endregion

        #region Public Methods
        public static void QueueRead(Torrent torrent, long torrentOffset, byte[] buffer, int bufferOffset, int readLength, DiskReadCallback callback)
        {
            if (torrent == null)
                throw new ArgumentNullException("torrent");
            else if (torrentOffset < 0L)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (buffer == null)
                throw new ArgumentNullException("buffer");
            else if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                throw new ArgumentOutOfRangeException("bufferOffset");
            else if (readLength < 0 || (bufferOffset + readLength) > buffer.Length)
                throw new ArgumentOutOfRangeException("readLength");
            else if (callback == null)
                throw new ArgumentNullException("callback");

            if (readLength == 0)
            {
                callback.Invoke(true, 0);
                return;
            }

            var newEntry = new DiskReadEntry(torrent, torrentOffset, buffer, bufferOffset, readLength, callback);
            queuedReads.Enqueue(newEntry);
            readResetEvent.Set();
        }

        public static void QueueWrite(Torrent torrent, long torrentOffset, byte[] data, DiskWriteCallback callback = null)
        {
            if (torrent == null)
                throw new ArgumentNullException("torrent");
            else if (torrentOffset < 0L)
                throw new ArgumentOutOfRangeException("torrentOffset");
            else if (data == null)
                throw new ArgumentNullException("data");

            if (data.Length == 0)
            {
                if (callback != null)
                {
                    callback.Invoke(true);
                }
                return;
            }

            var newEntry = new DiskWriteEntry(torrent, torrentOffset, data, callback);
            queuedWrites.Enqueue(newEntry);
            writeResetEvent.Set();
        }
        #endregion

        #region Internal
        internal static void Initialize()
        {
            if (isRunning)
                return;

            isRunning = true;
            readResetEvent.Set();
            writeResetEvent.Set();

            int readThreadCount = Prefs.Disk.MaxConcurrentReads;
            int writeThreadCount = Prefs.Disk.MaxConcurrentWrites;

            if (readThreadCount < 1)
                readThreadCount = 1;
            if (writeThreadCount < 1)
                writeThreadCount = 1;

            readThreads = new Thread[readThreadCount];
            for (int i = 0; i < readThreadCount; i++)
            {
                var thread = new Thread(ProcessReads);
                readThreads[i] = thread;
                thread.Priority = ThreadPriority.Normal;
                thread.Name = string.Format("TorrentDiskRead #{0}", (i + 1));
                thread.Start();
            }

            writeThreads = new Thread[writeThreadCount];
            for (int i = 0; i < writeThreadCount; i++)
            {
                var thread = new Thread(ProcessWrites);
                writeThreads[i] = thread;
                thread.Priority = ThreadPriority.Normal;
                thread.Name = string.Format("TorrentDiskWrite #{0}", (i + 1));
                thread.Start();
            }
        }

        internal static void Uninitialize()
        {
            if (!isRunning)
                return;

            isRunning = false;
            readResetEvent.Set();
            writeResetEvent.Set();

            if (readThreads != null)
            {
                for (int i = 0; i < readThreads.Length; i++)
                {
                    readThreads[i].Join();
                }
                readThreads = null;
            }

            if (writeThreads != null)
            {
                for (int i = 0; i < writeThreads.Length; i++)
                {
                    writeThreads[i].Join();
                }
                writeThreads = null;
            }
        }
        #endregion

        #region Private Methods
        private static void ProcessReads()
        {
            while (isRunning)
            {
                try
                {
                    DiskReadEntry readEntry;
                    while (queuedReads.TryDequeue(out readEntry))
                    {
                        var torrent = readEntry.torrent;
                        long torrentOffset = readEntry.torrentOffset;
                        var buffer = readEntry.buffer;
                        int bufferOffset = readEntry.bufferOffset;
                        int readLength = readEntry.readLength;
                        var callback = readEntry.callback;

                        bool success = false;
                        int readByteCount = 0;
                        try
                        {
                            readByteCount = torrent.ReadData(torrentOffset, buffer, bufferOffset, readLength);
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            success = false;
                            readByteCount = 0;
                            Log.LogErrorException(ex);
                        }

                        try
                        {
                            callback.Invoke(success, readByteCount);
                        }
                        catch (Exception ex)
                        {
                            Log.LogErrorException(ex);
                        }
                    }

                    readResetEvent.WaitOne(1000);
                }
                catch (Exception ex)
                {
                    Log.LogErrorException(ex);
                }
            }
        }

        private static void ProcessWrites()
        {
            while (isRunning)
            {
                try
                {
                    DiskWriteEntry writeEntry;
                    while (queuedWrites.TryDequeue(out writeEntry))
                    {
                        var torrent = writeEntry.torrent;
                        long torrentOffset = writeEntry.torrentOffset;
                        var data = writeEntry.data;
                        var callback = writeEntry.callback;

                        bool success = false;
                        try
                        {
                            torrent.WriteData(torrentOffset, data, 0, data.Length);
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            success = false;
                            Log.LogErrorException(ex);
                        }

                        if (callback != null)
                        {
                            try
                            {
                                callback.Invoke(success);
                            }
                            catch (Exception ex)
                            {
                                Log.LogErrorException(ex);
                            }
                        }
                    }

                    writeResetEvent.WaitOne(1000);
                }
                catch (Exception ex)
                {
                    Log.LogErrorException(ex);
                }
            }
        }
        #endregion
    }
}
