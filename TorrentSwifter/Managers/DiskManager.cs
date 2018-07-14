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
        #region Structs
        private struct DiskWriteEntry
        {
            public readonly Torrent torrent;
            public readonly long torrentOffset;
            public readonly byte[] data;
            public readonly Action<bool> callback;

            public DiskWriteEntry(Torrent torrent, long torrentOffset, byte[] data, Action<bool> callback)
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
        private static Thread[] writeThreads = null;

        private static ConcurrentQueue<DiskWriteEntry> queuedWrites = new ConcurrentQueue<DiskWriteEntry>();

        private static AutoResetEvent writeResetEvent = new AutoResetEvent(false);
        #endregion

        #region Properties
        public static int QueuedWrites
        {
            get { return queuedWrites.Count; }
        }
        #endregion

        #region Public Methods
        public static void QueueWrite(Torrent torrent, long torrentOffset, byte[] data, Action<bool> callback = null)
        {
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
            writeResetEvent.Set();

            int writeThreadCount = Prefs.Disk.MaxConcurrentWrites;
            if (writeThreadCount < 1)
                writeThreadCount = 1;

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
            writeResetEvent.Set();

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
