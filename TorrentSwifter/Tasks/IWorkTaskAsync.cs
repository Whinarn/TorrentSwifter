using System;
using System.Threading.Tasks;

namespace TorrentSwifter.Tasks
{
    /// <summary>
    /// A asynchronous work task.
    /// </summary>
    public interface IWorkTaskAsync : IWorkTask
    {
        /// <summary>
        /// Executes this work task asynchronously.
        /// </summary>
        /// <returns>The work task.</returns>
        Task ExecuteAsync();
    }
}
