using System;
using System.Globalization;
using System.Windows.Data;

namespace CSharpControls.Wpf.Converters
{
    public class ToggleSwitchOffsetConverter : IValueConverter
    {
        public bool IsReversed { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var width = (double)value;
            return width > 20D ? IsReversed ? -((width / 2) - 10) : (width / 2) - 10 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}