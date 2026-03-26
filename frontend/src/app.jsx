import React from "react";
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
import AppComponent from "./appComponent.jsx";
import Decimal from "decimal.js";
import vNTank from "./components/vnTank.jsx";

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
    this.meta.parentElement = this.meta.parentElement;
    this.meta.layout = () => {
      return (
        <>
          <AppComponent editForm={this.myApp.editForm} />
        </>
      );
    };
    this.meta.Layout = this.meta.layout;
    this.myApp = new Page();
    this.myApp.editForm = new EditForm("myApp");
    this.myApp.editForm = this.myApp.editForm;
    this.myApp.editForm.policies = [
      {
        canRead: true,
      },
    ];
    this.myApp.Meta = this.meta;
    this.myApp.meta = this.meta;
    this.myApp.editForm.Meta = this.meta;
    this.myApp.editForm.meta = this.meta;
  }

  async init() {
    Spinner.Init();
    const savedToken = localStorage.getItem("userInfo");

    if (!savedToken) {
      Client.token = null;
      LoginBL.instance.render();
      return;
    }
    const parsedToken = JSON.parse(savedToken);
    Client.token = parsedToken;
    LoginBL.instance.render();
  }

  removeUser() {
    Client.token = null;
    localStorage.removeItem("userInfo");
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
