using System;
using System.Threading.Tasks;

namespace TorrentSwifter.Tasks
{
    /// <summary>
    /// A helper class to inherit for asynchronous tasks.
    /// </summary>
    public abstract class AsyncWorkTask : IWorkTaskAsync
    {
        /// <summary>
        /// Executes this task.
        /// </summary>
        public void Execute()
        {
            var task = ExecuteAsync();
            task.Wait();
        }

        /// <summary>
        /// Executes this work task asynchronously.
        /// </summary>
        /// <returns>The work task.</returns>
        public abstract Task ExecuteAsync();
    }
}
