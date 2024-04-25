import { BadGatewayQueue } from "../models/badGatewayQueue.js";
import { Token } from "../models/token.js";
import { Utils } from "../utils/utils.js";

export class Client {
    static EpsilonNow = new Date(Date.now() + (1 * 60 * 1000));
    static ErrorMessage = "Hệ thống đang cập nhật vui lòng chờ trong 30s!";
    static ModelNamespace;
    static entities;
    static token;
    static GuidLength = 36;
    static BaseUri = (document.head.baseUri?.content || window.location.origin).toLowerCase();
    static IsPortal = document.head.startup?.content !== "admin";
    static MetaConn = document.head.metaKey?.content || "default";
    static DataConn = document.head.dataConn?.content || "bl";
    static Tenant = document.head.tenant?.content || "System";
    static Env = document.head.env?.content || "test";
    static FileFTP = document.head.file?.content || "/user";
    static Config = document.head.config?.content || "";
    static BadGatewayRequest = new BadGatewayQueue();
    static UnAuthorizedEventHandler;
    static SignOutEventHandler;
    static get Origin() { return document.head.origin?.content || window.location.origin; }
    _nameSpace;
    _config;
    CustomPrefix = (() => {
        const prefixElement = Array.from(document.head.children).find(x => x instanceof HTMLMetaElement && x.name === "prefix");
        return prefixElement?.content;
    })();

    constructor(entityName, ns = "", config = false) {
        this._nameSpace = ns;
        this._config = config;
        if (this._nameSpace && this._nameSpace.charAt(this._nameSpace.length - 1) !== '.') {
            this._nameSpace += '.';
        }
        this.EntityName = entityName;
    }

    /** @type {Client} */
    static _instance;
    /** @type {Client} */
    static get Instance() {
      if(!Client._instance) {
        Client._instance = new Client();
      }
      return Client._instance;
    }

    /** @type {Token} */
    static Token;

    async UserSvc(vm, annonymous = false) {
        return this.SubmitAsync({
            Value: JSON.stringify(vm),
            Url: Utils.UserSvc,
            IsRawString: true,
            Method: "POST",
            AllowAnonymous: annonymous
        });
    }

    async ComQuery(vm) {
        return this.SubmitAsync({
            Value: JSON.stringify(vm),
            Url: Utils.ComQuery,
            IsRawString: true,
            Method: "POST"
        });
    }

    async GetIds(sqlVm) {
        return new Promise((resolve, reject) => {
            sqlVm.Select = "ds.Id";
            sqlVm.Count = false;
            sqlVm.SkipXQuery = true;
            this.SubmitAsync({
                Url: Utils.ComQuery,
                Value: JSON.stringify(sqlVm),
                IsRawString: true,
                Method: "POST"
            }).then(ds => {
                const res = (ds.length === 0 || ds[0].length === 0) ? [] : ds[0].map(x => x.Id);
                resolve(res);
            }).catch(reject);
        });
    }

