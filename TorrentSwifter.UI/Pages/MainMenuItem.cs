using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace TorrentSwifter.UI.Pages
{
    /// <summary>
    /// A main menu page item.
    /// </summary>
    public class MainMenuPageItem : MainMenuItem
    {
        private readonly Page page;

        /// <summary>
        /// Gets the menu item page.
        /// </summary>
        public Page Page
        {
            get { return page; }
        }

        /// <summary>
        /// Creates a new main menu page item.
        /// </summary>
        /// <param name="id">The menu item ID.</param>
        /// <param name="title">The menu item title.</param>
        /// <param name="page">The menu item page.</param>
        public MainMenuPageItem(string id, string title, Page page)
            : base(id, title)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            this.page = page;
        }
    }

    /// <summary>
    /// A main menu action item.
    /// </summary>
    public class MainMenuActionItem : MainMenuItem
    {
        private readonly Action action;

        /// <summary>
        /// Gets the menu item action.
        /// </summary>
        public Action Action
        {
            get { return action; }
        }

        /// <summary>
        /// Creates a new main menu action item.
        /// </summary>
        /// <param name="id">The menu item ID.</param>
        /// <param name="title">The menu item title.</param>
        /// <param name="action">The menu item action.</param>
        public MainMenuActionItem(string id, string title, Action action)
            : base(id, title)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            this.action = action;
        }
    }

    /// <summary>
    /// A main menu item.
    /// </summary>
    public abstract class MainMenuItem : INotifyPropertyChanged
    {
        private readonly string id;
        private string title;

        /// <summary>
        /// Gets the menu item ID.
        /// </summary>
        public string ID
        {
            get { return id; }
        }

        /// <summary>
        /// Gets or sets the menu item title.
        /// </summary>
        public string Title
        {
            get { return title; }
            set
            {
                value = value ?? string.Empty;
                if (string.Equals(title, value))
                    return;

                title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Occurs when a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Creates a new main menu item.
        /// </summary>
        /// <param name="id">The menu item ID.</param>
        /// <param name="title">The menu item title.</param>
        public MainMenuItem(string id, string title)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            else if (title == null)
                throw new ArgumentNullException("title");

            this.id = id;
            this.title = title;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
