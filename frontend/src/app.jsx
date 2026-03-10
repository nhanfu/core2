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
import "./slimselect3.css";
import "./index.css";
import AppComponent from "./AppComponent.jsx";
import Decimal from "decimal.js";
import VNTank from "./components/VnTank.jsx";

export class App {
  /** @type {Page} */
  static myApp;
  /** @type {App} */
  static _instance;
  /** @type {App} */
  static get instance() {
    if (!this._instance) {
      this._instance = new App();
    }
    return this._instance;
  }
  /** @type {Feature} */
  meta;
  constructor() {
    this.meta = new Feature();
    this.meta.parentElement = document.getElementById("app");
    this.meta.layout = () => {
      return (
        <>
          <AppComponent editForm={this.myApp.editForm} />
        </>
      );
    };
    this.myApp = new Page();
    this.myApp.editForm = new EditForm("MyApp");
    this.myApp.editForm.policies = [
      {
        canRead: true,
      },
    ];
    this.myApp.meta = this.meta;
    this.myApp.editForm.meta = this.meta;
  }

  async init() {
    Spinner.Init();
    LoginBL.instance.render();
  }

  removeUser() {
    Client.Token = null;
    localStorage.removeItem("UserInfo");
    LoginBL.instance.render();
  }

  async renderLayout() {
    await this.myApp.render();
    var el = document.querySelector(".chrome-tabs");
    if (el != null) {
      ChromeTabs.init(el);
    }
  }
}
App.instance.init();
