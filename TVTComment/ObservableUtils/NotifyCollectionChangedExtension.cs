using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ObservableUtils
{
    static class NotifyCollectionChangedExtension
    {
        private class DelegateDisposable : IDisposable
        {
            private readonly Action disposeAction;
            public DelegateDisposable(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                disposeAction();
            }
        }

        public static IDisposable ObserveCollectionChanged<TCollection, TItem>(this TCollection collection, Action<TItem> itemAddedEventHandler, Action<TItem> itemRemovedEventHandler, Action itemClearedEventHandler)
            where TCollection : INotifyCollectionChanged, IEnumerable<TItem>
        {
            void collectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (object item in e.NewItems)
                            itemAddedEventHandler((TItem)item);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (object item in e.OldItems)
                            itemRemovedEventHandler((TItem)item);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        foreach (object item in e.OldItems)
                            itemRemovedEventHandler((TItem)item);
                        foreach (object item in e.NewItems)
                            itemAddedEventHandler((TItem)item);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        itemClearedEventHandler();
                        foreach (var item in collection)
                            itemAddedEventHandler(item);
                        break;
                }
            }
            collection.CollectionChanged += collectionChangedEventHandler;

            return new DelegateDisposable(() => collection.CollectionChanged -= collectionChangedEventHandler);
        }

        public static IDisposable ObserveCollectionChanged<TCollection, TItem>(this TCollection collection, Action<TItem, int> itemAddedEventHandler, Action<TItem, int> itemRemovedEventHandler, Action itemClearedEventHandler)
            where TCollection : INotifyCollectionChanged, IEnumerable<TItem>
        {
            void collectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
            {
                int idx;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        idx = e.NewStartingIndex;
                        foreach (object item in e.NewItems)
                            itemAddedEventHandler((TItem)item, idx++);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        idx = e.OldStartingIndex;
                        foreach (object item in e.OldItems)
                            itemRemovedEventHandler((TItem)item, idx++);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        idx = e.OldStartingIndex;
                        foreach (object item in e.OldItems)
                            itemRemovedEventHandler((TItem)item, idx++);
                        idx = e.NewStartingIndex;
                        foreach (object item in e.NewItems)
                            itemAddedEventHandler((TItem)item, idx++);
                        break;
                    case NotifyCollectionChangedAction.Move:
                        foreach (object item in e.OldItems)
                            itemRemovedEventHandler((TItem)item, e.OldStartingIndex);
                        idx = e.NewStartingIndex;
                        foreach (object item in e.NewItems)
                            itemAddedEventHandler((TItem)item, idx++);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        itemClearedEventHandler();
                        idx = 0;
                        foreach (var item in collection)
                            itemAddedEventHandler(item, idx++);
                        break;
                }
            }
            collection.CollectionChanged += collectionChangedEventHandler;

            return new DelegateDisposable(() => collection.CollectionChanged -= collectionChangedEventHandler);
        }

        public static IDisposable ObserveCollectionChanged<TItem>(this ObservableCollection<TItem> collection, Action<TItem> itemAddedEventHandler, Action<TItem> itemRemovedEventHandler, Action itemClearedEventHandler)
        {
            return ObserveCollectionChanged<ObservableCollection<TItem>, TItem>(collection, itemAddedEventHandler, itemRemovedEventHandler, itemClearedEventHandler);
        }

        public static IDisposable ObserveCollectionChanged<TItem>(this ObservableCollection<TItem> collection, Action<TItem, int> itemAddedEventHandler, Action<TItem, int> itemRemovedEventHandler, Action itemClearedEventHandler)
        {
            return ObserveCollectionChanged<ObservableCollection<TItem>, TItem>(collection, itemAddedEventHandler, itemRemovedEventHandler, itemClearedEventHandler);
        }

        public static IDisposable ObserveCollectionChanged<TItem>(this ReadOnlyObservableCollection<TItem> collection, Action<TItem> itemAddedEventHandler, Action<TItem> itemRemovedEventHandler, Action itemClearedEventHandler)
        {
            return ObserveCollectionChanged<ReadOnlyObservableCollection<TItem>, TItem>(collection, itemAddedEventHandler, itemRemovedEventHandler, itemClearedEventHandler);
        }

        public static IDisposable ObserveCollectionChanged<TItem>(this ReadOnlyObservableCollection<TItem> collection, Action<TItem, int> itemAddedEventHandler, Action<TItem, int> itemRemovedEventHandler, Action itemClearedEventHandler)
        {
            return ObserveCollectionChanged<ReadOnlyObservableCollection<TItem>, TItem>(collection, itemAddedEventHandler, itemRemovedEventHandler, itemClearedEventHandler);
        }
    }
}
