using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TVTComment.Views.Converters
{
    class ColorToStringConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(!(value is string))
                throw new ArgumentException($"{nameof(value)} must be string type", nameof(value));

            try
            {
                var vals = ((string)value).Split(',');
                if (vals.Length == 3)
                    return System.Drawing.Color.FromArgb(byte.Parse(vals[0]), byte.Parse(vals[1]), byte.Parse(vals[2]));
                else if (vals.Length == 4)
                    return System.Drawing.Color.FromArgb(byte.Parse(vals[0]), byte.Parse(vals[1]), byte.Parse(vals[2]), byte.Parse(vals[3]));
                else
                {
                    var ret = System.Drawing.Color.FromName((string)value);
                    if (ret.A==0 && ret.R==0 && ret.G==0 && ret.B==0)
                        return System.Drawing.Color.Empty;
                    return ret;
                }
            }
            catch(FormatException)
            {
                return System.Drawing.Color.Empty;
            }
            catch(OverflowException)
            {
                return System.Drawing.Color.Empty;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(!(value is System.Drawing.Color))
                throw new ArgumentException($"{nameof(value)} must be {nameof(System.Drawing.Color)} type", nameof(value));

            var val=(System.Drawing.Color)value;
            return val.IsEmpty ? "" : val.IsNamedColor ? val.Name : $"{val.R},{val.G},{val.B}";
        }
    }
}
