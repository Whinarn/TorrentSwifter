using System;

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// An attribute placed on a preference item field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class PreferenceItemAttribute : Attribute
    {
        private readonly string label;
        private int order = 0;
        private string placeholder = null;

        /// <summary>
        /// Gets the label of this item.
        /// </summary>
        public string Label
        {
            get { return label; }
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
        /// Gets or sets the placeholder of this item.
        /// </summary>
        public string Placeholder
        {
            get { return placeholder; }
            set { placeholder = value; }
        }

        /// <summary>
        /// Creates a new preference section attribute.
        /// </summary>
        /// <param name="label">The item label.</param>
        public PreferenceItemAttribute(string label)
        {
            this.label = label;
        }
    }
}
