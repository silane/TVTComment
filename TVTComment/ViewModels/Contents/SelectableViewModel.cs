using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.ViewModels.Contents
{
    class SelectableViewModel<T>:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private T value;
        private bool isSelected;
        public T Value
        {
            get { return value; }
            set { this.value = value;PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
        }
        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
        }

        public SelectableViewModel(T value)
        {
            Value = value;
        }

        public SelectableViewModel(T value,bool isSelected)
        {
            Value = value;
            IsSelected = isSelected;
        }
    }
}
