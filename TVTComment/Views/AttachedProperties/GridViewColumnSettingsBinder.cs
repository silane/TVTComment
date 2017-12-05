using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace TVTComment.Views.AttachedProperties
{
    public static class GridViewColumnSettingsBinder
    {
        public struct ColumnInfo
        {
            public string Id { get; }
            public double Width { get; }
            public ColumnInfo(string id,double width)
            {
                Id = id;
                Width = width;
            }
        }

        public static string GetColumnId(DependencyObject obj)
        {
            return (string)obj.GetValue(ColumnIdProperty);
        }

        public static void SetColumnId(DependencyObject obj, string value)
        {
            obj.SetValue(ColumnIdProperty, value);
        }

        // Using a DependencyProperty as the backing store for ColumnId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColumnIdProperty =
            DependencyProperty.RegisterAttached("ColumnId", typeof(string), typeof(GridViewColumnSettingsBinder), new PropertyMetadata(null));



        public static ColumnInfo[] GetBinding(DependencyObject obj)
        {
            return (ColumnInfo[])obj.GetValue(BindingProperty);
        }

        public static void SetBinding(DependencyObject obj, ColumnInfo[] value)
        {
            obj.SetValue(BindingProperty, value);
        }

        // Using a DependencyProperty as the backing store for Binding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BindingProperty =
            DependencyProperty.RegisterAttached("Binding", typeof(ColumnInfo[]), typeof(GridViewColumnSettingsBinder), new PropertyMetadata(new ColumnInfo[0],OnBindingPropertyChanged));


        private static Dictionary<GridViewColumnCollection, ListView> dict = new Dictionary<GridViewColumnCollection, ListView>();

        private static void OnBindingPropertyChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            var listView = (ListView)d;
            if (!listView.IsLoaded)
            {
                listView.Loaded -= ListView_Loaded;
                listView.Loaded += ListView_Loaded;
            }
            else
            {
                AttachGridViewColumnChangedEvent(listView);
                updateGridViewColumns(listView);
                updateColumnInfos(listView);
            }
        }

        private static void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            var listView = (ListView)sender;
            listView.Loaded -= ListView_Loaded;
            AttachGridViewColumnChangedEvent(listView);
            updateGridViewColumns(listView);
            updateColumnInfos(listView);
        }

        private static void AttachGridViewColumnChangedEvent(ListView listView)
        {
            var gridView = (GridView)listView.View;
            dict[gridView.Columns] = listView;
            gridView.Columns.CollectionChanged -= GridViewColumnCollectionChanged;
            gridView.Columns.CollectionChanged += GridViewColumnCollectionChanged;
            foreach(var column in gridView.Columns)
            {
                ((INotifyPropertyChanged)column).PropertyChanged -= GridViewColumnPropertyChanged;
                ((INotifyPropertyChanged)column).PropertyChanged += GridViewColumnPropertyChanged;
            }
        }

        private static void GridViewColumnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var column = (GridViewColumn)sender;
            var listView = dict.First(x => x.Key.Contains(column)).Value;
            if (e.PropertyName==nameof(GridViewColumn.Width))
            {
                updateColumnInfos(listView);
            }
        }

        private static void GridViewColumnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var columns = (GridViewColumnCollection)sender;
            updateColumnInfos(dict[(GridViewColumnCollection)sender]);
        }

        private static void updateColumnInfos(ListView listView)
        {
            var gridView = (GridView)listView.View;
            var columnInfos = gridView.Columns.Select(x =>
            {
                var id = GetColumnId(x);
                if (string.IsNullOrWhiteSpace(id)) throw new Exception("GridViewColumnSettingsBinder.ColumnId must be set on all GridViewColumn");
                return new ColumnInfo(id, x.Width);
            }).ToArray();
            var currentColumnInfos = GetBinding(listView);
            if (currentColumnInfos!=null && currentColumnInfos.SequenceEqual(columnInfos)) return;
            SetBinding(listView, columnInfos);
        }

        private static void updateGridViewColumns(ListView listView)
        {
            var gridView = (GridView)listView.View;
            var columnInfos = GetBinding(listView);
            if (columnInfos == null) return;
            int idx = 0;
            foreach(var columnInfo in columnInfos)
            {
                int idx2 = 0;
                foreach(var column in gridView.Columns)
                {
                    var id = GetColumnId(column);
                    if(id==columnInfo.Id)
                    {
                        gridView.Columns[idx2].Width = columnInfo.Width;
                        gridView.Columns.Move(idx2,idx);
                        break;
                    }
                    idx2++;
                }
                idx++;
            }
        }
    }
}