    /**
     * 
     * @param {XHRWrapper} options 
     * @returns 
     */
    async SubmitAsync(options) {
        const isNotFormData = options.FormData === undefined;
        const tcs = new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            if (!options.Headers && options.FormData === undefined) {
                options.Headers = {
                    "Content-Type": "application/json"
                };
            }
            xhr.onreadystatechange = function () {
                if (xhr.readyState !== XMLHttpRequest.DONE) return;
                if (xhr.status >= 200 && xhr.status < 300) {
                    ProcessSuccessRequest(options, resolve, xhr);
                } else {
                    ErrorHandler(options, reject, xhr);
                }
            };
            if (options.ProgressHandler !== undefined) {
                xhr.addEventListener("progress", ProgressHandler);
            }
            xhr.open(options.Method, options.FinalUrl ?? options.Url, true);
            if (!options.AllowAnonymous) {
                xhr.setRequestHeader("Authorization", "Bearer " + Client.Token?.AccessToken);
            }
            for (const [key, value] of Object.entries(options.Headers)) {
                xhr.setRequestHeader(key, value);
            }
            if (isNotFormData) {
                xhr.send(options.JsonData);
            } else {
                xhr.send(options.FormData);
            }
        });
        return tcs;
    }

    async FirstOrDefaultAsync(filter = null, clearCache = false, addTenant = false) {
        const headers = ClearCacheHeader(clearCache);
        const res = await this.SubmitAsync({
            Value: null,
            AddTenant: addTenant,
            Url: filter,
            Headers: headers,
            Method: "GET"
        });
        return res?.Value?.[0];
    }

    async GetByIdAsync(table, connKey, ...ids) {
        if (!table || ids.length === 0) {
            return null;
        }
        const tcs = new Promise((resolve) => {
            const vm = {
                Params: JSON.stringify({ Table: table, Ids: ids }),
                ComId: "Entity",
                Action: "ById",
                MetaConn: Client.MetaConn,
                DataConn: connKey || Client.DataConn
            };
            this.UserSvc(vm).then(ds => {
                resolve(ds.length > 0 ? ds[0] : null);
            });
        });
        return tcs;
    }

    async PostAsync(value, subUrl = "", annonymous = false) {
        return this.SubmitAsync({
            Value: value,
            Url: subUrl,
            Method: "POST",
            AllowAnonymous: annonymous,
        });
    }

    async PatchAsync(value, errHandler = null, annonymous = false) {
        return this.SubmitAsync({
            Value: JSON.stringify(value),
            IsRawString: true,
            Url: Utils.PatchSvc,
            Headers: { "Content-type": "application/json" },
            Method: "PATCH",
            AllowAnonymous: annonymous,
            ErrorHandler: errHandler
        });
    }

    async PostFilesAsync(file, url = "", progressHandler = null) {
        const formData = new FormData();
        formData.append("file", file);
        return this.SubmitAsync({
            FormData: formData,
            File: file,
            ProgressHandler: progressHandler,
            Method: "POST",
            Url: url
        });
    }

    async SendMail(email) {
        return this.SubmitAsync({
            Value: email,
            Method: "POST",
            Url: "Email"
        });
    }

    async DeactivateAsync(ids, table, connKey) {
        const vm = {
            Ids: ids,
            Params: table,
            MetaConn: Client.MetaConn,
            DataConn: connKey || Client.DataConn
        };
        return this.SubmitAsync({
            Url: Utils.DeactivateSvc,
            Value: JSON.stringify(vm),
            Method: "DELETE",
            IsRawString: true,
            Headers: {
                "Content-type": "application/json"
            }
        });
    }

    async HardDeleteAsync(ids, table, dataConn, connKey = null) {
        const vm = {
            Table: table,
            DeletedIds: ids,
            DataConn: dataConn || Client.DataConn,
            MetaConn: connKey || Client.MetaConn
        };
        return this.SubmitAsync({
            Url: Utils.DeleteSvc,
            Value: JSON.stringify(vm),
            Method: "POST",
            IsRawString: true,
            Headers: {
                "Content-type": "application/json"
            }
        });
    }

    static async LoadScript(src) {
        const scriptExists = Array.from(document.body.children).some(x => x instanceof HTMLScriptElement && x.src.split("/").pop() === src.split("/").pop());
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

    static async LoadLink(src) {
        const linkExists = Array.from(document.head.children).some(x => x instanceof HTMLLinkElement && x.href.replace(document.location.origin, '') === src);
        if (linkExists) return true;
        const tcs = new Promise((resolve) => {
            const link = document.createElement("link");
            link.href = src;
            link.addEventListener("load", () => {
                resolve(true);
            });
            link.onerror = () => {
                resolve(true);
                return false;
            };
            document.head.appendChild(link);
        });
        return tcs;
    }

    static async RefreshToken(success = null) {
        const oldToken = Client.Token;
        if (!oldToken || oldToken.RefreshTokenExp <= Client.EpsilonNow) return null;
        if (oldToken.AccessTokenExp > Client.EpsilonNow) return oldToken;
        if (oldToken.AccessTokenExp <= Client.EpsilonNow && oldToken.RefreshTokenExp > Client.EpsilonNow) {
            const newToken = await Client.GetToken(oldToken);
            if (newToken) {
                Client.Token = newToken;
                success?.(newToken);
            }
            return newToken;
        }
    }

    static async GetToken(oldToken) {
        const newToken = await Instance.SubmitAsync({
            NoQueue: true,
            Url: `/user/Refresh?t=${Token.TenantCode || Client.Tenant}`,
            Method: "POST",
            Value: { AccessToken: oldToken.AccessToken, RefreshToken: oldToken.RefreshToken },
            AllowAnonymous: true,
            ErrorHandler: (xhr) => {
                if (xhr.status === 400) {
                    Client.Token = null;
                    Toast.Warning("Phiên truy cập đã hết hạn! Vui lòng chờ trong giây lát, hệ thống đang tải lại trang");
                    window.location.reload();
                }
            },
        });
        return newToken;
    }

    static Download(path) {
        const removePath = RemoveGuid(path);
        const a = document.createElement("a");
        a.href = path.includes("http") ? path : PathIO.Combine(Origin, path);
        a.target = "_blank";
        a.setAttribute("download", removePath);
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }
}

