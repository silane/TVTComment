using System;
using System.Globalization;
using System.Windows.Data;

namespace TVTComment.Views.Converters
{
    class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = value as bool?;
            var strs = ((string)parameter).Split('|');
            if (val.HasValue && val.Value) return strs[0];
            else return strs[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
