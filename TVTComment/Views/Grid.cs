using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;

namespace TVTComment.Views
{
    public static class Grid
    {
        public static string GetShape(DependencyObject obj)
        {
            return (string)obj.GetValue(ShapeProperty);
        }

        public static void SetShape(DependencyObject obj, string value)
        {
            obj.SetValue(ShapeProperty, value);
        }

        // Using a DependencyProperty as the backing store for Shape.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.RegisterAttached("Shape", typeof(string), typeof(Grid), new PropertyMetadata(shapeChanged),validateShape);

        private static void shapeChanged(DependencyObject sender,DependencyPropertyChangedEventArgs e)
        {
            var shape = (string)e.NewValue;
            var grid = (System.Windows.Controls.Grid)sender;

            if (string.IsNullOrWhiteSpace(shape))
                return;

            int idx = shape.IndexOf(':');

            foreach(string s in shape.Substring(0,idx).Split(','))
            {
                var column = new System.Windows.Controls.ColumnDefinition();
                string str = s.Trim();

                //SharedSizeGroup
                int idx2 = str.IndexOf('#');
                if(idx2!=-1)
                {
                    column.SharedSizeGroup = str.Substring(idx2 + 1);
                    str = str.Substring(0, idx2).TrimEnd();
                }

                //Width
                if (str == "auto")
                    column.Width = GridLength.Auto;
                else
                {
                    if (str.EndsWith("*"))
                        column.Width = new GridLength(str.Length==1 ? 1 :  double.Parse(str.Substring(0, str.Length - 1)), GridUnitType.Star);
                    else
                        column.Width = new GridLength(double.Parse(str));
                }

                grid.ColumnDefinitions.Add(column);
            }

            foreach (string s in shape.Substring(idx+1).Split(','))
            {
                var row = new System.Windows.Controls.RowDefinition();
                string str = s.Trim();

                //SharedSizeGroup
                int idx2 = str.IndexOf('#');
                if (idx2 != -1)
                {
                    row.SharedSizeGroup = str.Substring(idx2 + 1);
                    str = str.Substring(0, idx2).TrimEnd();
                }

                //Width
                if (str == "auto")
                    row.Height = GridLength.Auto;
                else
                {
                    if (str.EndsWith("*"))
                        row.Height = new GridLength(str.Length==1 ? 1 : double.Parse(str.Substring(0, str.Length - 1)), GridUnitType.Star);
                    else
                        row.Height = new GridLength(double.Parse(str));
                }

                grid.RowDefinitions.Add(row);
            }
        }

        private static readonly Regex reShape = new Regex(@"^((\d+|\d*\*|auto),)*(\d+|\d*\*|auto):((\d+|\d*\*|auto),)*(\d+|\d*\*|auto)$");
        private static bool validateShape(object value)
        {
            string str=value as string;
            if (str == null) return true;
            return reShape.Match(str).Success;
        }
    }
}
