using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TorrentSwifter.UI.Pages
{
    /// <summary>
    /// The TorrentSwifter main page.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : MasterDetailPage
    {
        /// <summary>
        /// Creates a new main page.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            MenuPage.MenuItemSelected += OnMenuItemSelected;

            var torrentsMenuItem = new MainMenuPageItem("torrents", "Torrents", new TorrentListPage());
            var settingsMenuItem = new MainMenuPageItem("settings", "Settings", new SettingsPage());
            var quitMenuItem = new MainMenuActionItem("quit", "Quit", OnRequestApplicationQuit);

            MenuPage.AddMenuItem(torrentsMenuItem);
            MenuPage.AddMenuItem(settingsMenuItem);
            MenuPage.AddMenuItem(quitMenuItem);

            MenuPage.SelectMenuItem(torrentsMenuItem);
            IsPresented = true;
        }

        private void OnMenuItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var item = (e.SelectedItem as MainMenuItem);
            if (item == null)
                return;

            MainMenuPageItem pageItem;
            MainMenuActionItem actionItem;
            if ((pageItem = (item as MainMenuPageItem)) != null)
            {
                Detail = pageItem.Page;
                if (Device.Idiom != TargetIdiom.Desktop)
                {
                    IsPresented = false;
                }
            }
            else if ((actionItem = (item as MainMenuActionItem)) != null)
            {
                var action = actionItem.Action;
                action?.Invoke();
            }
        }

        private void OnRequestApplicationQuit()
        {
            Application.Current.Quit();
        }
    }
}
