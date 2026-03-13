import React from "react";
import { ToastContainer } from "react-toastify";
import { Client, Html, EditForm } from "../../lib";
import { keyCodeEnum, roleEnum } from "../../lib/models/enum.js";
import { Toast } from "../../lib/toast.js";
import { MenuComponent } from "../components/menu.js";
import { RegisterBL } from "./register.jsx";
import "../../lib/css/login.css";
import { App } from "../app.jsx";
import { LangSelect } from "../../lib";
import { EditableComponent } from "../../lib";
import Decimal from "decimal.js";
import { ComponentExt } from "../../lib";

export class LoginBL extends EditForm {
  static _instance;
  static _initApp;
  /** @type {MenuComponent} */
  static Menu;
  static taskList;
  static _backdrop;

  constructor() {
    super("User");
    this.entity = {
      autoSignIn: true,
      userName: "",
      password: "",
    };
    this.name = "Login";
    this.title = "Đăng nhập";
    this.login = true;
    this.meta.isPublic = true;
    this.meta.isPublic = true;
    this.meta.label = "Login";
    this.meta.label = "Login";
    this.title = "Login";
    this.meta.layout = () => {
      const logIn = async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const userName = formData.get("userName");
        const password = formData.get("Password");
        if (!userName || !password) {
          Toast.Warning("userName or Password is required!");
          return;
        }
        const login = {
          userName: userName,
          password: password,
          autoSignIn: true,
        };
        try {
          const res = await Client.instance.submitAsync({
            Url: `/api/auth/login`,
            jsonData: JSON.stringify(login),
            isRawString: true,
            Method: "POST",
            allowAnonymous: true,
          });
          const token = res?.data ?? res;
          if (!token?.accessToken) {
            throw new Error(res?.message || "Login failed");
          }
          Client.token = token;
          this.initFCM();
          if (this.signedInHandler) {
            this.signedInHandler(Client.token);
          }
          this.dispose();
          window.history.pushState(null, "Home", "");
          App.instance.renderLayout()
            .then(async () => {
              await this.initAppIfEmpty();
            })
            .finally(() => {
              window.setTimeout(() => {
                Toast.Success(`Hello ` + Client.token.fullName);
              }, 200);
            });
        } catch (error) {
          Toast.Warning(error?.message || error?.Message || "Login failed");
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
                    <label>User name</label>
                    <input
                      className="input ap-lg-input"
                      type="text"
                      name="userName"
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
                      res-key="formLogin_ForgotPassword"
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
                    <span res-key="formLogin_DontHaveAccount">
                      Dont have account?
                    </span>
                    <a
                      objname="jRegister"
                      className="register-btn"
                      target="_blank"
                      res-key="formLogin_Register"
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
    this.meta.Layout = this.meta.layout;
  }

  /** @type {LoginBL} */
  static get instance() {
    this._instance = new LoginBL();
    return this._instance;
  }

  get loginEntity() {
    return this.entity;
  }

  signedInHandler = null;
  initAppHandler = null;
  tokenRefreshedHandler = null;

  render() {
    let oldToken = Client.token;
    if (!oldToken || new Date(oldToken.refreshTokenExp) <= Client.epsilonNow) {
      this.parentElement = document.getElementById("app");
      this.element = this.parentElement;
      super.render();
      return;
    } else if (
      oldToken &&
      new Date(oldToken.accessTokenExp) > Client.epsilonNow
    ) {
      App.instance.renderLayout().then(async () => {
        await this.initAppIfEmpty();
      });
    } else if (
      oldToken &&
      new Date(oldToken.refreshTokenExp) > Client.epsilonNow
    ) {
      Client.refreshToken().then((newToken) => {
        App.instance.renderLayout().then(async () => {
          await this.initAppIfEmpty();
        });
      });
    }
  }
  /**
   *
   * @param {Event} event
   * @returns {void}
   */
  keyCodeEnter(event) {
    if (event.keyCodeEnum() !== keyCodeEnum.Enter) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    document.getElementById("btnLogin").click();
  }

  async login() {
    debugger;
    let isValid = await this.isFormValid();
    if (!isValid) {
      return false;
    }
    return this.submitLogin();
  }

  async register() {
    RegisterBL.instance.render();
  }

  submitLogin() {
    const login = this.loginEntity;
    const tcs = new Promise((resolve, reject) => {
      // @ts-ignore
      Client.instance.submitAsync({
        Url: `/api/auth/login`,
        jsonData: JSON.stringify(login),
        isRawString: true,
        Method: "POST",
        allowAnonymous: true,
      })
        .then((res) => {
          const token = res?.data ?? res;
          if (!token?.accessToken) {
            resolve(false);
            Toast.Warning(res?.message || "Invalid username or password");
            return;
          }
          Client.token = token;
          this.initFCM();
          if (this.signedInHandler) {
            this.signedInHandler(Client.token);
          }
          resolve(true);
          this.dispose();
          window.history.pushState(null, "Home", "");
          App.instance.renderLayout()
            .then(async () => {
              await this.initAppIfEmpty();
            })
            .finally(() => {
              window.setTimeout(() => {
                Toast.Success(`Hello ` + Client.token.fullName);
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

  async forgotPassword(login) {
    return Client.instance.postAsync(login, "/user/forgotPassword").then(
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

  async initAppIfEmpty() {
    const systemRoleId = roleEnum.System;
    Client.instance.systemRole = Client.token.roleIds.includes(
      systemRoleId.toString()
    );
    if (this._initApp) {
      return;
    }
    this._initApp = true;
    this.loadByFromUrl();
    this.initAppHandler?.(Client.token);
    MenuComponent.instance.render();
  }

  loadByFromUrl() {
    var fName = this.getFeatureNameFromUrl() || { pathname: "", params: null };
    if (fName.pathname == "") {
      return;
    }
    ComponentExt.initFeatureByName(fName.pathname, true).then((tab) => {
      window.setTimeout(() => {
        if (fName.params.id) {
          Client.instance.getByIdAsync(tab.meta.entityId, [
            fName.params.id,
          ]).then((data) => {
            if (data && data.data && data.data[0]) {
              tab.openPopup(fName.params.popup, data.data[0]);
              window.setTimeout(() => {
                if (fName.params.popup2) {
                  var popup = tab.children.find((x) => x.popup);
                  Client.instance.submitAsync({
                    Url: `/api/feature/loadFeature`,
                    Method: "POST",
                    jsonData: JSON.stringify({
                      Name: fName.params.popup2,
                    }),
                  }).then((item) => {
                    Client.instance.getByIdAsync(item.entityId, [
                      fName.params.id2,
                    ]).then((data2) => {
                      if (data2.data[0]) {
                        popup.openPopup(fName.params.popup2, data2.data[0]);
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
  getFeatureNameFromUrl() {
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

  toastOki() {
    Toast.Success("oKi");
  }

  initFCM(signout = false) {
    console.log("Init fcm");
    let tenantCode = Client.token.tenantCode;
    let strUserId = `U${Client.token.userId.toString().padStart(7, "0")}`;
  }

  static diposeAll() {
    while (this.Tabs.length > 0) {
      this.Tabs[0]?.dispose();
    }
    if (this.menuComponent) {
      this.menuComponent.dispose();
    }
    if (this.taskList) {
      this.taskList.dispose();
    }

    this.menuComponent = null;
    this.taskList = null;
  }
}
