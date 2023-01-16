using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.MVVM
{
    public enum ObservableAction
    {
        Render,
        Add,
        AddRange,
        Update,
        Remove,
        Move
    }

    public class ObservableList<T>
    {
        /// <summary>
        /// Use this to get and set List object without notifying change
        /// </summary>
        public List<T> _data;

        /// <summary>
        /// Use this to set List object and notify change
        /// </summary>
        public List<T> Data
        {
            get => _data;
            set
            {
                _data = value;
                ListChanged?.Invoke(new ObservableListArgs<T>
                {
                    ListData = _data,
                    Action = ObservableAction.Render
                });
            }
        }
        public Action<ObservableListArgs<T>> ListChanged;

        /// <summary>
        /// Init Observable list instance <br />
        /// Note that the data can be ICollection, don't use incompatible methods
        /// </summary>
        public ObservableList()
        {
            _data = new List<T>();
        }

        public ObservableList(List<T> data)
        {
            _data = data;
        }

        public void NotifyArrayChanged(ObservableListArgs<T> arg)
        {
            ListChanged?.Invoke(arg);
        }

        public void Add(T item, int? index = null)
        {
            ListChanged?.Invoke(new ObservableListArgs<T>
            {
                ListData = _data,
                Item = item,
                Index = index ?? _data.Count,
                Action = ObservableAction.Add
            });
        }

        public void Remove(T item)
        {
            ListChanged?.Invoke(new ObservableListArgs<T>
            {
                ListData = Data,
                Item = item,
                Action = ObservableAction.Remove
            });
        }

        public void RemoveAt(int index)
        {
            if (index < 0)
            {
                return;
            }

            var item = _data.ElementAt(index);
            ListChanged?.Invoke(new ObservableListArgs<T>
            {
                ListData = _data,
                Item = item,
                Index = index,
                Action = ObservableAction.Remove
            });
        }

        public void Update(T item, int index)
        {
            ListChanged?.Invoke(new ObservableListArgs<T>
            {
                ListData = _data,
                Item = item,
                Index = index,
                Action = ObservableAction.Update
            });
        }
    }
}
