using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TorrentSwifter.UI.Pages
{
    /// <summary>
    /// The TorrentSwifter main menu page.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainMenuPage : ContentPage
    {
        #region Fields
        private readonly MainMenuPageViewModel viewModel;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the count of menu items in this menu.
        /// </summary>
        public int MenuItemCount
        {
            get { return viewModel.MenuItems.Count; }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a menu item has been selected.
        /// </summary>
        public event EventHandler<SelectedItemChangedEventArgs> MenuItemSelected;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new main menu page.
        /// </summary>
        public MainMenuPage()
        {
            InitializeComponent();

            viewModel = new MainMenuPageViewModel();
            BindingContext = viewModel;
            MenuItemsListView.ItemSelected += OnMenuItemSelected;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a new menu item to this menu.
        /// </summary>
        /// <param name="menuItem">The menu item to add.</param>
        public void AddMenuItem(MainMenuItem menuItem)
        {
            if (menuItem == null)
                throw new ArgumentNullException("menuItem");

            bool isIDUsed = viewModel.MenuItems.Where(x => string.Equals(x.ID, menuItem.ID)).Any();
            if (isIDUsed)
                throw new ArgumentException(string.Format("The menu item ID is already used by another item: {0}", menuItem.ID), "menuItem");

            viewModel.MenuItems.Add(menuItem);
        }

        /// <summary>
        /// Removes a menu item from this menu.
        /// </summary>
        /// <param name="menuItem">The menu item to remove.</param>
        /// <returns>If successfully removed.</returns>
        public bool RemoveMenuItem(MainMenuItem menuItem)
        {
            if (menuItem == null)
                throw new ArgumentNullException("menuItem");

            return viewModel.MenuItems.Remove(menuItem);
        }

        /// <summary>
        /// Returns a menu item in this meny by its index.
        /// </summary>
        /// <param name="index">The menu item index.</param>
        /// <returns>The menu item.</returns>
        public MainMenuItem GetMenuItem(int index)
        {
            if (index < 0 || index >= viewModel.MenuItems.Count)
                throw new ArgumentOutOfRangeException("index");

            return viewModel.MenuItems[0];
        }

        /// <summary>
        /// Returns a menu item in this meny by its ID.
        /// </summary>
        /// <param name="id">The menu item ID.</param>
        /// <returns>The menu item, if any.</returns>
        public MainMenuItem GetMenuItem(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            return viewModel.MenuItems.FirstOrDefault(x => string.Equals(x.ID, id));
        }

        /// <summary>
        /// Selects a specific menu item.
        /// </summary>
        /// <param name="menuItem">The menu item to select.</param>
        public void SelectMenuItem(MainMenuItem menuItem)
        {
            MenuItemsListView.SelectedItem = menuItem;
        }
        #endregion

        #region Private Methods
        private void OnMenuItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var item = e.SelectedItem as MainMenuItem;
            if (item == null)
                return;

            MenuItemSelected?.Invoke(this, e);
        }
        #endregion

        #region Main Menu Page View Model
        class MainMenuPageViewModel
        {
            private readonly ObservableCollection<MainMenuItem> menuItems;

            public ObservableCollection<MainMenuItem> MenuItems
            {
                get { return menuItems; }
            }
            
            public MainMenuPageViewModel()
            {
                menuItems = new ObservableCollection<MainMenuItem>();
            }
        }
        #endregion
    }
}
