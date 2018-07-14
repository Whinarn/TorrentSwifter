using System;
using System.Threading.Tasks;
using TorrentSwifter.Logging;

namespace TorrentSwifter
{
    internal static class TaskExtensions
    {
        public static Task CatchExceptions(this Task task)
        {
            return task.ContinueWith((t) =>
            {
                Log.LogErrorException(t.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
