import React from "react";
import { ToastContainer } from "react-toastify";
import { Client, Html, EditForm } from "../../lib";
import { KeyCodeEnum, RoleEnum } from "../../lib/models/enum.js";
import { Toast } from "../../lib/toast.js";
import { MenuComponent } from "../components/menu.js";
import { RegisterBL } from "./register.jsx";
import "../../lib/css/login.css";
import { App } from "../app.jsx";
import { WebSocketClient } from "../../lib/clients/index.js";
import { LangSelect } from "../../lib";
import { EditableComponent } from "../../lib";
import Decimal from "decimal.js";
import { ComponentExt } from "../../lib";

export class LoginBL extends EditForm {
  static _instance;
  static _initApp;
  /** @type {MenuComponent} */
  static Menu;
  static TaskList;
  static _backdrop;

  constructor() {
    super("User");
    this.Entity = {
      AutoSignIn: true,
      TanentCode: "dev",
      UserName: "",
      Password: "",
    };
    this.Name = "Login";
    this.Title = "Đăng nhập";
    window.addEventListener("beforeunload", () =>
      this.NotificationClient?.Close()
    );
    this.Meta.IsPublic = true;
    this.Meta.Label = "Login";
    this.Title = "Login";
    this.Meta.Layout = () => {
      const logIn = async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const tanentCode = formData.get("TanentCode");
        const userName = formData.get("UserName");
        const password = formData.get("Password");
        if (!tanentCode || !userName || !password) {
          Toast.Warning("UserName or Password is required!");
          return;
        }
        const login = {
          TanentCode: tanentCode,
          UserName: userName,
          Password: password,
          AutoSignIn: true,
        };
        try {
          var res = await Client.Instance.SubmitAsync({
            Url: `/api/auth/login?t=` + tanentCode,
            JsonData: JSON.stringify(login),
            IsRawString: true,
            Method: "POST",
            AllowAnonymous: true,
          });
          Client.Token = res;
          this.InitFCM();
          if (this.SignedInHandler) {
            this.SignedInHandler(Client.Token);
          }
          this.Dispose();
          window.history.pushState(null, "Home", "");
          App.Instance.RenderLayout()
            .then(async () => {
              await this.InitAppIfEmpty();
            })
            .finally(() => {
              window.setTimeout(() => {
                Toast.Success(`Hello ` + Client.Token.FullName);
              }, 200);
            });
        } catch (error) {
          Toast.Warning(error.Message);
        }
      };
      return (
        <>
          <div className="container-login" view="login" bg={100}>
            <div className="wrap-login" type="login">
              <div className="login-form validate-form">
                <span className="login-form-logo1" />
                <span objname="jTitle" className="login-form-title">
                  LOGISTICS LOGIN
                </span>
                <form
                  className="login-form-inputs login-class"
                  objname="jInputs"
                  onSubmit={logIn}
                >
                  <div className="wrap-input username-wrap validate-input">
                    <label>Company name</label>
                    <input
                      className="input ap-lg-input"
                      type="text"
                      name="TanentCode"
                    />
                  </div>
                  <div className="wrap-input username-wrap validate-input">
                    <label>User name</label>
                    <input
                      className="input ap-lg-input"
                      type="text"
                      name="UserName"
                    />
                  </div>
                  <div className="wrap-input pass-wrap validate-input">
                    <label>Password</label>
                    <input
                      className="input ap-lg-input"
                      name="Password"
                      type="password"
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          document
                            .querySelector(".login-form-inputs")
                            .dispatchEvent(
                              new Event("submit", { bubbles: true })
                            );
                        }
                      }}
                    />
                  </div>
                  <div className="text-right" style={{ display: "flex" }}>
                    <a
                      objname="jForgot"
                      className="forgot-password"
                      target="_blank"
                      res-key="FormLogin_ForgotPassword"
                    >
                      Forgot password?
                    </a>
                    <div style={{ flex: 1 }} />
                  </div>
                  <div className="container-login-form-btn login-class">
                    <button type="submit" className="login-form-btn">
                      Login
                    </button>
                  </div>
                  <div className="register-block login-class">
                    <span res-key="FormLogin_DontHaveAccount">
                      Dont have account?
                    </span>
                    <a
                      objname="jRegister"
                      className="register-btn"
                      target="_blank"
                      res-key="FormLogin_Register"
                    >
                      Register
                    </a>
                  </div>
                </form>
              </div>
              <div objname="jCopyRight" className="text-center copy-right-text">
                Copyright © 2024
              </div>
            </div>
          </div>
          <ToastContainer />
        </>
      );
    };
  }

  /** @type {LoginBL} */
  static get Instance() {
    this._instance = new LoginBL();
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
    if (!oldToken || new Date(oldToken.RefreshTokenExp) <= Client.EpsilonNow) {
      Html.Take("#app");
      this.Element = Html.Context;
      super.Render();
      return;
    } else if (
      oldToken &&
      new Date(oldToken.AccessTokenExp) > Client.EpsilonNow
    ) {
      App.Instance.RenderLayout().then(async () => {
        await this.InitAppIfEmpty();
      });
    } else if (
      oldToken &&
      new Date(oldToken.RefreshTokenExp) > Client.EpsilonNow
    ) {
      Client.RefreshToken().then((newToken) => {
        App.Instance.RenderLayout().then(async () => {
          await this.InitAppIfEmpty();
        });
      });
    }
  }
  /**
   *
   * @param {Event} event
   * @returns {void}
   */
  KeyCodeEnter(event) {
    if (event.KeyCodeEnum() !== KeyCodeEnum.Enter) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    document.getElementById("btnLogin").click();
  }

  async Login() {
    debugger;
    let isValid = await this.IsFormValid();
    if (!isValid) {
      return false;
    }
    return this.SubmitLogin();
  }

  async Register() {
    RegisterBL.Instance.Render();
  }

  SubmitLogin() {
    const login = this.LoginEntity;
    const tcs = new Promise((resolve, reject) => {
      // @ts-ignore
      Client.Instance.SubmitAsync({
        Url: `/api/auth/login`,
        JsonData: JSON.stringify(login),
        IsRawString: true,
        Method: "POST",
        AllowAnonymous: true,
      })
        .then((res) => {
          if (!res) {
            resolve(false);
            return;
          }
          Client.Token = res;
          this.InitFCM();
          if (this.SignedInHandler) {
            this.SignedInHandler(Client.Token);
          }
          resolve(true);
          this.Dispose();
          window.history.pushState(null, "Home", "");
          App.Instance.RenderLayout()
            .then(async () => {
              await this.InitAppIfEmpty();
            })
            .finally(() => {
              window.setTimeout(() => {
                Toast.Success(`Hello ` + Client.Token.FullName);
              }, 200);
            });
        })
        .catch(() => {
          resolve(false);
          Toast.Warning("Invalid username or password");
        });
    });
    return tcs;
  }

  async ForgotPassword(login) {
    return Client.Instance.PostAsync(login, "/user/ForgotPassword").then(
      (res) => {
        if (res) {
          Toast.Warning(
            "An error occurs. Please contact the administrator to get your password!"
          );
        } else {
          Toast.Success(
            "A recovery email has been sent to your email address. Please check and follow the steps in the email!"
          );
        }
        return res;
      }
    );
  }

  async InitAppIfEmpty() {
    const systemRoleId = RoleEnum.System;
    Client.Instance.SystemRole = Client.Token.RoleIds.includes(
      systemRoleId.toString()
    );
    if (this._initApp) {
      return;
    }
    this._initApp = true;
    const json = {
      Value: null,
      Url: "/api/dictionary",
      IsRawString: true,
      Method: "GET",
    };
    var rs = await Client.Instance.SubmitAsync(json);
    localStorage.setItem("Dictionary", JSON.stringify(rs));
    const cul = localStorage.getItem("Culture") || "en";
    const map = rs
      .filter((x) => x.LangCode == cul)
      .reduce((acc, cur) => {
        acc[cur.Key] = cur.Value;
        return acc;
      }, {});
    if (!LangSelect.Culture) {
      LangSelect.Culture = cul;
    }
    LangSelect._dictionaries = map;
    localStorage.setItem(LangSelect.Culture, JSON.stringify(map));
    try {
      const json2 = {
        Value: null,
        Url: "/api/webConfig",
        IsRawString: true,
        Method: "GET",
      };
      var rsConfig = await Client.Instance.SubmitAsync(json2);
      const map = rsConfig.reduce((acc, cur) => {
        acc[cur.Id] = cur.Value;
        return acc;
      }, {});
      LangSelect._webConfig = map;
      const map2 = rsConfig.reduce((acc, cur) => {
        acc[cur.Key] = parseInt(cur.Value);
        return acc;
      }, {});
      localStorage.setItem("DP", JSON.stringify(map2));
    } catch {
      localStorage.setItem("ConfigNumber", 3);
    }
    try {
      const json3 = {
        Value: null,
        Url: "/api/salesFunction",
        IsRawString: true,
        Method: "GET",
      };
      var rsSaleFunction = await Client.Instance.SubmitAsync(json3);
      const mapSaleFunction = rsSaleFunction.reduce((acc, cur) => {
        acc[cur.Code] = cur.IsYes;
        return acc;
      }, {});
      localStorage.setItem("SalesFunction", JSON.stringify(mapSaleFunction));
    } catch {}
    this.LoadByFromUrl();

    await this.getExchangeRate();
    const json3 = {
      Value: null,
      Url: "/api/exchangeRate",
      IsRawString: true,
      Method: "GET",
    };
    var rsExt = await Client.Instance.SubmitAsync(json3);
    const ext2 = rsExt.reduce((acc, cur) => {
      acc[cur.CurrencyCode] = Decimal(cur.RateSaleVND || 0);
      return acc;
    }, {});
    const ext3 = rsExt.reduce((acc, cur) => {
      acc[cur.CurrencyCode] = Decimal(cur.RateSaleUSD || 0);
      return acc;
    }, {});
    EditableComponent.ExchangeRateSaleVND = ext2;
    EditableComponent.ExchangeRateSaleUSD = ext3;
    localStorage.setItem("ExchangeRateSaleVND", JSON.stringify(ext2));
    localStorage.setItem("ExchangeRateSaleUSD", JSON.stringify(ext3));
    //
    const ext4 = rsExt.reduce((acc, cur) => {
      acc[cur.CurrencyCode] = Decimal(cur.RateProfitVND || 0);
      return acc;
    }, {});
    const ext5 = rsExt.reduce((acc, cur) => {
      acc[cur.CurrencyCode] = Decimal(cur.RateProfitUSD || 0);
      return acc;
    }, {});
    EditableComponent.ExchangeRateProfitVND = ext4;
    EditableComponent.ExchangeRateProfitUSD = ext5;
    localStorage.setItem("ExchangeRateProfitVND", JSON.stringify(ext4));
    localStorage.setItem("ExchangeRateProfitUSD", JSON.stringify(ext5));
    window.setInterval(async () => {
      await this.getExchangeRate();
    }, 60 * 60 * 1000);
    this.InitAppHanlder?.(Client.Token);
    MenuComponent.Instance.Render();
    EditForm.NotificationClient = new WebSocketClient("apiv2.forwardx.vn/task");
  }

  async getExchangeRate() {
    try {
      const json3 = {
        Value: null,
        Url: "/api/VCBExchangeRate",
        IsRawString: true,
        Method: "GET",
      };
      var xmlString = await Client.Instance.SubmitAsync(json3);
      const parser = new DOMParser();
      const xmlDoc = parser.parseFromString(xmlString, "text/xml");
      const json = this.extractExchangeRates(xmlDoc);
      json.push({
        CurrencyCode: "VND",
        CurrencyName: "VND",
        Buy: "1",
        Transfer: "1",
        Sell: "1",
      });
      const ext = json.reduce((acc, cur) => {
        acc[cur.CurrencyCode] = Decimal(cur.Transfer.replace(/,/g, ""));
        return acc;
      }, {});
      var exUSD = Decimal(
        json.find((x) => x.CurrencyCode == "USD").Transfer.replace(/,/g, "")
      );
      const ext1 = json.reduce((acc, cur) => {
        const eurToUsdRate = Decimal(cur.Transfer.replace(/,/g, "")).div(exUSD);
        acc[cur.CurrencyCode] = eurToUsdRate;
        return acc;
      }, {});
      EditableComponent.ExchangeRateVND = ext;
      localStorage.setItem("ExchangeRateVND", JSON.stringify(ext));
      EditableComponent.ExchangeRateUSD = ext1;
      localStorage.setItem("ExchangeRateUSD", JSON.stringify(ext1));
    } catch {}
  }

  extractExchangeRates(xmlDoc) {
    const exchangeRates = [];
    const exrateElements = xmlDoc.getElementsByTagName("Exrate");

    for (let i = 0; i < exrateElements.length; i++) {
      const exrate = exrateElements[i];
      const rate = {
        CurrencyCode: exrate.getAttribute("CurrencyCode"),
        CurrencyName: exrate.getAttribute("CurrencyName").trim(),
        Buy: exrate.getAttribute("Buy"),
        Transfer: exrate.getAttribute("Transfer"),
        Sell: exrate.getAttribute("Sell"),
      };
      exchangeRates.push(rate);
    }

    return exchangeRates;
  }

  LoadByFromUrl() {
    var fName = this.GetFeatureNameFromUrl() || { pathname: "", params: null };
    if (fName.pathname == "") {
      return;
    }
    ComponentExt.InitFeatureByName(fName.pathname, true).then((tab) => {
      window.setTimeout(() => {
        if (fName.params.id) {
          Client.Instance.GetByIdAsync(tab.Meta.EntityId, [
            fName.params.id,
          ]).then((data) => {
            if (data && data.data && data.data[0]) {
              tab.OpenPopup(fName.params.popup, data.data[0]);
              window.setTimeout(() => {
                if (fName.params.popup2) {
                  var popup = tab.Children.find((x) => x.Popup);
                  Client.Instance.SubmitAsync({
                    Url: `/api/feature/getFeature`,
                    Method: "POST",
                    JsonData: JSON.stringify({
                      Name: fName.params.popup2,
                    }),
                  }).then((item) => {
                    Client.Instance.GetByIdAsync(item.EntityId, [
                      fName.params.id2,
                    ]).then((data2) => {
                      if (data2.data[0]) {
                        popup.OpenPopup(fName.params.popup2, data2.data[0]);
                      }
                    });
                  });
                }
              }, 500);
            }
          });
        }
      }, 700);
    });
    return fName;
  }

  /**
   * @returns {string | null}
   */
  GetFeatureNameFromUrl() {
    let hash = window.location.hash; // Get the full hash (e.g., '#/chat-editor?Id=-00612540-0000-0000-8000-4782e9f44882')

    if (hash.startsWith("#/")) {
      hash = hash.replace("#/", ""); // Remove the leading '#/'
    }

    if (!hash.trim() || hash == undefined) {
      return null; // Return null if the hash is empty or undefined
    }

    let [pathname, queryString] = hash.split("?"); // Split the hash into pathname and query string
    let params = new URLSearchParams(queryString); // Parse the query string into a URLSearchParams object
    if (pathname.includes("/")) {
      let segments = pathname.split("/");
      pathname = segments[segments.length - 1] || segments[segments.length - 2];
    }
    return {
      pathname: pathname || null, // Pathname (e.g., 'chat-editor')
      params: Object.fromEntries(params.entries()), // Query parameters (e.g., { Id: '-00612540-0000-0000-8000-4782e9f44882' })
    };
  }

  ToastOki() {
    Toast.Success("OKi");
  }

  InitFCM(signout = false) {
    console.log("Init fcm");
    let tenantCode = Client.Token.TenantCode;
    let strUserId = `U${Client.Token.UserId.toString().padStart(7, "0")}`;
  }

  static DiposeAll() {
    while (this.Tabs.length > 0) {
      this.Tabs[0]?.Dispose();
    }
    if (this.MenuComponent) {
      this.MenuComponent.Dispose();
    }
    if (this.TaskList) {
      this.TaskList.Dispose();
    }

    this.MenuComponent = null;
    this.TaskList = null;
  }
}
