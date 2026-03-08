import React, { useEffect, useState } from "react";
import { ToastContainer } from "react-toastify";
import { Client } from "../lib";
import UserDropdown from "./components/userDropdown.jsx";
import NotificationDropdown from "./components/NotificationDropdown.jsx";
import LangComponent from "./components/LangComponent.jsx";
import store from "./redux/store.js";
import { Provider } from "react-redux";
import UserActive from "./components/userActive.jsx";
import ExchangeRate from "./components/ExchangeRate.jsx";
const AppComponent = ({ editForm }) => {
  const [isMobile, setIsMobile] = useState(window.innerWidth <= 1024);

  useEffect(() => {
    const checkIsMobile = () => {
      const matches = window.matchMedia(
        "only screen and (max-width: 1024px)"
      ).matches;
      setIsMobile(matches);
      const body = document.querySelector("body");
      if (matches) {
        body.classList.add("collapse-sidebar");
      } else {
        body.classList.remove("collapse-sidebar");
      }
    };
    checkIsMobile();
    window.addEventListener("resize", checkIsMobile);
    return () => window.removeEventListener("resize", checkIsMobile);
  }, []);

  const actionToggle = (e) => {
    e.preventDefault();
    var t = document.querySelector("body");
    var main = document.querySelector(".main-sidebar");
    if (t.classList.contains("collapse-sidebar")) {
      t.classList.add("expand-sidebar");
      t.classList.remove("collapse-sidebar");
      if (isMobile) {
        main.focus();
      }
    } else {
      t.classList.remove("expand-sidebar");
      t.classList.add("collapse-sidebar");
    }
  };

  const actionBlur = (e) => {
    e.preventDefault();
    var t = document.querySelector("body");
    if (!t.classList.contains("expand-sidebar")) {
      return;
    }
    actionToggle(e);
  };

  return (
    <Provider store={store}>
      <header className="header-navbar">
        <div className="header-wrapper">
          <div className="header-left">
            <div
              className="sidebar-toggle action-toggle"
              onClick={actionToggle}
            >
              <i className="fal fa-bars"></i>
            </div>
          </div>
          <div className="chrome-tabs">
            <div className="chrome-tabs-content"></div>
          </div>
          <div className="header-content">
            <LangComponent />
            <UserActive />
            <NotificationDropdown />
            <UserDropdown editForm={editForm} />
          </div>
        </div>
      </header>
      <nav className="main-sidebar ps-menu" onBlur={actionBlur} tabIndex="-1">
        <div className="sidebar-header">
          <a className="text">
            <img src={Client.Token.Vendor?.Logo} />
          </a>
        </div>
        <div className="search-content p-2"></div>
        <div className="sidebar-content"></div>
      </nav>
      <div className="main-content" id="tab-content"></div>
      <ToastContainer />
      <ExchangeRate />
    </Provider>
  );
};
export default AppComponent;
