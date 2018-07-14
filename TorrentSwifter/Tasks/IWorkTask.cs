using System;

namespace TorrentSwifter.Tasks
{
    /// <summary>
    /// A work task.
    /// </summary>
    public interface IWorkTask
    {
        /// <summary>
        /// Executes this work task.
        /// </summary>
        void Execute();
    }
}
