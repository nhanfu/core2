using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.Notifications;
using Core.ViewModels;
using System;
using System.Threading.Tasks;

namespace Core.Fw.Authentication
{
    public class LoginBL : PopupEditor
    {
        protected static LoginBL _instance;
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
        public static Menu MenuComponent { get; set; }
        public static NotificationBL TaskList { get; set; }

        protected LoginBL() : base(nameof(User))
        {
            Entity = new LoginVM()
            {
                CompanyName = LocalStorage.GetItem<string>("Tanent") ?? "Softek",
                AutoSignIn = true
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

        public void RenderLoginForm(string formName = "Login")
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
                _backdrop = Html.Take(Document.Body)
                    .Div.ClassName("modal is-open").TabIndex(-1).Event(EventType.KeyPress, KeyCodeEnter).GetContext();
                Html.Instance.Div.ClassName("modal-container")
                    .Button.ClassName("fa fa-times").Event(EventType.Click, Dispose).End
                    .Div.ClassName("modal-left")
                        .H1.ClassName("modal-title").IText("Welcome!").End
                        .P.ClassName("modal-desc").IText("Administrator management!").End
                        .Div.ClassName("input-block")
                            .Label.ClassName("input-label").IText("Company name").End
                            .Input.Id("CompanyName").Event(EventType.Input, (e) => LoginEntity.CompanyName = e.Target.Cast<HTMLInputElement>().Value).Attr("name", "CompanyName").Value(LoginEntity.CompanyName).Type("text").End.End
                        .Div.ClassName("input-block")
                            .Label.ClassName("input-label").IText("User name").End
                            .Input.Event(EventType.Input, (e) => LoginEntity.UserName = e.Target.Cast<HTMLInputElement>().Value).Attr("name", "UserName").Value(LoginEntity.UserName).Type("text").End.End
                        .Div.ClassName("input-block")
                            .Label.ClassName("input-label").IText("Password").End
                            .Input.Event(EventType.Input, (e) => LoginEntity.Password = e.Target.Cast<HTMLInputElement>().Value).Attr("name", "Password").Value(LoginEntity.Password).Type("password").End.End
                        .Div.ClassName("input-block")
                            .Label.ClassName("input-label").IText("Auto signin").End
                            .Label.ClassName("checkbox input-small transition-on style2")
                            .Checkbox(LoginEntity.AutoSignIn).Event(EventType.Input, (e) => LoginEntity.AutoSignIn = e.Target.Cast<HTMLInputElement>().Checked).Attr("name", "AutoSignIn").Attr("name", "AutoSignIn").End
                            .Span.ClassName("check myCheckbox").End.End.End
                        .Div.ClassName("modal-buttons")
                            .A.Href("").IText("Forgot your password?").End
                            .Button.Id("btnLogin").Event(EventType.Click, async () => await Login(LoginEntity)).ClassName("input-button").IText("Login").End.End.End
                    .Div.ClassName("modal-right")
                        .Img.Src("../image/bg-launch.jpg").End.Render();
                (_backdrop.QuerySelector("#CompanyName") as HTMLInputElement)?.Focus();
            }, 100);
        }

        private void KeyCodeEnter(Event e)
        {
            switch (e.KeyCode())
            {
                case (int)KeyCodeEnum.Enter:
                    e.PreventDefault();
                    Document.GetElementById("btnLogin").Click();
                    break;
                case (int)KeyCodeEnum.Escape:
                    Dispose();
                    break;
            }
        }

        public async Task<bool> Login(LoginVM login)
        {
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return false;
            }
            login.RecoveryToken = Utils.GetUrlParam("recovery");
            Token res = null;
            try
            {
                res = await Client.CreateAsync<Token>(login, "SignIn?t=" + login.CompanyName);
            }
            catch
            {
            }
            if (!login.AutoSignIn)
            {
                login.Password = string.Empty;
            }
            if (res is null)
            {
                Toast.Warning("Tên đăng nhập hoặc mật khẩu không chính xác!");
            }
            else
            {
                Toast.Success($"Xin chào {res.FullName}!");
                Client.Token = res;
                login.UserName = string.Empty;
                LocalStorage.SetItem("Tanent", login.CompanyName);
                LayoutForm?.UpdateView();
                // InitAppIfEmpty();
                InitFCM();
                SignedInHandler?.Invoke(Client.Token);
                Client.SignOutEventHandler = () => InitFCM(false);
                Dispose();
            }
            this.FindComponentByName<Section>("Form")?.UpdateView();

            return true;
        }

        public async Task<bool> ForgotPassword(LoginVM login)
        {
            var res = await Client.PostAsync<bool?>(login, "ForgotPassword");
            if (res is null || !res.Value)
            {
                Toast.Warning("An error occurs. Please contact the administrator to get your password!");
            }
            else
            {
                Toast.Success($"A recovery email has been sent to your email address. Please check and follow the steps in the email!");
            }
            return true;
        }

        public void InitAppIfEmpty()
        {
            Client.SystemRole = Client.Token.RoleIds.Contains(((int)RoleEnum.System).ToString());
            LayoutForm?.UpdateView();
            if (_initApp || Portal)
            {
                return;
            }
            _initApp = true;
            InitAppHanlder?.Invoke(Client.Token);
            var userId = Client.Token.UserId;
            if (NotificationClient is null)
            {
                //NotificationClient = new WebSocketClient("task");
            }

            if (MenuComponent is null)
            {
                Menu.Instance.Render();
            }
            if (TaskList is null)
            {
                TaskList = InitNotification();
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

        public virtual NotificationBL InitNotification()
        {
            return NotificationBL.Instance;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        public static void InitFCM(bool signout = false)
        {
            Console.WriteLine("Init fcm");
            var tanentCode = Client.Token.TenantCode.ToUpper();
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
            if (signout && push.unregister) {
                push.unregister();
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
        }
    }
}