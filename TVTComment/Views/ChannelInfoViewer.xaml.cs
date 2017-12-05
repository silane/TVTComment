using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Prism.Mvvm;

namespace TVTComment.Views
{
    /// <summary>
    /// Interaction logic for ChannelInfo.xaml
    /// </summary>
    public partial class ChannelInfoViewer : UserControl
    {
        private class ViewModel : BindableBase
        {
            private Model.ChannelInfo channel;
            public Model.ChannelInfo Channel { get { return channel; } set { SetProperty(ref channel, value); } }

            private Model.EventInfo _event;
            public Model.EventInfo Event { get { return _event; } set { SetProperty(ref _event, value); OnPropertyChanged(nameof(EventEndTime)); } }

            public DateTime? EventEndTime { get { return Event?.StartTime+Event?.Duration; } }
        }
        public ChannelInfoViewer()
        {
            InitializeComponent();
            this.OuterMostGrid.DataContext = new ViewModel();
        }

        public static readonly DependencyProperty ChannelInfoProperty = DependencyProperty.Register("ChannelInfo", typeof(Model.ChannelInfo), typeof(ChannelInfoViewer),
            new PropertyMetadata(null, (sender, e) =>
            {
                (sender as ChannelInfoViewer).OnChannelInfoPropertyChanged(sender, e);
            }));
        public static readonly DependencyProperty EventInfoProperty = DependencyProperty.Register("EventInfo", typeof(Model.EventInfo), typeof(ChannelInfoViewer),
            new PropertyMetadata(null, (sender, e) =>
            {
                (sender as ChannelInfoViewer).OnEventInfoPropertyChanged(sender, e);
            }));

        public Model.ChannelInfo ChannelInfo
        {
            get { return (Model.ChannelInfo)this.GetValue(ChannelInfoProperty); }
            set { this.SetValue(ChannelInfoProperty, value); }
        }

        public Model.EventInfo EventInfo
        {
            get { return (Model.EventInfo)this.GetValue(EventInfoProperty); }
            set { this.SetValue(EventInfoProperty, value); }
        }

        private void OnChannelInfoPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (this.OuterMostGrid.DataContext as ViewModel).Channel = (Model.ChannelInfo)e.NewValue;
        }
        private void OnEventInfoPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (this.OuterMostGrid.DataContext as ViewModel).Event = (Model.EventInfo)e.NewValue;
        }
    }
}
