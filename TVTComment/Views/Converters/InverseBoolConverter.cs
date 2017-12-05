using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TVTComment.Views.Converters
{
    class InverseBoolConverter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            bool val = (bool)value;
            return !val;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            bool val = (bool)value;
            return !val;
        }
    }
}
