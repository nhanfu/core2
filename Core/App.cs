using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Extensions;
using Core.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            LangSelect.Translate().Done((x) =>
            {
                InitApp();
            });
        }

        private static void InitApp()
        {
            Client.ModelNamespace = typeof(User).Namespace + ".";
            Spinner.Init();
            if (Client.IsPortal) InitPortal();
            else LoginBL.Instance.Render();
            AlterDeviceScreen();
        }

        private static void InitPortal()
        {
            LoginBL.Instance.SignedInHandler += (x) => Window.Location.Reload();
            LoadByFromUrl();
            EditForm.NotificationClient = new WebSocketClient("task");
            Window.AddEventListener(EventType.PopState, () =>
            {
                LoadByFromUrl();
                NotificationBL.Instance.Render();
            });
        }

        private static void LoadByFromUrl()
        {
            var builder = new StringBuilder();
            var feature = Window.Location.Href.Split(Utils.Slash).LastOrDefault();
            string fName;
            if (feature.IsNullOrWhiteSpace())
            {
                fName = "index";
            }
            else
            {
                for (int i = 0; i < feature.Length; i++)
                {
                    if (feature[i] == '?' || feature[i] == '#') break;
                    builder.Append(feature[i]);
                }
                fName = builder.ToString();
            }
            ComponentExt.InitFeatureByName(Client.ConnKey, fName, true).Done();
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
