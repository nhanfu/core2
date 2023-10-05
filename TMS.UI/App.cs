using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.UI.Business.Authentication;

namespace TMS.UI
{
    public static class App
    {
        public static async Task Main()
        {
            if (LangSelect.Culture == null)
            {
                LangSelect.Culture = "vi";
            }
            var translateTask = LangSelect.Translate();
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
                          if (result.isConfirmed) {
                            Core.Clients.LocalStorage.SetItem(System.String, "Version", versionCurrent.Description);
                            window.location.reload(true);
                          } else {
                            Core.Clients.LocalStorage.SetItem(System.String, "Version", versionCurrent.Description);
                            window.location.reload(true);
                          }
                        })
                     */
                    return;
                }
            }
            var loadEntityTask = Client.LoadEntities();
            await Task.WhenAll(translateTask, loadEntityTask);
            InitTheme();
            Client.ModelNamespace = typeof(User).Namespace + ".";
            _ = Spinner.Instance;
            var tanent = Document.Head.GetElementsByTagName("meta")["tenant"]["content"].ToString();
            LoginBL.Instance.LoginEntity.CompanyName = tanent;
            LoginBL.Instance.Render();
            Client.UnAuthorizedEventHandler += xhr => LoginBL.Instance.Render();
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
            if (Client.Token != null)
            {
                var newToken = await Client.GetToken(Client.Token);
                if (newToken != null)
                {
                    Client.Token = newToken;
                }
                else
                {
                    Toast.Success("Logout success!");
                    Client.Token = null;
                    LocalStorage.RemoveItem("UserInfo");
                    Window.Location.Reload();
                }
                await LoadUserSetting(Client.Token?.UserId);
            }
            LoginBL.Instance.InitAppHanlder += LoadSettingWrapper;
            Window.SetTimeout(() =>
            {
                /*@
                 Theme1.initBeforeLoad();
                 Theme1.initCore();
                 **/
            }, 1000);
        }

        public static void InitTheme()
        {
            var theme = LocalStorage.GetItem<string>("theme");
            if (theme != null)
            {
                theme = theme.Replace("\"", "");
                Document.Body.ReplaceClass("theme-1", theme);
                if (theme == "theme-2")
                {
                    Document.QuerySelector("#navbar-menu-mode")?.ReplaceClass("navbar-light", "navbar-dark");
                    Document.QuerySelector("#breadcrumb-menu-mode")?.ReplaceClass("breadcrumb-line-light", "breadcrumb-line-dark");
                    Document.Body.ReplaceClass("bg-light", "bg-dark");
                }
                else
                {
                    Document.QuerySelector("#navbar-menu-mode")?.ReplaceClass("navbar-dark", "navbar-light");
                    Document.QuerySelector("#breadcrumb-menu-mode")?.ReplaceClass("breadcrumb-line-dark", "breadcrumb-line-light");
                    Document.Body.ReplaceClass("bg-dark", "bg-light");
                }
            }
        }

        private static void LoadSettingWrapper(Token token)
        {
            Task.Run(async () => await LoadUserSetting(token?.UserId));
        }

        private static async Task LoadUserSetting(string userId)
        {
            if (userId != null)
            {
                return;
            }
            var rsUserSetting = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>($"?t={LoginBL.Instance.LoginEntity.CompanyName}&$filter=Active eq true and UserId eq {userId} and Name eq 'ShowTabText'");
            if (rsUserSetting is null)
            {
                TabEditor.ShowTabText = true;
            }
            else
            {
                bool.TryParse(rsUserSetting.Value, out bool outbool);
                TabEditor.ShowTabText = outbool;
            }
        }
    }
}
