using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Forms;
using Core.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.UI.Business.Authentication;

namespace TMS.UI
{
    public static class App
    {
        private static bool _initApp;
        private static int _countDownUpdator;
        private static int _countAutoUpdateStaus;
        public static async Task Main()
        {
            if (LangSelect.Culture == null)
            {
                LangSelect.Culture = "vi";
            }

            var versionCurrent = await new Client(nameof(Entity)).FirstOrDefaultAsync<Entity>("?$top=1&$filter=Name eq 'Version'");
            if (versionCurrent != null)
            {
                var version = LocalStorage.GetItem<string>("Version");
                if (version is null)
                {
                    LocalStorage.SetItem("Version", "1");
                    version = "1";
                }
                if (version != versionCurrent.Description)
                {
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
                    return;
                }
            }

            await TryInitData();
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
            Task.Run(async () => await LoadUserSetting(token));
        }

        private static async Task LoadUserSetting(Token token)
        {
            if (token is null)
            {
                return;
            }
            var rsUserSetting = new Client(nameof(UserSetting)).GetAsync<OdataResult<UserSetting>>($"?$filter=Active eq true and UserId eq {token.UserId}");
            var rstimeTrackGPS = new Client(nameof(MasterData)).GetAsync<OdataResult<MasterData>>($"?$filter=Active eq true and Name eq 'TimeTrackGPS'");
            var rsTimeUpdateStatus = new Client(nameof(MasterData)).GetAsync<OdataResult<MasterData>>($"?$filter=Active eq true and Name eq 'TimeUpdateStatus'");
            await Task.WhenAll(rsUserSetting, rstimeTrackGPS);
            var rs = rsUserSetting.Result.Value.FirstOrDefault();
            var timeTrackGPS = int.Parse(rstimeTrackGPS.Result.Value.FirstOrDefault().Description);
            var timeUpdateStatus = int.Parse(rstimeTrackGPS.Result.Value.FirstOrDefault().Description);
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
