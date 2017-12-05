using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Drawing;
using System.Windows.Media;

namespace TVTComment.Views.Converters
{
    class ColorToSolidColorBrushConverter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        {
            if (!(value is System.Drawing.Color))
                throw new ArgumentException($"{nameof(value)} must be {nameof(System.Drawing.Color)} type", nameof(value));

            System.Drawing.Color color = (System.Drawing.Color)value;
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        {
            if (!(value is SolidColorBrush))
                throw new ArgumentException($"{nameof(value)} must be {nameof(SolidColorBrush)} type", nameof(value));

            SolidColorBrush brush = (SolidColorBrush)value;
            return System.Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
        }
    }
}
