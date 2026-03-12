import { SqlViewModel } from "../models/sqlViewModel.js";
import { badGatewayQueue } from "../models/badGatewayQueue.js";
import { Token } from "../models/token.js";
import { Utils } from "../utils/utils.js";
import { PatchVM } from "../models/patch.js";
import { EmailVM } from "../models/emailVM.js";
import { Toast } from "../toast.js";
import { Path } from "../utils/path.js";
import { Entity } from "../models/enum.js";
import { Action } from "../models/action.js";
import { encode, decode } from "@msgpack/msgpack";

export class Client {
    /** @type {Entity[]} */
    static Entities = [];
    static epsilonNow = new Date(Date.now() + (1 * 60 * 1000));
    static errorMessage = "Hệ thống đang cập nhật vui lòng chờ trong 30s!";
    static modelNamespace;
    static entities;
    static token;
    static guidLength = 36;
    // @ts-ignore
    static Host = (import.meta.env?.VITE_API_HOST || window.location.host).toLowerCase();
    // @ts-ignore
    static baseUri = (import.meta.env?.VITE_API_BASE_URI || window.location.origin).toLowerCase();
    // @ts-ignore
    static isPortal = ((import.meta.env?.VITE_IS_PORTAL || import.meta.env?.VITE_STARTUP || "").toLowerCase() !== "admin");
    // @ts-ignore
    static metaConn = import.meta.env?.VITE_META_CONN || "default";
    // @ts-ignore
    static dataConn = import.meta.env?.VITE_DATA_CONN || "bl";
    // @ts-ignore
    static Tenant = import.meta.env?.VITE_TENANT || "System";
    // @ts-ignore
    static Env = import.meta.env?.VITE_ENV || "test";
    // @ts-ignore
    static fileFTP = import.meta.env?.VITE_FILE_FTP || "/user";
    // @ts-ignore
    /** @type {string} */
    static apiV2 = import.meta.env?.VITE_API_V2_URL;
    static api = import.meta.env?.VITE_API_URL;
    // @ts-ignore
    static Config = document.head.config?.content || "";
    static badGatewayRequest = new badGatewayQueue();
    static unAuthorizedEventHandler = new Action();
    static signOutEventHandler = new Action();
    // @ts-ignore
    static get Origin() { return document.head.origin?.content || window.location.origin; }
    _nameSpace;
    _config;
    customPrefix = (() => {
        const prefixElement = Array.from(document.head.children).find(x => x instanceof HTMLMetaElement && x.name === "prefix");
        return prefixElement?.content;
    })();

    constructor(entityName, ns = "", config = false) {
        this._nameSpace = ns;
        this._config = config;
        if (this._nameSpace && this._nameSpace.charAt(this._nameSpace.length - 1) !== '.') {
            this._nameSpace += '.';
        }
        this.entityName = entityName;
    }

    /** @type {Client} */
    static _instance;
    /** @type {Client} */
    static get instance() {
        if (!Client._instance) {
            Client._instance = new Client();
        }
        return Client._instance;
    }
    /** @type {Token} */
    static get token() {
        return JSON.parse(localStorage.getItem('userInfo'));
    }

    static set token(value) {
        localStorage.setItem('userInfo', JSON.stringify(value));
    }
    static get systemRole() {
        return Client.token.roleNames.some(x => x.toLowerCase() == "admin");
    }
    static get bodRole() {
        return Client.token.roleNames.some(x => x.toLowerCase() == "bod");
    }
    /**
     * @param {SqlViewModel} vm
     */
    async userSvc(vm, annonymous = false) {
        /** @type {XHRWrapper} */
        // @ts-ignore
        const data = {
            Value: JSON.stringify(vm),
            Url: Utils.userSvc,
            isRawString: true,
            Method: "POST",
            allowAnonymous: annonymous
        };
        return this.submitAsync(data);
    }

    async comQuery(vm) {
        /** @type {XHRWrapper} */
        // @ts-ignore
        const data = {
            Value: JSON.stringify(vm),
            Url: Utils.comQuery,
            isRawString: true,
            Method: "POST"
        };
        return this.submitAsync(data);
    }

