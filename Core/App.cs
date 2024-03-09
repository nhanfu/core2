using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Extensions;
using Core.Models;
using System.Text;

namespace Core
{
    public static class App
    {
        public static string DefaultFeature = Utils.HeadChildren.layout?.content as string ?? "index";
        public static bool FeatureLoaded;

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
            NotificationBL.Instance.Render();
            EditForm.NotificationClient = new WebSocketClient("task");
            Window.AddEventListener(EventType.PopState, (e) =>
            {
                LoadByFromUrl();
            });
        }

        private static string LoadByFromUrl()
        {
            var fName = GetFeatureNameFromUrl() ?? DefaultFeature;
            ComponentExt.InitFeatureByName(Client.ConnKey, fName, true).Done();
            return fName;
        }

        public static string GetFeatureNameFromUrl()
        {
            var builder = new StringBuilder();
            var feature = Window.Location.PathName.Replace(Client.BaseUri, string.Empty);
            if (feature.StartsWith(Utils.Slash))
            {
                feature = feature.Substring(1);
            }
            string fName;
            if (feature.IsNullOrWhiteSpace())
            {
                return null;
            }
            for (int i = 0; i < feature.Length; i++)
            {
                if (feature[i] == '?' || feature[i] == '#') break;
                builder.Append(feature[i]);
            }
            fName = builder.ToString();

            return fName;
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
