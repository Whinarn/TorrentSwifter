using System;
using Xamarin.Forms;
using TorrentSwifter.UI.Pages;

namespace TorrentSwifter.UI
{
    /// <summary>
    /// The TorrentSwifter UI application.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Creates a new TorrentSwifter UI application.
        /// </summary>
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        /// <summary>
        /// The application just started.
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
        }

        /// <summary>
        /// The application has entered a sleep.
        /// </summary>
        protected override void OnSleep()
        {
            base.OnSleep();
        }

        /// <summary>
        /// The application has been resumed after sleeping.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
        }

        /// <summary>
        /// The user has initiated an application link request.
        /// </summary>
        /// <param name="uri">The request URI.</param>
        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
        }
    }
}
