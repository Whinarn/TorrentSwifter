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

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TorrentSwifter.Logging;
using TorrentSwifter.Preferences;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Managers
{
    /// <summary>
    /// A manager of I/O disk (or solid-state) reading and writing.
    /// </summary>
    internal static class DiskManager
    {
        #region Delegates
        /// <summary>
        /// A callback for disk reads.
        /// </summary>
        /// <param name="success">If the disk read was successful.</param>
        /// <param name="exception">Any exception that occured if not successful.</param>
        /// <param name="readCount">The count of bytes read.</param>
        public delegate void DiskReadCallback(bool success, Exception exception, int readCount);

        /// <summary>
        /// A callback for disk writes.
        /// </summary>
        /// <param name="success">If the disk write was successful.</param>
        /// <param name="exception">Any exception that occured if not successful.</param>
        public delegate void DiskWriteCallback(bool success, Exception exception);
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
        /// <summary>
        /// Gets the current count of queued reads.
        /// </summary>
        public static int QueuedReads
        {
            get { return queuedReads.Count; }
        }

        /// <summary>
        /// Gets the current count of queued writes.
        /// </summary>
        public static int QueuedWrites
        {
            get { return queuedWrites.Count; }
        }
        #endregion

        #region Public Methods
        #region Queueing
        /// <summary>
        /// Queues a read from a specific torrent at an offset.
        /// </summary>
        /// <param name="torrent">The torrent to read from.</param>
        /// <param name="torrentOffset">The offset within the torrent.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="bufferOffset">The offset within the buffer to start writing into.</param>
        /// <param name="readLength">The length of bytes to read.</param>
        /// <param name="callback">The callback with the result.</param>
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
                callback.Invoke(true, null, 0);
                return;
            }

            var newEntry = new DiskReadEntry(torrent, torrentOffset, buffer, bufferOffset, readLength, callback);
            queuedReads.Enqueue(newEntry);
            readResetEvent.Set();
        }

        /// <summary>
        /// Queues a write to a specific torrent at an offset.
        /// </summary>
        /// <param name="torrent">The torrent to write to.</param>
        /// <param name="torrentOffset">The offset within the torrent.</param>
        /// <param name="data">The data of bytes to write.</param>
        /// <param name="callback">The callback with the result.</param>
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
                    callback.Invoke(true, null);
                }
                return;
            }

            var newEntry = new DiskWriteEntry(torrent, torrentOffset, data, callback);
            queuedWrites.Enqueue(newEntry);
            writeResetEvent.Set();
        }
        #endregion

        #region Async
        /// <summary>
        /// Reads from a specific torrent at an offset asynchronously.
        /// </summary>
        /// <param name="torrent">The torrent to read from.</param>
        /// <param name="torrentOffset">The offset within the torrent.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="bufferOffset">The offset within the buffer to start writing into.</param>
        /// <param name="readLength">The length of bytes to read.</param>
        /// <returns>The asynchronous task, with the count of bytes read as a result.</returns>
        public static Task<int> ReadAsync(Torrent torrent, long torrentOffset, byte[] buffer, int bufferOffset, int readLength)
        {
            var taskCompletionSource = new TaskCompletionSource<int>();
            DiskManager.QueueRead(torrent, torrentOffset, buffer, bufferOffset, readLength, (success, exception, readCount) =>
            {
                if (success)
                {
                    taskCompletionSource.SetResult(readCount);
                }
                else
                {
                    taskCompletionSource.SetException(exception);
                }
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Reads from a specific torrent at an offset asynchronously.
        /// </summary>
        /// <param name="torrent">The torrent to read from.</param>
        /// <param name="torrentOffset">The offset within the torrent.</param>
        /// <param name="data">The data of bytes to write.</param>
        /// <returns>The asynchronous task.</returns>
        public static Task WriteAsync(Torrent torrent, long torrentOffset, byte[] data)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            DiskManager.QueueWrite(torrent, torrentOffset, data, (success, exception) =>
            {
                if (success)
                {
                    taskCompletionSource.SetResult(true);
                }
                else
                {
                    taskCompletionSource.SetException(exception);
                }
            });
            return taskCompletionSource.Task;
        }
        #endregion
        #endregion

        #region Internal Methods
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

                        try
                        {
                            int readByteCount = torrent.ReadData(torrentOffset, buffer, bufferOffset, readLength);

                            try
                            {
                                callback.Invoke(true, null, readByteCount);
                            }
                            catch (Exception ex)
                            {
                                Log.LogErrorException(ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                callback.Invoke(false, ex, 0);
                            }
                            catch (Exception ex2)
                            {
                                Log.LogErrorException(ex2);
                            }
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

                        try
                        {
                            torrent.WriteData(torrentOffset, data, 0, data.Length);

                            if (callback != null)
                            {
                                try
                                {
                                    callback.Invoke(true, null);
                                }
                                catch (Exception ex)
                                {
                                    Log.LogErrorException(ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (callback != null)
                            {
                                try
                                {
                                    callback.Invoke(false, ex);
                                }
                                catch (Exception ex2)
                                {
                                    Log.LogErrorException(ex2);
                                }
                            }
                            else
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
