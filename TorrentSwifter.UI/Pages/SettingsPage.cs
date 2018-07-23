using System;
using System.Collections.Generic;
using System.Reflection;
using Xamarin.Forms;
using TorrentSwifter.Preferences;

namespace TorrentSwifter.UI.Pages
{
    /// <summary>
    /// The TorrentSwifter settings page.
    /// </summary>
	public class SettingsPage : ContentPage
	{
        /// <summary>
        /// Creates a new settings page.
        /// </summary>
		public SettingsPage()
		{
            var tableRoot = new TableRoot("Settings");
            CreateSections(tableRoot);

            var tableView = new TableView(tableRoot);
            tableView.Intent = TableIntent.Settings;
            Content = tableView;
		}

        private static void CreateSections(TableRoot tableRoot)
        {
            var prefsType = typeof(Prefs);
            var prefsFields = prefsType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            // Gather the sections
            var sectionList = new List<SettingsSection>(prefsFields.Length);
            foreach (var fieldInfo in prefsFields)
            {
                var sectionAttribute = fieldInfo.GetCustomAttribute<PreferenceSectionAttribute>(false);
                if (sectionAttribute == null)
                    continue;

                object sectionObj = fieldInfo.GetValue(null);
                if (sectionObj == null)
                    continue;

                sectionList.Add(new SettingsSection(sectionAttribute.Title, sectionAttribute.Order, sectionObj));
            }

            // Sort the sections
            sectionList.Sort((x, y) => x.Order.CompareTo(y.Order));

            // Create the sections
            foreach (var section in sectionList)
            {
                var newSection = CreateSection(section.Title, section.SectionObj);
                if (newSection != null && newSection.Count > 0)
                {
                    tableRoot.Add(newSection);
                }
            }
        }

        private static TableSection CreateSection(string title, object sectionObj)
        {
            var sectionType = sectionObj.GetType();
            var sectionFields = sectionType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var sectionProperties = sectionType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var section = new TableSection(title);
            var fieldList = new List<SettingsField>(sectionFields.Length + sectionProperties.Length);

            // Gather all fields
            foreach (var fieldInfo in sectionFields)
            {
                var itemAttribute = fieldInfo.GetCustomAttribute<PreferenceItemAttribute>(false);
                if (itemAttribute == null)
                    continue;

                var field = new SettingsField(itemAttribute, sectionObj, fieldInfo);
                fieldList.Add(field);
            }

            // Gather all properties
            foreach (var propertyInfo in sectionProperties)
            {
                var itemAttribute = propertyInfo.GetCustomAttribute<PreferenceItemAttribute>(false);
                if (itemAttribute == null)
                    continue;

                var field = new SettingsField(itemAttribute, sectionObj, propertyInfo);
                fieldList.Add(field);
            }

            // Sort the fields by order
            fieldList.Sort((x, y) => x.Order.CompareTo(y.Order));

            // Create the cells
            foreach (var field in fieldList)
            {
                var cell = CreateCell(field);
                if (cell == null)
                    continue;

                section.Add(cell);
            }

            return section;
        }

        private static Cell CreateCell(SettingsField field)
        {
            var fieldType = field.FieldType;
            if (fieldType.Equals(typeof(bool)))
            {
                return CreateToggleCell(field);
            }
            else if (fieldType.Equals(typeof(int)))
            {
                return CreateIntegerInputView(field, int.MinValue, int.MaxValue);
            }
            else if (fieldType.Equals(typeof(long)))
            {
                return CreateIntegerInputView(field, long.MinValue, long.MaxValue);
            }
            else
            {
                return null;
            }
        }

        private static Cell CreateToggleCell(SettingsField field)
        {
            bool isOn = (bool)field.GetValue();

            if (Device.Idiom == TargetIdiom.Desktop)
            {
                var viewCell = new ViewCell();
                var stackLayout = new StackLayout();
                stackLayout.Orientation = StackOrientation.Horizontal;

                var fieldLabel = new Label();
                fieldLabel.Text = field.Label;

                var fieldSwitch = new Switch();
                fieldSwitch.HorizontalOptions = LayoutOptions.FillAndExpand;
                fieldSwitch.IsToggled = isOn;
                fieldSwitch.Toggled += (sender, e) =>
                {
                    field.SetValue(e.Value);
                };

                stackLayout.Children.Add(fieldLabel);
                stackLayout.Children.Add(fieldSwitch);
                viewCell.View = stackLayout;
                return viewCell;
            }
            else
            {
                var switchCell = new SwitchCell();
                switchCell.Text = field.Label;
                switchCell.On = isOn;
                switchCell.OnChanged += (sender, e) =>
                {
                    field.SetValue(e.Value);
                };
                return switchCell;
            }
        }

