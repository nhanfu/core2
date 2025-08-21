import React from "react";
import { ToastContainer } from "react-toastify";
import {
  Page,
  EditForm,
  Feature,
  ComponentExt,
  ChromeTabs,
  LangSelect,
  Client,
  EditableComponent,
} from "../lib/index.js";
import { Spinner } from "../lib/spinner.js";
import { LoginBL } from "./forms/login.jsx";
import "./slimselect.css";
import "./index.css";
import AppComponent from "./AppComponent.jsx";
import Decimal from "decimal.js";
import VNTank from "./components/VnTank.jsx";

export class App {
  /** @type {Page} */
  static MyApp;
  /** @type {App} */
  static _instance;
  /** @type {App} */
  static get Instance() {
    if (!this._instance) {
      this._instance = new App();
    }
    return this._instance;
  }
  /** @type {Feature} */
  Meta;
  constructor() {
    this.Meta = new Feature();
    this.Meta.ParentElement = document.getElementById("app");
    this.Meta.Layout = () => {
      return (
        <>
          <AppComponent editForm={this.MyApp.EditForm} />
        </>
      );
    };
    this.MyApp = new Page();
    this.MyApp.EditForm = new EditForm("MyApp");
    this.MyApp.EditForm.Policies = [
      {
        CanRead: true,
      },
    ];
    this.MyApp.Meta = this.Meta;
    this.MyApp.EditForm.Meta = this.Meta;
  }

  async Init() {
    Spinner.Init();
    if (Client.Token) {
      Client.GetToken(Client.Token)
        .then((token) => {
          Client.Token = token;
          LoginBL.Instance.Render();
        })
        .catch(() => {
          this.removeUser();
        });
    } else {
      LoginBL.Instance.Render();
    }
  }

  removeUser() {
    Client.Token = null;
    localStorage.removeItem("UserInfo");
    LoginBL.Instance.Render();
  }

  async RenderLayout() {
    await this.MyApp.Render();
    var el = document.querySelector(".chrome-tabs");
    if (el != null) {
      ChromeTabs.init(el);
    }
  }
}
App.Instance.Init();
