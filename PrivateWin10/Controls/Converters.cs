using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrivateWin10.Controls
{
    [ValueConversion(typeof(UInt64), typeof(String))]
    public class SizeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((UInt64)value) == 0)
                return "";
            return FileOps.FormatSize((UInt64)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(UInt64), typeof(String))]
    public class RateConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(((UInt64)value) == 0)
                return "";
            return FileOps.FormatSize((UInt64)value) + "/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(DateTime), typeof(String))]
    public class DateConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || ((DateTime)value) == DateTime.MinValue)
                return "";
            return ((DateTime)value).ToString("HH:mm:ss dd.MM.yyyy");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