    async submitAsyncWithToken(options) {
        const isFormData = !!options.formData;
        const useMsgPack = false;
        options.Headers = {
            ...(!options.Headers && !isFormData && !useMsgPack && { "Content-Type": "application/json" }),
            ...(!options.Headers && !isFormData && useMsgPack && { "Content-Type": "application/msgpack" }),
            ...(options.Headers || {}),
            ...(!options.allowAnonymous && { Authorization: `Bearer ${Client.token?.accessToken}` }),
            ...(useMsgPack ? { Accept: "application/msgpack, application/json" } : {}),
            "User-Agent": "Mozilla/5.0"
        };

        const url = Client.api + (options.finalUrl ?? options.Url);
        console.log('[DEBUG submitAsyncWithToken] Client.api:', Client.api, 'Url:', options.Url, 'finalUrl:', options.finalUrl, 'Constructed url:', url);

        try {
            const response = await fetch(url, {
                method: options.Method,
                headers: options.Headers,
                body: isFormData ? options.formData : options.jsonData
            });

            const contentType = (response.headers.get("Content-Type") || "").toLowerCase();
            const disposition = response.headers.get("Content-Disposition") || "";

            if (!response.ok) {
                if (contentType.includes("application/msgpack") || contentType.includes("application/x-msgpack")) {
                    const buf = await response.arrayBuffer();
                    const err = decode(new uint8Array(buf));
                    return Promise.reject(err);
                }
                try {
                    const errJson = await response.json();
                    return Promise.reject(errJson);
                } catch {
                    const errText = await response.text();
                    return Promise.reject(errText);
                }
            }

            if (contentType.includes("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")) {
                const blob = await response.blob();
                return blob;
            }

            if (contentType.includes("application/msgpack") || contentType.includes("application/x-msgpack")) {
                const buf = await response.arrayBuffer();
                return decode(new uint8Array(buf));
            }

            if (contentType.includes("application/json")) {
                return response.json();
            }

            return response.text();
        } catch (error) {
            return Promise.reject(error);
        }
    }
    /**
     * 
     * @param {XHRWrapper} options 
     * @returns 
     */
    async submitAsync(options) {
        if (!options.allowAnonymous) {
            await Client.refreshToken();
        }
        return await this.submitAsyncWithToken(options);
    }
    /**
     * @param {[]} arrays
     */
    async getByIdsAsync(arrays) {
        const data = {
            jsonData: JSON.stringify(arrays),
            Url: Utils.comQuerys,
            isRawString: true,
            Method: "POST"
        };
        return this.submitAsync(data);
    }
    /**
     * @param {string} table
     * @param {string} connKey
     * @param {string[]} ids
     */
    async getByIdAsync(table, ids) {
        const data = {
            jsonData: JSON.stringify({ Table: table, Id: ids }),
            Url: Utils.comQuery,
            isRawString: true,
            Method: "POST"
        };
        return this.submitAsync(data);
    }
    /**
     * @param {string} table
     * @param {string} connKey
     * @param {string[]} ids
     */
    async getByNameAsync(table, ids, format) {
        const data = {
            jsonData: JSON.stringify({ Table: table, Id: ids, Format: format }),
            Url: Utils.comQueryByName,
            isRawString: true,
            Method: "POST"
        };
        return this.submitAsync(data);
    }

    async notificationUser(entity, ...user) {
        const data = {
            jsonData: JSON.stringify({ Entity: entity, Rule: user }),
            Url: "/api/feature/notificationuser",
            isRawString: true,
            Method: "POST"
        };
        return this.submitAsync(data);
    }

    async notificationRole(entity, ...role) {
        const data = {
            jsonData: JSON.stringify({ Entity: entity, Rule: role }),
            Url: "/api/feature/notificationrole",
            isRawString: true,
            Method: "POST"
        };
        return this.submitAsync(data);
    }

    /**
     * @param {string} name
     */
    async getService(name) {
        const data = {
            jsonData: JSON.stringify({ Name: name }),
            Url: "/api/feature/getService",
            Method: "POST"
        };
        return this.submitAsync(data);
    }

    async postAsync(value, subUrl = "", annonymous = false) {
        /** @type {XHRWrapper} */
        // @ts-ignore
        const data = {
            jsonData: JSON.stringify(value),
            Url: subUrl,
            Method: "POST",
            allowAnonymous: annonymous,
        };
        return this.submitAsync(data);
    }

    /**
     * 
     * @param {PatchVM | PatchVM[]} value 
     * @param {function} errHandler 
     * @param {boolean} annonymous 
     * @returns {Promise<any>} Effected rows in the database
     */
    async patchAsync(value, errHandler = null, annonymous = false) {
        /** @type {XHRWrapper} */
        // @ts-ignore
        const data = {
            jsonData: JSON.stringify(value),
            isRawString: true,
            Url: Utils.patchSvc,
            Headers: { "Content-type": "application/json" },
            Method: "PATCH",
            allowAnonymous: annonymous,
            errorHandler: errHandler
        };
        return this.submitAsync(data);
    }

    /**
     * 
     * @param {PatchVM[]} value 
     * @param {function} errHandler 
     * @param {boolean} annonymous 
     * @returns {Promise<any>} Effected rows in the database
     */
    async patchAsync2(value, errHandler = null, annonymous = false) {
        /** @type {XHRWrapper} */
        // @ts-ignore
        const data = {
            jsonData: JSON.stringify(value),
            isRawString: true,
            Url: Utils.patchSvcs,
            Headers: { "Content-type": "application/json" },
            Method: "PATCH",
            allowAnonymous: annonymous,
            errorHandler: errHandler
        };
        return this.submitAsync(data);
    }

    async postFilesAsync(file, url = "", progressHandler = null) {
        const formData = new FormData();
        formData.append("file", file);
        /** @type {XHRWrapper} */
        // @ts-ignore
        const data = {
            formData: formData,
            File: file,
            progressHandler: progressHandler,
            Method: "POST",
            Url: url
        };
        return await this.submitAsync(data);
    }

