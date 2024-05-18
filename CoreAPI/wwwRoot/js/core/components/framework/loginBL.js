import { Client } from "clients/client";
import { WebSocketClient } from "clients/websocketClient";
import { KeyCodeEnum, RoleEnum } from "models/enum";
import EventType from "models/eventType";
import { PopupEditor } from "popupEditor";
import { Toast } from "toast";
import { Html } from "utils/html";
import { Utils } from "utils/utils";

export class LoginBL extends PopupEditor {
    static _instance;
    static _initApp;
    static MenuComponent;
    static TaskList;

    constructor() {
        super("User");
        this.Entity = {
            AutoSignIn: true,
        };
        this.Name = "Login";
        this.Title = "Đăng nhập";
        window.addEventListener("beforeunload", () => this.NotificationClient?.Close());
        this.Public = true;
        this.HeartBeat();
    }

    static get Instance() {
        if (!this._instance) {
            this._instance = new LoginBL();
        }
        return this._instance;
    }

    get LoginEntity() {
        return this.Entity;
    }

    SignedInHandler = null;
    InitAppHanlder = null;
    TokenRefreshedHandler = null;

    Render() {
        let oldToken = Client.Token;
        if (!oldToken || oldToken.RefreshTokenExp <= Client.EpsilonNow) {
            this.RenderLoginForm();
        } else if (oldToken.AccessTokenExp > Client.EpsilonNow) {
            this.InitAppIfEmpty();
        } else if (oldToken.RefreshTokenExp > Client.EpsilonNow) {
            Client.RefreshToken().then(newToken => {
                this.InitAppIfEmpty();
            });
        }
    }

    HeartBeat() {
        setInterval(() => {
            if (!Client.Token) return;
            Client.RefreshToken().then(() => {});
        }, 1000);
    }

    RenderLoginForm() {
        window.clearTimeout(this._renderAwaiter);
        this._renderAwaiter = window.setTimeout(() => {
            if (this._backdrop !== null) {
                document.body.appendChild(this._backdrop);
                this._backdrop.Show();
                return;
            }
            Html.Take("#tab-content")
                .Div.ClassName("modal is-open").Event(EventType.KeyPress, this.KeyCodeEnter);
                this._backdrop = Html.Context;
            
            Html.Instance.Div.ClassName("modal-container")
                .Div.ClassName("modal-left")
                    .H1.ClassName("modal-title").Text("XIN CHÀO").End
                    .Div.ClassName("input-block")
                        .Label.ClassName("input-label").Text("Tên tài khoản").End
                        .Input.Event("input", (e) => this.LoginEntity.UserName = e.GetInputText()).Attr("name", "UserName").Type("text").End.End
                    .Div.ClassName("input-block")
                        .Label.ClassName("input-label").Text("Mật khẩu").End
                        .Input.Event("input", (e) => this.LoginEntity.Password = e.GetInputText()).Attr("name", "Password").Value(this.LoginEntity.Password).Type("password").End.End
                    .Div.ClassName("input-block")
                        .Label.ClassName("input-label").Text("Ghi nhớ").End
                        .Label.ClassName("checkbox input-small transition-on style2")
                            .Checkbox(this.LoginEntity.AutoSignIn).Event("input", (e) => this.LoginEntity.AutoSignIn =  e.target.checked).Attr("name", "AutoSignIn").Attr("name", "AutoSignIn").End
                            .Span.ClassName("check myCheckbox").End.End.End
                    .Div.ClassName("modal-buttons")
                        .A.Href("").Text("Quên mật khẩu?").End
                        .Button.Id("btnLogin").Event("click", () => this.Login().Done()).ClassName("input-button").Text("Đăng nhập").End.End.End
                .Div.ClassName("modal-right")
                    .Img.Src("../image/bg-launch.jpg").End.Render();
            this.Element = Html.Context;
        }, 100);
    }

