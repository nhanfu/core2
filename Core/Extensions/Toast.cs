using Bridge.Html5;
using Core.Components;
using Core.MVVM;

namespace Core.Extensions
{
    public class ToastOptions
    {
        public int Timeout { get; set; }
        public string ClassName { get; set; }
        public string Message { get; set; }
        public object[] Params { get; set; }
    }

    public static class Toast
    {
        public static void Create(ToastOptions options)
        {
            /*@
            if (typeof(PNotify) !== 'undefined') {
                 new PNotify({
                            title: options.Message,
                            delay: options.Timeout,
                            type: options.ClassName
                        });
                return;
            }
             */
            Html.Take(Document.Body).Div.ClassName("toast").ClassName(options.ClassName).IHtml(options.Message, options.Params);
            var toast = Html.Context;

            Window.SetTimeout(() =>
            {
                toast.Remove();
            }, options.Timeout);
        }

        public static void Success(string message, int timeout = 2500, params object[] parameters)
        {
            Create(new ToastOptions
            {
                ClassName = "success",
                Timeout = timeout,
                Message = message,
                Params = parameters
            });
        }

        public static void Warning(string message)
        {
            Create(new ToastOptions
            {
                ClassName = "warning",
                Timeout = 2500,
                Message = message,
            });
        }

        public static void Small(string message, int timeout = 2500)
        {
            Create(new ToastOptions
            {
                ClassName = "sm-tran",
                Timeout = timeout,
                Message = message,
            });
        }
    }
}
