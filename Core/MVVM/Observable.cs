using System;

namespace Core.MVVM
{
    public class Observable<T>
    {
        public Func<T> Getter;
        public Action<T> Setter;
        public T _data;

        public Observable()
        {
            _data = default(T);
        }

        public Observable(T data)
        {
            _data = data;
        }

        public event Action<ObservableArgs<T>> Changed;
        public virtual T Data
        {
            get => Getter != null ? Getter() : _data;
            set
            {
                if (_data.Equals(value))
                {
                    return;
                }

                var oldValue = _data;
                _data = value;
                Changed?.Invoke(new ObservableArgs<T> { NewData = _data, OldData = oldValue });
                if (Setter != null)
                {
                    Setter.Invoke(_data);
                }
            }
        }
    }
}
