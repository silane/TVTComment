using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace ObservableUtils
{
    class ObservableValue<T> : INotifyPropertyChanged, IObservable<T>
    {
        private readonly T initialValue;
        private T val;
        private readonly BehaviorSubject<T> subject;

        public T Value
        {
            get { return val; }
            set
            {
                if (val == null && value == null)
                    return;

                if (val != null && value != null && EqualityComparer<T>.Default.Equals(val, value))
                    return;

                val = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                subject.OnNext(value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableValue(T initialValue = default)
        {
            subject = new BehaviorSubject<T>(Value);
            this.initialValue = initialValue;
            Value = initialValue;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subject.Subscribe(observer);
        }

        public void ForceNotify()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            subject.OnNext(val);
        }

        public void RestoreInitialValue()
        {
            Value = initialValue;
        }
    }

    static class ObservableValueExtensions
    {
        public static ObservableValue<TResult> MakeLinkedObservableValue<TSource, TResult>(this ObservableValue<TSource> source,
            Func<TSource, TResult> convert, Func<TResult, TSource> convertBack)
        {
            var ret = new ObservableValue<TResult>(convert(source.Value));
            source.Subscribe(x => ret.Value = convert(x));
            ret.Subscribe(x => source.Value = convertBack(x));
            return ret;
        }
    }

    class ReadOnlyObservableValue<T> : INotifyPropertyChanged, IObservable<T>, IDisposable
    {
        private readonly IObservable<T> source;
        private T val;
        private readonly IDisposable disposable;

        public T Value => val;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyObservableValue(IObservable<T> source, T initialValue = default)
        {
            this.source = source;
            val = initialValue;

            disposable = source.Subscribe(val =>
              {
                  this.val = val;
                  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
              });
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(val);
            return source.Subscribe(observer);
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}