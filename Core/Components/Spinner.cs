using Bridge.Html5;
using Core.MVVM;

namespace Core.Components
{
    public class Spinner
    {
        private static Spinner _instance;
        private static HTMLElement _span;
        private static HTMLElement _backdrop;
        private static int _hiddenAwaiter;

        private Spinner()
        {
        }

        public static Spinner Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = new Spinner();
                var existing = Document.GetElementById("spinner");
                if (existing is null)
                {
                    Html.Take(Document.Body).Div.ClassName("backdrop").Style("background: transparent !important;").End.Span.ClassName("spinner");
                    _span = Html.Context;
                }
                else
                {
                    _span = existing;
                }
                _backdrop = _span.PreviousElementSibling;
                _span.Style.Display = Display.None.ToString();
                _backdrop.Style.Display = Display.None.ToString();

                return _instance;
            }
        }

        public static void AppendTo(HTMLElement node, bool lockScreen = true, bool autoHide = true, int timeout = 7000)
        {
            if (_span.ParentElement == node && _span.Style.Display.ToString() == string.Empty)
            {
                return;
            }
            if (node != null)
            {
                node.AppendChild(_span);
            }
            else
            {
                Document.Body.AppendChild(_span);
            }

            _span.Style.Display = string.Empty;
            if (lockScreen)
            {
                _backdrop.Style.Display = string.Empty;
            }
            if (!autoHide)
            {
                return;
            }
            Window.ClearTimeout(_hiddenAwaiter);
            _hiddenAwaiter = Window.SetTimeout(Hide, timeout);
        }

        public static void Hide()
        {
            if(_span!= null)
            {
                _span.Style.Display = Display.None.ToString();
                _backdrop.Style.Display = Display.None.ToString();
            }
        }
    }
}
