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
            var reload = await ShouldReloadNewVersion();
            if (reload)
            {
                return;
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

        private static async Task<bool> ShouldReloadNewVersion()
        {
            var versionCurrent = await new Client().SubmitAsync<dynamic>(new XHRWrapper
            {
                Url = "/Entity/?$top=1&$filter=Name eq 'Version'"
            });
            if (versionCurrent == null || versionCurrent.data == null || versionCurrent.data.length == null)
            {
                return false;
            }
            var version = LocalStorage.GetItem<string>("Version");
            if (version is null)
            {
                LocalStorage.SetItem("Version", "1");
                version = "1";
            }
            if (version == versionCurrent.value[0].Description)
            {
                return false;
            }
            /*@
             const swalWithBootstrapButtons = Swal.mixin({
                  customClass: {
                    confirmButton: 'btn btn-success',
                    cancelButton: 'btn btn-danger'
                  },
                  buttonsStyling: false
                })
                swalWithBootstrapButtons.fire({
                  title: 'Cập nhật !',
                  text: "Phiên bản của bạn chưa mới nhất!",
                  icon: 'error',
                  showCancelButton: true,
                  confirmButtonText: 'Yes, Cập nhật!',
                  cancelButtonText: 'No, Không!',
                  reverseButtons: true
                }).then((result) => {
                  Core.Clients.LocalStorage.SetItem(System.String, "Version", versionCurrent.Description);
                  window.location.reload(true);
                })
             */
            return true;
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
            LoadUserSetting(token);
        }

        private static void LoadUserSetting(Token token)
        {
            if (token is null)
            {
                return;
            }
            var rsUserSetting = new Client(nameof(UserSetting)).GetAsync<OdataResult<UserSetting>>($"?$filter=Active eq true and UserId eq {token.UserId}");
            var rs = rsUserSetting.Result.Value.FirstOrDefault();
            if (rs is null)
            {
                TabEditor.ShowTabText = false;
            }
            else
            {
                bool.TryParse(rs.Value, out bool outbool);
                TabEditor.ShowTabText = outbool;
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
