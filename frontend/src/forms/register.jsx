import React from "react";
import { ToastContainer } from "react-toastify";
import { Client, Html, EditForm } from "../../lib";
import { KeyCodeEnum, RoleEnum } from "../../lib/models/enum.js";
import { Toast } from "../../lib/toast.js";
import { MenuComponent } from "../components/menu.js";
import "../../lib/css/login.css";
import { App } from "../app.jsx";
import { LoginBL } from "./login.jsx";

export class RegisterBL extends EditForm {
  static _instance;
  static _initApp;
  /** @type {MenuComponent} */
  static Menu;
  static TaskList;
  static _backdrop;

  constructor() {
    super("User");
    this.entity = {
      autoSignIn: true,
      username: "johndoe2",
      password: "secret",
    };
    this.name = "Register";
    this.title = "Register";
    this.public = true;
    this.Meta.isPublic = true;
    this.Meta.IsPublic = true;
    this.Meta.layout = () => (
      <>
        <div className="container-login" view="login" bg={7}>
          <div className="wrap-login" type="login">
            <div className="login-form validate-form">
              <span className="login-form-logo1" />
              <div className="login-form-inputs login-class" objname="jInputs">
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="CompanyName"
                    placeholder="Company Name"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="TenantCode"
                    placeholder="Tanent Code"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="TaxCode"
                    placeholder="Tax code"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="Email"
                    placeholder="Email"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="PhoneNumber"
                    placeholder="Phone Number"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="UserName"
                    placeholder="Số điện thoại/email"
                  />
                </div>
                <div className="wrap-input pass-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    name="Password"
                    placeholder="Mật khẩu"
                  />
                  <i objname="jBntShowPass" className="btn-show-pass" />
                </div>
              </div>
              <div className="container-login-form-btn login-class">
                <button name="btnRegister" className="login-form-btn">
                  Đăng ký
                </button>
              </div>
              <div className="register-block login-class">
                <span res-key="FormLogin_DontHaveAccount">
                  Chưa đã có công ty?
                </span>
                <a
                  objname="jRegister"
                  className="register-btn"
                  target="_blank"
                  res-key="FormLogin_Register"
                  onClick={() => this.login()}
                >
                  Đăng nhập
                </a>
              </div>
            </div>
            <div objname="jCopyRight" className="text-center copy-right-text">
              Copyright © 2012 - 2024 TINJS JSC
            </div>
          </div>
        </div>
        <ToastContainer />
      </>
    );
    this.Meta.Layout = this.Meta.layout;
    this.Meta.components = [
      {
        componentType: "Button",
        fieldName: "btnRegister",
        onClick: async () => {
          await this.register();
        },
      },
      {
        componentType: "Input",
        fieldName: "CompanyName",
        label: "Company Name",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "Email",
        label: "Email",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "TaxCode",
        label: "Tax Code",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "TenantCode",
        label: "Tanent Code",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "PhoneNumber",
        label: "Phone Number",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "UserName",
        label: "User Name",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Password",
        label: "Password",
        fieldName: "Password",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      }
    ];
  }

  /** @type {RegisterBL} */
  static get instance() {
    this._instance = new RegisterBL();
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
    if (!oldToken || new Date(oldToken.RefreshTokenExp) <= Client.EpsilonNow) {
      this.ParentElement = document.getElementById("app");
      this.Element = this.ParentElement;
      super.Render();
      return;
    } else if (
      oldToken &&
      new Date(oldToken.AccessTokenExp) > Client.EpsilonNow
    ) {
      App.instance.renderLayout().then(() => {
        this.initAppIfEmpty();
      });
    } else if (
      oldToken &&
      new Date(oldToken.RefreshTokenExp) > Client.EpsilonNow
    ) {
      Client.refreshToken().then((newToken) => {
        App.instance.renderLayout().then(() => {
          this.initAppIfEmpty();
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
    if (event.keyCodeEnum() !== KeyCodeEnum.Enter) {
      return;
    }
    event.preventDefault();
    document.getElementById("btnLogin").click();
  }

  async register() {
    let isValid = await this.isFormValid();
    if (!isValid) {
      return false;
    }
    return this.submitRegister();
  }

  async login() {
    LoginBL.instance.render();
  }

  submitRegister() {
    const login = this.loginEntity;
    const tcs = new Promise((resolve, reject) => {
      // @ts-ignore
      Client.instance.submitAsync({
        Url: `/api/auth/register`,
        JsonData: JSON.stringify(login),
        IsRawString: true,
        Method: "POST",
        AllowAnonymous: true,
      }).then((res) => {
        if (!res) {
          resolve(false);
          return;
        }
        Client.token = res.token;
        login.UserName = "";
        login.Password = "";
        this.initFCM();
        if (this.signedInHandler) {
          this.signedInHandler(Client.token);
        }
        resolve(true);
        this.dispose();
        App.instance.renderLayout()
          .then(() => {
            this.initAppIfEmpty();
          })
          .finally(() => {
            window.setTimeout(() => {
              Toast.Success(`Xin chào ` + Client.token.FullName);
            }, 200);
          });
      })
        .catch((e) => resolve(false));
    });
    return tcs;
  }

  async forgotPassword(login) {
    return Client.instance.postAsync(login, "/user/ForgotPassword").then(
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

  initAppIfEmpty() {
    const systemRoleId = RoleEnum.System;
    // @ts-ignore
    Client.instance.SystemRole = Client.token.RoleIds.includes(
      systemRoleId.toString()
    );
    if (this._initApp) {
      return;
    }
    this._initApp = true;
    this.initAppHandler?.(Client.token);
  }

  initFCM(signout = false) {
    console.log("Init fcm");
    let tenantCode = Client.token.TenantCode;
    let strUserId = `U${Client.token.UserId.toString().padStart(7, "0")}`;
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
