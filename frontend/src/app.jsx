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
    this.meta.ParentElement = this.meta.parentElement;
    this.meta.layout = () => {
      return (
        <>
          <AppComponent editForm={this.myApp.EditForm} />
        </>
      );
    };
    this.meta.Layout = this.meta.layout;
    this.myApp = new Page();
    this.myApp.EditForm = new EditForm("MyApp");
    this.myApp.editForm = this.myApp.EditForm;
    this.myApp.EditForm.policies = [
      {
        canRead: true,
      },
    ];
    this.myApp.Meta = this.meta;
    this.myApp.meta = this.meta;
    this.myApp.EditForm.Meta = this.meta;
    this.myApp.EditForm.meta = this.meta;
  }

  async init() {
    Spinner.Init();
    LoginBL.instance.render();
  }

  removeUser() {
    Client.token = null;
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
