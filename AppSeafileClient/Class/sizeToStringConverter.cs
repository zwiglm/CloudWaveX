using System;
using System.Globalization;
using System.Windows.Data;
using PlasticWonderland.Domain;

namespace PlasticWonderland.Class
{
    public class sizeToStringConverter : IValueConverter
    {

        // This converts the value object to the string to display.
        // This will work with most simple types.
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string d = CloudHelper.bytesToString(long.Parse(value.ToString()));
            return d;
        }

        // No need to implement converting back on a one-way binding
        public object ConvertBack(object value, Type targetType,
        object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