    /**
     * @param {EmailVM} email
     */
    async sendMail(email) {
        // @ts-ignore
        return this.submitAsync({
            Value: email,
            Method: "POST",
            Url: "Email"
        });
    }

    /**
     * @param {string[]} ids
     * @param {string} table
     * @param {string} connKey
     */
    async deactivateAsync(ids, table, connKey) {
        const vm = {
            Ids: ids,
            Params: table,
            metaConn: Client.metaConn,
            dataConn: connKey || Client.dataConn
        };
        // @ts-ignore
        return this.submitAsync({
            Url: Utils.deactivateSvc,
            Value: JSON.stringify(vm),
            Method: "DELETE",
            isRawString: true,
            Headers: {
                "Content-type": "application/json"
            }
        });
    }

    async hardDeleteAsync(ids, table, newId, comId) {
        const vm = {
            Table: table,
            newId: newId || null,
            comId: comId || null,
            Delete: [
                {
                    Table: table,
                    Ids: ids
                }
            ],
        };
        return this.submitAsync({
            Url: Utils.deleteSvc,
            jsonData: JSON.stringify(vm),
            Method: "DELETE",
            isRawString: true,
            Headers: {
                "Content-type": "application/json"
            }
        });
    }

    async getConfig(name, scope = 'global') {
        return Client.instance.userSvc({
            metaConn: this.metaConn,
            dataConn: this.dataConn,
            comId: "UserSetting",
            Action: "getConfig",
            Params: JSON.stringify({ name: name, scope: scope })
        });
    }

    static async loadScript(src) {
        const scriptExists = Array.from(document.body.children).some(x => x instanceof hTMLScriptElement && x.src.split("/").pop() === src.split("/").pop());
        if (scriptExists) return true;
        const tcs = new Promise((resolve) => {
            const script = document.createElement("script");
            script.src = src;
            script.addEventListener("load", () => {
                resolve(true);
            });
            script.onerror = () => {
                resolve(true);
                return false;
            };
            document.body.appendChild(script);
        });
        return tcs;
    }

    static async refreshToken(success = null) {
        const oldToken = Client.token;
        console.log('[DEBUG refreshToken] oldToken:', oldToken ? { accessTokenExp: oldToken.accessTokenExp, refreshTokenExp: oldToken.refreshTokenExp } : null);
        console.log('[DEBUG refreshToken] epsilonNow:', Client.epsilonNow);
        if (!oldToken || new Date(oldToken.refreshTokenExp) <= Client.epsilonNow) {
            console.log('[DEBUG refreshToken] Case 1: No token or refreshToken expired');
            return null;
        }
        if (new Date(oldToken.accessTokenExp) > Client.epsilonNow) {
            console.log('[DEBUG refreshToken] Case 2: accessToken still valid, returning old token');
            return oldToken;
        }
        if (new Date(oldToken.accessTokenExp) <= Client.epsilonNow && new Date(oldToken.refreshTokenExp) > Client.epsilonNow) {
            console.log('[DEBUG refreshToken] Case 3: Need to refresh token');
            const newToken = await Client.getToken(oldToken);
            console.log('[DEBUG refreshToken] Got newToken:', newToken);
            if (newToken) {
                Client.token = newToken;
                success?.(newToken);
            }
            return newToken;
        }
        console.log('[DEBUG refreshToken] Case 4: refreshToken also expired');
        return null;
    }

    /**
     * @param {Token} oldToken
     */
    static async getToken(oldToken) {
        // @ts-ignore
        const newToken = await Client.instance.submitAsync({
            noQueue: true,
            Url: `/api/auth/refreshToken?t=${Client.token.tenantCode || Client.Tenant}`,
            Method: "POST",
            jsonData: JSON.stringify({ refreshToken: oldToken.refreshToken, accessToken: oldToken.accessToken }),
            allowAnonymous: true,
            errorHandler: (xhr) => {
                if (xhr.status === 400) {
                    Client.token = null;
                    Toast.Warning("Phiên truy cập đã hết hạn! Vui lòng chờ trong giây lát, hệ thống đang tải lại trang");
                }
            },
        });
        return newToken;
    }

    /**
     * @param {string} path
     */
    static removeGuid(path) {
        const url = path;
        const filename = url.split("/").pop(); // Lấy phần tên file
        const cleanFilename = filename.replace(/.{36}(?=\.\w+$)/, "");
        return cleanFilename;
    }
    /**
     * @param {string} path
     */
    static async download(path, fileName = null) {
        const removePath = this.removeGuid(path);
        const url = path.includes("http") ? path : Path.Combine(Client.Origin, path);
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
            const blob = await response.blob();
            const objectUrl = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = objectUrl;
            a.setAttribute("download", fileName || removePath);
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(objectUrl);
        } catch (error) {
            console.error("Download failed:", error);
        }
    }
}
