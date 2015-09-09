using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlasticWonderland
{
    public abstract class DataTemplateSelector : ContentControl
    {
        private Dictionary<string, DataTemplate> dataTemplates;

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            ContentTemplate = SelectTemplate(newContent, this);
        }

        public virtual DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return null;
        }

        protected DataTemplate GetDataTemplate(string templateName, DependencyObject container)
        {
            DataTemplate dataTemplate = null;

            // Try to find in the dictionary
            if (dataTemplates != null && dataTemplates.ContainsKey(templateName))
            {
                dataTemplate = dataTemplates[templateName];
            }
            else
            {
                // Try to find the resurce in the App
                dataTemplate = App.Current.Resources[templateName] as DataTemplate;

                // If not found, then try to find it on the page resources
                PhoneApplicationPage page = null;
                while (container != null && dataTemplate == null)
                {
                    container = VisualTreeHelper.GetParent(container);
                    page = container as PhoneApplicationPage;
                    if (page != null)
                    {
                        dataTemplate = page.Resources[templateName] as DataTemplate;
                    }
                }

                // Instantiate the dictionary if null
                if (dataTemplates == null)
                {
                    dataTemplates = new Dictionary<string, DataTemplate>();
                }

                // save find result to the dictionary, even if null
                dataTemplates[templateName] = dataTemplate;
            }

            return dataTemplate;
        }
    }
}
