import React from "react";
import { ToastContainer } from "react-toastify";
import { Client, Html, EditForm } from "../../lib";
import { keyCodeEnum, roleEnum } from "../../lib/models/enum.js";
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
  static taskList;
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
    this.meta.isPublic = true;
    this.meta.isPublic = true;
    this.meta.layout = () => (
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
                    name="companyName"
                    placeholder="Company Name"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="tenantCode"
                    placeholder="Tanent Code"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="taxCode"
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
                    name="phoneNumber"
                    placeholder="Phone Number"
                  />
                </div>
                <div className="wrap-input username-wrap validate-input">
                  <input
                    className="input ap-lg-input"
                    type="text"
                    name="userName"
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
                <span res-key="formLogin_DontHaveAccount">
                  Chưa đã có công ty?
                </span>
                <a
                  objname="jRegister"
                  className="register-btn"
                  target="_blank"
                  res-key="formLogin_Register"
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
    this.meta.Layout = this.meta.layout;
    this.meta.components = [
      {
        componentType: "Button",
        fieldName: "btnRegister",
        onClick: async () => {
          await this.register();
        },
      },
      {
        componentType: "Input",
        fieldName: "companyName",
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
        fieldName: "taxCode",
        label: "Tax Code",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "tenantCode",
        label: "Tanent Code",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "phoneNumber",
        label: "Phone Number",
        validation: `[{"Rule": "required", "Message": "{0} is required"}]`
      },
      {
        componentType: "Input",
        fieldName: "userName",
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
    if (!Client.hasUsableRefreshToken(oldToken)) {
      this.parentElement = document.getElementById("app");
      this.element = this.parentElement;
      super.render();
      return;
    } else if (Client.hasUsableAccessToken(oldToken)) {
      App.instance.renderLayout().then(() => {
        this.initAppIfEmpty();
      });
    } else if (Client.hasUsableRefreshToken(oldToken)) {
      Client.refreshToken().then(() => {
        if (!Client.hasUsableRefreshToken(Client.token)) {
          this.parentElement = document.getElementById("app");
          this.element = this.parentElement;
          super.render();
          return;
        }
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
    if (event.keyCodeEnum() !== keyCodeEnum.Enter) {
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
        url: `/api/auth/register`,
        jsonData: JSON.stringify(login),
        isRawString: true,
        method: "POST",
        allowAnonymous: true,
      }).then((res) => {
        if (!res) {
          resolve(false);
          return;
        }
        Client.token = res.token;
        login.userName = "";
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
              Toast.success(`Xin chào ` + Client.token.fullName);
            }, 200);
          });
      })
        .catch((e) => resolve(false));
    });
    return tcs;
  }

  async forgotPassword(login) {
    return Client.instance.postAsync(login, "/user/forgotPassword").then(
      (res) => {
        if (res) {
          Toast.warning(
            "An error occurs. Please contact the administrator to get your password!"
          );
        } else {
          Toast.success(
            "A recovery email has been sent to your email address. Please check and follow the steps in the email!"
          );
        }
        return res;
      }
    );
  }

  initAppIfEmpty() {
    const systemRoleId = roleEnum.System;
    // @ts-ignore
    Client.instance.systemRole = Client.token.roleIds.includes(
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
