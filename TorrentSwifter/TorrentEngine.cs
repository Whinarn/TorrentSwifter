using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
        private static Task engineTask = null;
        private static CancellationTokenSource engineCancellationTokenSource = null;

        private static ConcurrentQueue<IWorkTask> workQueue = new ConcurrentQueue<IWorkTask>();
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

            if (engineCancellationTokenSource != null)
            {
                engineCancellationTokenSource.Cancel();
            }
            if (engineTask != null)
            {
                engineTask.Wait();
                engineTask = null;
            }

            PeerListener.StartListening();

            engineCancellationTokenSource = new CancellationTokenSource();
            engineTask = EngineLoop(engineCancellationTokenSource.Token);
            engineTask.CatchExceptions();
        }

        /// <summary>
        /// Uninitializes the torrent engine.
        /// </summary>
        public static void Uninitialize()
        {
            if (!isInitialized)
                return;

            isInitialized = false;

            TorrentRegistry.StopAllActiveTorrents();
            PeerListener.StopListening();

            if (engineCancellationTokenSource != null)
            {
                engineCancellationTokenSource.Cancel();
            }
            if (engineTask != null)
            {
                engineTask.Wait();
                engineTask = null;
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

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public static void QueueWork(Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTaskAsync(action);
            QueueWork(task);
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="arg">The argument.</param>
        public static void QueueWork<T>(Func<T, Task> action, T arg)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTaskAsync<T>(action, arg);
            QueueWork(task);
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second.</param>
        public static void QueueWork<T1, T2>(Func<T1, T2, Task> action, T1 arg1, T2 arg2)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTaskAsync<T1, T2>(action, arg1, arg2);
            QueueWork(task);
        }

        /// <summary>
        /// Queues work that will be run in the engine main thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        public static void QueueWork<T1, T2, T3>(Func<T1, T2, T3, Task> action, T1 arg1, T2 arg2, T3 arg3)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new DelegateTaskAsync<T1, T2, T3>(action, arg1, arg2, arg3);
            QueueWork(task);
        }
        #endregion
        #endregion

        #region Private Methods
        private static async Task EngineLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    IWorkTask workTask;
                    while (!cancellationToken.IsCancellationRequested && workQueue.TryDequeue(out workTask))
                    {
                        try
                        {
                            // Check if the work task supports asynchronous execution
                            var workTaskAsync = workTask as IWorkTaskAsync;
                            if (workTaskAsync != null)
                            {
                                await workTaskAsync.ExecuteAsync();
                            }
                            else
                            {
                                workTask.Execute();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.LogErrorException(ex);
                        }
                    }

                    // TODO: Some async reset event that notifies us when there are more tasks would be better here
                    await Task.Delay(10);
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
