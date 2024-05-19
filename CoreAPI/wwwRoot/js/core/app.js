import { WebSocketClient } from "./clients/websocketClient.js";
import { Client } from "./clients/client.js";
import EventType from "./models/eventType.js";
import { ComponentExt } from "./utils/componentExt.js";
import { LangSelect } from "./utils/langSelect.js";
import { StringBuilder } from "./utils/stringBuilder.js";
import { Utils } from "./utils/utils.js";
import { EditForm } from "./editForm.js";
import { Spinner } from "./spinner.js";
import { LoginBL } from "./forms/loginBL.js";
import { NotificationBL } from "./forms/notificationBL.js";
export class App {
    // @ts-ignore
    static DefaultFeature = Utils.HeadChildren.layout?.content || "index";
    static FeatureLoaded = false;

    /**
     * The main entry point of the application.
     */
    static Main() {
        if (!LangSelect.Culture) {
            LangSelect.Culture = "vi";
        }
        LangSelect.Translate().Done((x) => {
            App.InitApp();
        });
    }

    /**
     * Initializes the application.
     */
    static InitApp() {
        Client.ModelNamespace = 'TMS.Models.';
        Spinner.Init();
        if (Client.IsPortal) {
            App.InitPortal();
        } else {
            LoginBL.Instance.Render();
        }
        App.AlterDeviceScreen();
    }

    /**
     * Initializes the portal part of the application.
     */
    static InitPortal() {
        LoginBL.Instance.SignedInHandler += (x) => window.location.reload();
        App.LoadByFromUrl();
        NotificationBL.Instance.Render();
        EditForm.NotificationClient = new WebSocketClient("task");
        window.addEventListener(EventType.PopState, (e) => {
            App.LoadByFromUrl();
        });
    }

    /**
     * Loads a feature by name from the URL.
     * @returns {string} The name of the feature loaded.
     */
    static LoadByFromUrl() {
        const fName = App.GetFeatureNameFromUrl() || App.DefaultFeature;
        ComponentExt.InitFeatureByName(fName, true).Done();
        return fName;
    }

    /**
     * Retrieves the feature name from the URL.
     * @returns {string|null} The feature name or null if none is found.
     */
    static GetFeatureNameFromUrl() {
        let builder = new StringBuilder();
        let feature = window.location.pathname.toLowerCase().replace(Client.BaseUri.toLowerCase(), "");
        if (feature.startsWith(Utils.Slash)) {
            feature = feature.substring(1);
        }
        if (!feature.trim()) {
            return null;
        }
        for (let i = 0; i < feature.length; i++) {
            if (feature[i] === '?' || feature[i] === '#') break;
            builder.Append(feature[i]);
        }
        return builder.toString();
    }

    /**
     * Adjusts the device screen settings based on the device type.
     */
    static AlterDeviceScreen() {
        const iPhone = /(iPod|iPhone)/.test(window.navigator.userAgent)
                        && /AppleWebKit/.test(window.navigator.userAgent);
        if (iPhone) {
            let className = "iphone ";
            if (window.screen.availHeight === 812 && window.screen.availWidth === 375) {
                className += "portrait";
            } else {
                className += "landscape";
            }
            document.documentElement.className = className;
        }
    }

    /**
     * Checks if the device is mobile.
     * @returns {boolean} True if mobile, false otherwise.
     */
    static IsMobile() {
        // @ts-ignore
        return typeof cordova !== 'undefined';
    }
}
