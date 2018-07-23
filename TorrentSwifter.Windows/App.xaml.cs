using System;
using System.Windows;

namespace TorrentSwifter.Windows
{
    public partial class App : Application
    {
        public App()
        {
            TorrentEngine.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TorrentEngine.Uninitialize();

            base.OnExit(e);
        }
    }
}
