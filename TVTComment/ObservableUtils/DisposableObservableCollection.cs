using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservableUtils
{
    class DisposableObservableCollection<T>:ObservableCollection<T>,IDisposable
    {
        private List<IDisposable> disposables=new List<IDisposable>();

        public DisposableObservableCollection() { }
        public DisposableObservableCollection(List<T> list) : base(list) { }
        public DisposableObservableCollection(IEnumerable<T> collection) : base(collection) { }

        protected override void RemoveItem(int index)
        {
            var removedItem = this[index];
            base.RemoveItem(index);
            (removedItem as IDisposable)?.Dispose();
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
                (item as IDisposable)?.Dispose();
            base.ClearItems();
        }

        public void AddDisposable(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        public void Dispose()
        {
            foreach (var item in this)
                (item as IDisposable)?.Dispose();
            foreach (var disposable in disposables)
                disposable.Dispose();
        }
    }

    class DisposableReadOnlyObservableCollection<T>:ReadOnlyObservableCollection<T>,IDisposable
    {
        private DisposableObservableCollection<T> list;
        public DisposableReadOnlyObservableCollection(DisposableObservableCollection<T> list):base(list)
        {
            this.list = list;
        }
        public void Dispose()
        {
            list.Dispose();
        }
    }

}
