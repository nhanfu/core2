using Bridge.Html5;
using Core.Clients;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Threading.Tasks;

namespace Core.Components.Framework
{
    public class LoginBL : PopupEditor
    {
        private static LoginBL _instance;
        private static bool _initApp;
        private int _renderAwaiter;
        public LoginVM LoginEntity => Entity as LoginVM;

        /// <summary>
        /// This action is invoke after the user get signed in
        /// </summary>
        public Action<Token> SignedInHandler { get; set; }

        /// <summary>
        /// This action is invoked when the app is initial
        /// </summary>
        public Action<Token> InitAppHanlder { get; set; }

        /// <summary>
        /// This action is invoked after user signed in or the token is refreshed
        /// </summary>
        public Action<Token> TokenRefreshedHandler { get; set; }
        public static MenuComponent MenuComponent { get; set; }
        public static NotificationBL TaskList { get; set; }

        private LoginBL() : base(nameof(User))
        {
            Entity = new LoginVM()
            {
                AutoSignIn = true,
            };
            Name = "Login";
            Title = "Đăng nhập";
            Window.AddEventListener(EventType.BeforeUnload, () => NotificationClient?.Close());
            Public = true;
        }

        public static LoginBL Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new LoginBL();
                }

                return _instance;
            }
        }

        public override void Render()
        {
            var oldToken = Client.Token;
            if (oldToken is null || oldToken.RefreshTokenExp <= Client.EpsilonNow)
            {
                RenderLoginForm();
            }
            else if (oldToken.AccessTokenExp > Client.EpsilonNow)
            {
                InitAppIfEmpty();
            }
            else if (oldToken.RefreshTokenExp > Client.EpsilonNow)
            {
                Task.Run(async () => await Client.RefreshToken((newToken) =>
                {
                    InitAppIfEmpty();
                }));
            }
        }

        public void RenderLoginForm()
        {
            Window.ClearTimeout(_renderAwaiter);
            _renderAwaiter = Window.SetTimeout(() =>
            {
                if (_backdrop != null)
                {
                    Document.Body.AppendChild(_backdrop);
                    _backdrop.Show();
                    return;
                }
                Html.Take("#tab-content")
                .Div.ClassName("modal is-open").Event(EventType.KeyPress, KeyCodeEnter);
                _backdrop = Html.Context;
                Html.Instance.Div.ClassName("modal-container")
                            .Div.ClassName("modal-left")
                                .H1.ClassName("modal-title").Text("XIN CHÀO").End
                                 .Div.ClassName("input-block")
                                    .Label.ClassName("input-label").Text("Tên tài khoản").End
                                    .Input.Event(EventType.Input, (e) => LoginEntity.UserName = e.GetInputText()).Attr("name", "UserName").Type("text").End.End
                                .Div.ClassName("input-block")
                                    .Label.ClassName("input-label").Text("Mật khẩu").End
                                    .Input.Event(EventType.Input, (e) => LoginEntity.Password = e.GetInputText()).Attr("name", "Password").Value(LoginEntity.Password).Type("password").End.End
                                .Div.ClassName("input-block")
                                    .Label.ClassName("input-label").Text("Ghi nhớ").End
                                    .Label.ClassName("checkbox input-small transition-on style2")
                                    .Checkbox(LoginEntity.AutoSignIn).Event(EventType.Input, (e) => LoginEntity.AutoSignIn = e.Target.Cast<HTMLInputElement>().Checked).Attr("name", "AutoSignIn").Attr("name", "AutoSignIn").End
                                    .Span.ClassName("check myCheckbox").End.End.End
                                .Div.ClassName("modal-buttons")
                                    .A.Href("").Text("Quên mật khẩu?").End
                                    .Button.Id("btnLogin").Event(EventType.Click, () => Login().Done()).ClassName("input-button").Text("Đăng nhập").End.End.End
                            .Div.ClassName("modal-right")
                                .Img.Src("../image/bg-launch.jpg").End.Render();
                base.Element = Html.Context;
            }, 100);
        }

        private void KeyCodeEnter(Event e)
        {
            if (e.KeyCode() != (int)KeyCodeEnum.Enter)
            {
                return;
            }
            e.PreventDefault();
            Document.GetElementById("btnLogin").Click();
        }

        public Task<bool> Login()
        {
            var tcs = new TaskCompletionSource<bool>();
            IsFormValid().Done(isValid =>
            {
                if (!isValid)
                {
                    tcs.TrySetResult(false);
                    return;
                }
                ProcessValidLogin().Done(status => tcs.TrySetResult(status));
            });
            return tcs.Task;
        }

        private Task<bool> ProcessValidLogin()
        {
            var login = LoginEntity;
            var tcs = new TaskCompletionSource<bool>();
            login.RecoveryToken = Utils.GetUrlParam("recovery");
            var domainUser = login.UserName.Split('/');
            login.CompanyName = domainUser.Length > 1 ? domainUser[0] : Client.Tenant;
            login.UserName = domainUser.Length > 1 ? domainUser[1] : login.UserName;
            if (login.CompanyName.IsNullOrWhiteSpace())
            {
                Toast.Warning("Company name must be provided!\nFor example my-compay/my-username");
                return Task.FromResult(false);
            }
            login.Env = Client.Env;
            Client.Instance.SubmitAsync<Token>(new XHRWrapper
            {
                Url = $"/{login.CompanyName}/User/SignIn",
                Value = JSON.Stringify(login),
                IsRawString = true,
                Method = HttpMethod.POST,
                AllowAnonymous = true
            }).Done(res =>
            {
                if (res == null)
                {
                    tcs.TrySetResult(false);
                    return;
                }
                Toast.Success($"Xin chào {res.FullName}!");
                Client.Token = res;
                login.UserName = string.Empty;
                login.Password = string.Empty;
                InitAppIfEmpty();
                InitFCM();
                SignedInHandler?.Invoke(Client.Token);
                tcs.TrySetResult(true);
                Dispose();
            })
            .Catch(e => tcs.TrySetResult(false));
            return tcs.Task;
        }

        public Task<bool> ForgotPassword(LoginVM login)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.Instance
                .PostAsync<bool>(login, "/user/ForgotPassword")
                .Done(res =>
                {
                    if (res)
                    {
                        Toast.Warning("An error occurs. Please contact the administrator to get your password!");
                    }
                    else
                    {
                        Toast.Success($"A recovery email has been sent to your email address. Please check and follow the steps in the email!");
                    }
                    tcs.TrySetResult(res);
                });
            return tcs.Task;
        }

        public void InitAppIfEmpty()
        {
            Client.SystemRole = Client.Token.RoleIds.Contains(((int)RoleEnum.System).ToString());
            if (_initApp)
            {
                return;
            }
            _initApp = true;
            InitAppHanlder?.Invoke(Client.Token);
            var userId = Client.Token.UserId;
            if (NotificationClient is null)
            {
                NotificationClient = new WebSocketClient("task");
            }

            if (MenuComponent is null)
            {
                MenuComponent.Instance.Render();
            }
            if (TaskList is null)
            {
                TaskList = NotificationBL.Instance;
                TaskList.Render();
                TaskList.DOMContentLoaded += () =>
                {
                    Document.GetElementById("name-user").TextContent = Client.Token.UserName;
                    Document.GetElementById("Username-text").TextContent = Client.Token.FullName;
                    Document.GetElementById("text-address").TextContent = Client.Token.Address;
                    Html.Take("#user-image").Src("./image/" + Client.Token.Avatar);
                    Html.Take("#img-detail").Src("./image/" + Client.Token.Avatar);
                };
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static void InitFCM(bool signout = false)
        {
            Console.WriteLine("Init fcm");
            var tanentCode = Client.Token.TenantCode;
            var strUserId = $"U{Client.Token.UserId:0000000}";
            /*@
            if (typeof(PushNotification) === 'undefined') return;
            var topics = ['/topics/' + tanentCode + strUserId];
            const push = PushNotification.init({
                android: {
                    senderID: '9681598079',
                    topics: topics
                },
                ios: {
                    alert: "true",
                    badge: "true",
                    sound: "true",
                    topics: topics
                },
            });
            if (signout && push.unsubscribe) {
                push.unsubscribe(topics[0]);
                return;
            }
            push.on('registration', (data) => {
            });
            push.on('notification', (data) => {
                if (typeof(cordova) !== 'undefined' &&
                    typeof(cordova.plugins) !== 'undefined' &&
                    typeof(cordova.plugins.notification) !== 'undefined') {
                    cordova.plugins.notification.local.schedule({
                        title: data.title,
                        text: data.message,
                        foreground: true,
                    });
                }
                // data.message,
                // data.title,
                // data.count,
                // data.sound,
                // data.image,
                // data.additionalData
            });

            push.on('error', (e) => {
                // e.message
            });
            */
        }

        public static void DiposeAll()
        {
            while (Tabs.Count > 0)
            {
                Tabs[0]?.Dispose();
            }
            if (MenuComponent != null)
            {
                MenuComponent.Dispose();
            }

            if (TaskList != null)
            {
                TaskList.Dispose();
            }

            MenuComponent = null;
            TaskList = null;
        }

        public override void Dispose()
        {
            _backdrop.Hide();
            Html.Take(".is-open").Clear();
        }
    }
}