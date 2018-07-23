using System;

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// An attribute placed on a preference section field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class PreferenceSectionAttribute : Attribute
    {
        private readonly string title;
        private int order = 0;

        /// <summary>
        /// Gets the title of this section.
        /// </summary>
        public string Title
        {
            get { return title; }
        }

        /// <summary>
        /// Gets or sets the order of this section.
        /// Lower comes before higher.
        /// </summary>
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        /// <summary>
        /// Creates a new preference section attribute.
        /// </summary>
        /// <param name="title">The section title.</param>
        public PreferenceSectionAttribute(string title)
        {
            this.title = title;
        }
    }
}
