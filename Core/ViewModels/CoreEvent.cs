using Bridge.Html5;

namespace Core.ViewModels
{
    public class CoreEvent : Event
    {
        public object OldVal { get; internal set}
        public object NewVal { get; internal set}
    }
}