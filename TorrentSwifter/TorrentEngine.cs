using System;
using System.Collections.Concurrent;
using System.Threading;
using TorrentSwifter.Logging;
using TorrentSwifter.Managers;
using TorrentSwifter.Peers;
using TorrentSwifter.Tasks;
using TorrentSwifter.Torrents;

namespace TorrentSwifter
{
    /// <summary>
    /// The engine for the entire TorrentSwifter library.
    /// </summary>
    public static class TorrentEngine
    {
        #region Fields
        private static bool isInitialized = false;
        private static bool isStopping = false;
        private static Thread engineThread = null;

        private static ConcurrentQueue<IWorkTask> workQueue = new ConcurrentQueue<IWorkTask>();
        private static AutoResetEvent workQueueResetEvent = new AutoResetEvent(false);
        #endregion

        #region Properties
        /// <summary>
        /// Gets if the torrent engine has been initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get { return isInitialized; }
        }
        #endregion

        #region Public Methods
        #region Initialize & Uninitialize
        /// <summary>
        /// Initializes the torrent engine.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            Log.LogInfo("[Engine] Starting up TorrentEngine.");

            if (engineThread != null)
            {
                engineThread.Join();
                engineThread = null;
            }

            isStopping = false;
            workQueueResetEvent.Set();

            DiskManager.Initialize();
            PeerListener.StartListening();
            LocalPeerListener.StartListening();
            LocalPeerDiscovery.Initialize();

            engineThread = new Thread(EngineLoop);
            engineThread.Priority = ThreadPriority.Normal;
            engineThread.Name = "TorrentEngineThread";
            engineThread.Start();
        }

        /// <summary>
        /// Uninitializes the torrent engine.
        /// </summary>
        public static void Uninitialize()
        {
            if (!isInitialized)
                return;

            Log.LogInfo("[Engine] Shutting down TorrentEngine.");

            isInitialized = false;
            isStopping = true;
            workQueueResetEvent.Set();

            TorrentRegistry.StopAllActiveTorrents();
            PeerListener.StopListening();
            LocalPeerListener.StopListening();
            LocalPeerDiscovery.Uninitialize();
            DiskManager.Uninitialize();

            if (engineThread != null)
            {
                engineThread.Join();
                engineThread = null;
            }
        }
        #endregion

        #region Queue Work
        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="task">The work task.</param>
        public static void QueueWork(IWorkTask task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            workQueue.Enqueue(task);
            workQueueResetEvent.Set();
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public static void QueueWork(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTask(action);
            QueueWork(task);
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="arg">The argument.</param>
        public static void QueueWork<T>(Action<T> action, T arg)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTask<T>(action, arg);
            QueueWork(task);
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second.</param>
        public static void QueueWork<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTask<T1, T2>(action, arg1, arg2);
            QueueWork(task);
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        public static void QueueWork<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTask<T1, T2, T3>(action, arg1, arg2, arg3);
            QueueWork(task);
        }
        #endregion
        #endregion

        #region Private Methods
        private static void EngineLoop()
        {
            while (!isStopping)
            {
                try
                {
                    Stats.Update();

                    IWorkTask workTask;
                    while (!isStopping && workQueue.TryDequeue(out workTask))
                    {
                        try
                        {
                            workTask.Execute();
                        }
                        catch (Exception ex)
                        {
                            Log.LogErrorException(ex);
                        }
                    }

                    workQueueResetEvent.WaitOne(1000);
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