function ProcessSuccessRequest(options, resolve, xhr) {
    if (options.Retry) {
        resolve(true);
        return;
    }
    if (!xhr.responseText) {
        resolve(null);
        return;
    }
    if (options.CustomParser) {
        resolve(options.CustomParser(xhr.response));
        return;
    }
    const type = typeof options.Value;
    if (type === "number") {
        resolve(Number(xhr.responseText));
    } else if (type === "string") {
        resolve(xhr.responseText);
    } else if (type === "object") {
        const parsed = JSON.parse(xhr.responseText);
        resolve(parsed);
    } else {
        try {
            const parsed = JSON.parse(xhr.responseText);
            resolve(parsed);
        } catch {
            resolve(xhr.responseText);
        }
    }
}

function ErrorHandler(options, reject, xhr) {
    if (options.Retry) {
        reject(false);
        return;
    }
    let exp;
    try {
        exp = JSON.parse(xhr.responseText);
        exp.StatusCode = xhr.status;
    } catch {
        exp = { Message: "Đã có lỗi xảy ra trong quá trình xử lý", StackTrace: xhr.responseText };
    }
    if (options.ErrorHandler) {
        options.ErrorHandler(xhr);
        reject(new Error(exp.Message));
        return;
    }
    if (xhr.status >= 400 && xhr.status < 500) {
        if (exp && exp.Message && options.ShowError) {
            console.warn(exp.Message);
        }
    } else if (xhr.status === 500 || xhr.status === 404) {
        console.warn(exp);
    } else if (xhr.status === 401) {
        Client.UnAuthorizedEventHandler?.(options);
    } else if (xhr.status >= 502 || xhr.status === 504 || xhr.status === 503) {
        if (options.ShowError) {
            console.warn("Lỗi kết nối tới máy chủ, vui lòng chờ trong giây lát...");
        }
        if (!options.Retry) {
            Client.BadGatewayRequest.Enqueue(options);
        }
    } else {
        if (xhr.responseText) {
            console.warn(xhr.responseText);
        }
    }
    reject(new Error(exp.Message));
}

function ClearCacheHeader(clearCache) {
    const headers = {};
    if (clearCache) {
        headers["Pragma"] = "no-cache";
        headers["Expires"] = "0";
        headers["Last-Modified"] = new Date().toString();
        headers["If-Modified-Since"] = new Date().toString();
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate, post-check=0, pre-check=0";
    }
    return headers;
}
