using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Models;
using Core.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public static class App
    {
        private static bool _initApp;
        public static async Task Main()
        {
            if (LangSelect.Culture == null)
            {
                LangSelect.Culture = "vi";
            }
            await TryInitData();
            InitApp();
        }

        private static void InitApp()
        {
            Client.ModelNamespace = typeof(User).Namespace + ".";
            _ = Spinner.Instance;
            LoginBL.Instance.Render();
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
            LoginBL.Instance.TokenRefreshedHandler += LoadSettingWrapper;
        }

        private static async Task TryInitData()
        {
            var tranTask = LangSelect.Translate();
            var entityTask = Client.LoadEntities();
            try
            {
                await Task.WhenAll(tranTask, entityTask);
            }
            catch (System.Exception)
            {
            }
        }

        private static void LoadSettingWrapper(Token token)
        {
            if (_initApp)
            {
                return;
            }
            _initApp = true;
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
