using Bridge.Html5;
using System;
using System.Collections.Generic;

namespace Core.MVVM
{
    public class ObservableArgs
    {
        public EventType EvType { get; set; }
        public object NewData { get; set; }
        public object OldData { get; set; }
        public object NewMatch { get; set; }
        public object OldMatch { get; set; }
        public object NewEntity { get; set; }
        public object OldEntity { get; set; }
        public string FieldName { get; set; }
    }

    public class ObservableArgs<T>
    {
        public T NewData { get; set; }
        public T OldData { get; set; }
    }

    public class ObservableListArgs<T>
    {
        public List<T> ListData { get; set; }
        public T Item { get; set; }
        public int Index { get; set; }
        public ObservableAction? Action { get; set; }
        internal HTMLElement Element { get; set; }
        internal Action<T, int> Renderer { get; set; }
    }
}
