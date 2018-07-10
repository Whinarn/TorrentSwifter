using System;
using System.Runtime.CompilerServices;
using TorrentSwifter.Logging;

namespace TorrentSwifter
{
    internal static class DelegateExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SafeInvoke(this EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler != null)
            {
                try
                {
                    eventHandler.Invoke(sender, args);
                }
                catch (Exception ex)
                {
                    Logger.LogErrorException(ex);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs args)
        {
            if (eventHandler != null)
            {
                try
                {
                    eventHandler.Invoke(sender, args);
                }
                catch (Exception ex)
                {
                    Logger.LogErrorException(ex);
                }
            }
        }
    }
}