    KeyCodeEnter(event) {
        if (event.keyCode !== KeyCodeEnum.Enter) {
            return;
        }
        event.preventDefault();
        document.getElementById("btnLogin").click();
    }

    async Login() {
        let isValid = await this.IsFormValid();
        if (!isValid) {
            return false;
        }
        return this.SubmitLogin();
    }

    SubmitLogin() {
        const login = this.LoginEntity;
        const tcs = new Promise((resolve, reject) => {
            login.RecoveryToken = Utils.GetUrlParam("recovery");
            const domainUser = login.UserName.split('/');
            login.CompanyName = domainUser.length > 1 ? domainUser[0] : Client.Tenant;
            login.UserName = domainUser.length > 1 ? domainUser[1] : login.UserName;
            
            if (!login.CompanyName.trim()) {
                Toast.Warning("Company name must be provided!\nFor example my-compay/my-username");
                resolve(false);
                return;
            }
            
            login.Env = Client.Env;
            
            // @ts-ignore
            Client.Instance.SubmitAsync({
                Url: `/${login.CompanyName}/User/SignIn`,
                Value: JSON.stringify(login),
                IsRawString: true,
                Method: "POST",
                AllowAnonymous: true
            }).then(res => {
                if (!res) {
                    resolve(false);
                    return;
                }
                Toast.Success(`Xin chào ${res.FullName}!`);
                Client.Token = res;
                login.UserName = "";
                login.Password = "";
                this.InitAppIfEmpty();
                this.InitFCM();
                if (this.SignedInHandler) {
                    this.SignedInHandler(Client.Token);
                }
                resolve(true);
                this.Dispose();
            }).catch(e => resolve(false));
        });
        return tcs;
    }

    async ForgotPassword(login) {
        return Client.Instance.PostAsync(login, "/user/ForgotPassword").then(res => {
            if (res) {
                Toast.Warning("An error occurs. Please contact the administrator to get your password!");
            } else {
                Toast.Success("A recovery email has been sent to your email address. Please check and follow the steps in the email!");
            }
            return res;
        });
    }

    InitAppIfEmpty() {
        const systemRoleId = RoleEnum.System;
        // @ts-ignore
        Client.SystemRole = Client.Token.RoleIds.includes(systemRoleId.toString());
    if (this._initApp) {
        return;
    }
        this._initApp = true;
        this.InitAppHanlder?.(Client.Token);
        const userId = Client.Token.UserId;
        if (!this.NotificationClient) {
            this.NotificationClient = new WebSocketClient("task");
        }
    
        if (!LoginBL.MenuComponent) {
            LoginBL.MenuComponent.Instance.Render();
        }
        if (!LoginBL.TaskList) {
            LoginBL.TaskList = NotificationBL.Instance;
            LoginBL.TaskList.Render();
            LoginBL.TaskList.DOMContentLoaded = () => {
                document.getElementById("name-user").textContent = Client.Token.UserName;
                document.getElementById("Username-text").textContent = Client.Token.FullName;
                document.getElementById("text-address").textContent = Client.Token.Address;
                Html.Take("#user-image").Src("./image/" + Client.Token.Avatar);
                Html.Take("#img-detail").Src("./image/" + Client.Token.Avatar);
            };
        }
    }
    

    InitFCM(signout = false) {
        console.log("Init fcm");
        let tenantCode = Client.Token.TenantCode;
        let strUserId = `U${Client.Token.UserId.toString().padStart(7, '0')}`;
    }

    static DiposeAll() {
        while (this.Tabs.length > 0) {
            this.Tabs[0]?.Dispose();
        }
        if (this.MenuComponent !== null) {
            this.MenuComponent.Dispose();
        }
        if (this.TaskList !== null) {
            this.TaskList.Dispose();
        }
    
        this.MenuComponent = null;
        this.TaskList = null;
    }

    Dispose() {
       this._backdrop.Hide();
        Html.Take(".is-open").Clear();
        
    }
}
