using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Framework;
using Core.Models;

namespace Core
{
    public static class App
    {
        public static void Main()
        {
            if (LangSelect.Culture == null)
            {
                LangSelect.Culture = "vi";
            }
            Client.ExecTask(LangSelect.Translate(), (x) =>
            {
                InitApp();
            });
        }

        private static void InitApp()
        {
            Client.ModelNamespace = typeof(User).Namespace + ".";
            Spinner.Init();
            LoginBL.Instance.Render();
            AlterDeviceScreen();
        }

        private static void AlterDeviceScreen()
        {
            var iPhone = new RegExp("(iPod|iPhone)").Test(Window.Navigator.UserAgent)
                            && new RegExp("AppleWebKit").Test(Window.Navigator.UserAgent);
            if (iPhone)
            {
                var className = "iphone ";
                if (Window.Screen.AvailHeight == 812 && Window.Screen.AvailWidth == 375)
                {
                    className += "portrait";
                }
                else
                {
                    className += "landscape";
                }
                Document.DocumentElement.ClassName = className;
            }
        }

        public static bool IsMobile()
        {
            /*@
            * if (typeof(cordova) == 'undefined') {
            *     return false;
            * }
            */
            return true;
        }
    }
}