        private static Cell CreateIntegerInputView(SettingsField field, long minimumValue, long maximumValue)
        {
            string valueText = field.GetValue().ToString();

            if (Device.Idiom == TargetIdiom.Desktop)
            {
                var viewCell = new ViewCell();
                var stackLayout = new StackLayout();
                stackLayout.Orientation = StackOrientation.Horizontal;

                var fieldLabel = new Label();
                fieldLabel.Text = field.Label;

                var fieldEntry = new Entry();
                fieldEntry.HorizontalOptions = LayoutOptions.FillAndExpand;
                fieldEntry.Text = valueText;
                fieldEntry.IsTextPredictionEnabled = false;
                fieldEntry.Keyboard = Keyboard.Numeric;
                fieldEntry.TextChanged += (sender, e) =>
                {
                    long value;
                    if (long.TryParse(e.NewTextValue, out value))
                    {
                        if (value < minimumValue)
                            value = minimumValue;
                        else if (value > maximumValue)
                            value = maximumValue;

                        object convertedValue = Convert.ChangeType(value, field.FieldType);
                        field.SetValue(convertedValue);
                    }
                };

                stackLayout.Children.Add(fieldLabel);
                stackLayout.Children.Add(fieldEntry);
                viewCell.View = stackLayout;
                return viewCell;
            }
            else
            {
                var entryCell = new EntryCell();
                entryCell.Label = field.Label;
                entryCell.Keyboard = Keyboard.Numeric;
                entryCell.Placeholder = field.Attribute.Placeholder ?? string.Empty;
                entryCell.Text = valueText;
                entryCell.HorizontalTextAlignment = TextAlignment.Start;

                entryCell.Completed += (sender, e) =>
                {
                    long value;
                    if (long.TryParse(entryCell.Text, out value))
                    {
                        if (value < minimumValue)
                            value = minimumValue;
                        else if (value > maximumValue)
                            value = maximumValue;

                        object convertedValue = Convert.ChangeType(value, field.FieldType);
                        field.SetValue(convertedValue);
                    }
                };
                return entryCell;
            }
        }

        private class SettingsSection
        {
            private readonly string title;
            private readonly int order;
            private readonly object sectionObj;

            public string Title
            {
                get { return title; }
            }

            public int Order
            {
                get { return order; }
            }

            public object SectionObj
            {
                get { return sectionObj; }
            }

            public SettingsSection(string title, int order, object sectionObj)
            {
                this.title = title;
                this.order = order;
                this.sectionObj = sectionObj;
            }
        }

        private class SettingsField
        {
            private readonly PreferenceItemAttribute attribute;
            private readonly object instance;
            private readonly FieldInfo fieldInfo;
            private readonly PropertyInfo propertyInfo;

            public string Label
            {
                get { return attribute.Label; }
            }

            public int Order
            {
                get { return attribute.Order; }
            }

            public PreferenceItemAttribute Attribute
            {
                get { return attribute; }
            }

            public Type FieldType
            {
                get
                {
                    if (fieldInfo != null)
                        return fieldInfo.FieldType;
                    else if (propertyInfo != null)
                        return propertyInfo.PropertyType;
                    else
                        throw new NotSupportedException();
                }
            }

            public SettingsField(PreferenceItemAttribute attribute, object instance, FieldInfo fieldInfo)
            {
                this.attribute = attribute;
                this.instance = instance;
                this.fieldInfo = fieldInfo;
                this.propertyInfo = null;
            }

            public SettingsField(PreferenceItemAttribute attribute, object instance, PropertyInfo propertyInfo)
            {
                this.attribute = attribute;
                this.instance = instance;
                this.fieldInfo = null;
                this.propertyInfo = propertyInfo;
            }

            public object GetValue()
            {
                if (fieldInfo != null)
                    return fieldInfo.GetValue(instance);
                else if (propertyInfo != null)
                    return propertyInfo.GetValue(instance);
                else
                    throw new NotSupportedException();
            }

            public void SetValue(object value)
            {
                if (fieldInfo != null)
                    fieldInfo.SetValue(instance, value);
                else if (propertyInfo != null)
                    propertyInfo.SetValue(instance, value);
                else
                    throw new NotSupportedException();
            }
        }
    }
}