using System;
using System.Windows.Data;

namespace TVTComment.Views.Converters
{
    class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";
            try
            {
                return Enum.GetName(value.GetType(), value) ?? "";
            }
            catch (ArgumentException)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not string valueString)
                return Binding.DoNothing;

            try
            {
                return Enum.Parse(targetType, valueString);
            }
            catch (ArgumentException)
            {
                return Binding.DoNothing;
            }
        }
    }
}
