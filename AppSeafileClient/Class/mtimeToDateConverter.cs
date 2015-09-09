using System;
using System.Globalization;
using System.Windows.Data;

namespace PlasticWonderland.Class
{
    public class mtimeToDateConverter : IValueConverter
    {


        // This converts the value object to the string to display.
        // This will work with most simple types.
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string d = conv_Timestamp2Date(value.ToString());
            return d.ToString();
        }

        private string conv_Timestamp2Date(string Timestamp)
        {

            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(double.Parse(Timestamp)).ToLocalTime();

            /*
            //  calculate from Unix epoch
            System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            // add seconds to timestamp
            dateTime = dateTime.AddSeconds(double.Parse(Timestamp));
            string Date = dateTime.ToShortDateString() + ", " + dateTime.ToShortTimeString();
            */

            return dtDateTime.ToString();
        }

        // No need to implement converting back on a one-way binding
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}