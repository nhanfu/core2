import { Client } from "clients/client";
import EditableComponent from "./editableComponent.js";
import { WebSocketClient } from "clients/websocketClient";
import { KeyCodeEnum, RoleEnum } from "models/enum";
import EventType from "models/eventType";
import { PopupEditor } from "popupEditor";
import { Toast } from "toast";
import { Html } from "utils/html";
import { Utils } from "utils/utils";
import { TaskStateEnum } from "Core/Enums"


export class NotificationBL extends EditableComponent {
    static NoMoreTask = "No more task";
    static _instance = null;
    static _countNtf = null;
    static _countUser = null;
    static Notifications = [];
    static UserActive = [];
    static NotiRoot = document.getElementById("notification-list");
    static ProfileRoot = document.getElementById("profile-info1");

    constructor() {
        super(null);
        this.notifications = [];
        this.userActive = [];
        this._countNtf = "";
        this._countUser = "";
        this._task = null;
        this._countBadge = null;
        this.currentUser = null;
        window.addEventListener("task", this.processIncomMessage.bind(this));
    }

    processIncomMessage(event) {
        const obj = event.detail;
        if (!obj) {
            return;
        }

        const task = obj;
        if (!task) {
            return;
        }

        const existTask = this.notifications.find(x => x.Id === task.Id);
        if (!existTask) {
            this.notifications.push(task);
            this.toggleBadgeCount(this.notifications.length);
        }
        this.setBadgeNumber();
        const entity = Utils.getEntityById(task.EntityId);
        task.Entity = { Name: entity.Name };

    //     if (typeof(Notification) !== 'undefined' && Notification.permission === "granted") {
    //         this.showNativeNtf(task);
    //     } else if (typeof(Notification) !== 'undefined' && Notification.permission !== "denied") {
    //         Notification.requestPermission().then((permission) => {
    //             if (permission !== 'granted') {
    //                 this.showToast(task);
    //             } else {
    //                 this.showNativeNtf(task);
    //             }
    //         });
    //     } else {
    //         this.showToast(task);
    //     }
    // }

    setBadgeNumber() {
        const unreadCount = this.notifications.filter(x => x.StatusId === TaskStateEnum.UnreadStatus.toString()).length;
        this._countNtf = unreadCount > 9 ? "9+" : unreadCount.toString();
        const badge = unreadCount > 9 ? 9 : unreadCount;

        // if (typeof(cordova) !== 'undefined' &&
        //     typeof(cordova.plugins) !== 'undefined' &&
        //     typeof(cordova.plugins.notification) !== 'undefined') {
        //     cordova.plugins.notification.badge.requestPermission(function(granted) {
        //         cordova.plugins.notification.badge.set(unreadCount);
        //     });
        // }
        return badge;
    }

    // showNativeNtf(task) {
    //     if (!task) {
    //         return;
    //     }

    //     const nativeNtf = new Notification(task.Title, {
    //         body: task.Description,
    //         icon: task.Attachment,
    //         vibrate: [200, 100, 200],
    //         badge: "./favicon.ico"
    //     });

    //     nativeNtf.addEventListener('click', () => this.openNotification(task));
    //     setTimeout(() => {
    //         nativeNtf.close();
    //     }, 7000);
    // }

    // showToast(task) {
    //     // Implementation for showing toast notification
    // }

    // toggleBadgeCount(count) {
    //     // Implementation for toggling badge count
    // }

    // openNotification(task) {
    //     // Implementation for opening notification
    // }

    static getInstance() {
        if (!this._instance) {
            this._instance = new NotificationBL();
        }
        return this._instance;
    }

    render() {
        const notiRoot = document.getElementById("notification-list");
        const profileRoot = document.getElementById("profile-info1");

        if (!notiRoot || !profileRoot) {
            return;
        }

        notiRoot.innerHTML = '';
        document.querySelector("#user-active").innerHTML = '';
        this.setBadgeNumber();

        this.currentUser = Client.token;
        if (!this.currentUser) return;

        this.currentUser.Avatar = (this.currentUser.Avatar.includes("://") ? "" : Client.origin) + (this.currentUser.Avatar.trim() === "" ? "./image/chinese.jfif" : this.currentUser.Avatar);
        this.renderNotification();
        this.renderProfile(profileRoot);

        const xhr = {
            url: "/GetUserActive",
            method: "POST",
            showError: false
        };

        Client.instance.submitAsync(xhr)
            .then(users => this.userActive = users)
            .catch(console.log);
    }

    renderProfile(profileRoot) {
        if (!profileRoot) return;

        const isSave = window.localStorage.getItem("isSave");
        profileRoot.innerHTML = '';

        const html = new HtmlElementWrapper(profileRoot);
        html.addClassName("navbar-nav-link d-flex align-items-center dropdown-toggle")
            .setDataAttr("toggle", "dropdown")
            .appendSpan("text-truncate", this.currentUser.TenantCode + ": " + this.currentUser.FullName)
            .end(ElementType.a)
            .appendDiv("dropdown-menu dropdown-menu-right notClose mt-0 border-0", "border-top-left-radius: 0; border-top-right-radius: 0")
            .appendA("dropdown-item", this.viewProfile.bind(this))
            .appendI("far fa-user")
            .end("Account (" + this.currentUser.TenantCode + ": " + this.currentUser.FullName + ")")
            .end(ElementType.a)
            .appendDiv("dropdown-divider")
            .end(ElementType.div);

        const langSelect = new LangSelect(new Component(), html.getContext());
        langSelect.render();

        html.appendDiv("dropdown-divider")
            .end(ElementType.div)
            .appendA("dropdown-item", this.darkOrLightModeSwitcher.bind(this))
            .appendI("far fa-moon-cloud")
            .end("Dark/Light Mode")
            .end(ElementType.a)
            .appendA("dropdown-item", this.signOut.bind(this))
            .appendI("far fa-power-off")
            .end("Logout")
            .end(ElementType.a);
    }

    signOut(event) {
        event.preventDefault();
        const task = Client.instance.postAsync(Client.token, "/user/signOut");
        task.then(res => {
            Client.signOutEventHandler?.();
            Client.token = null;
            EditForm.notificationClient?.close();
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        });
    }

    darkOrLightModeSwitcher(event) {
        event.preventDefault();
        const htmlElement = document.documentElement;
        htmlElement.style.filter = htmlElement.style.filter.includes("(1)") ? "invert(0)" : "invert(1)";
    }

    viewProfile(event) {
        event.preventDefault();
        this.openTab({
            id: "User" + Client.token.UserId,
            featureName: "UserProfile",
            factory: () => {
                const type = Type.getType("Core.Fw.User.UserProfileBL");
                const instance = new type();
                return instance;
            }
        }).then(() => {
            this.dispose();
        });
    }

    renderNotification() {
        const notiRoot = NotificationBL.NotiRoot;
        if (!notiRoot) return;

        const html = new HtmlElementWrapper(notiRoot);
        html.addClassName("navbar-nav-link")
            .setDataAttr("toggle", "dropdown")
            .appendI("far fa-bell fa-lg")
            .end(ElementType.i);

        if (this._countNtf !== '') {
            html.appendSpan("badge badge-pill bg-warning-400 ml-auto ml-md-0", this._countNtf);
            this._countBadge = html.getContext();
        }

        html.end(ElementType.a)
            .appendDiv("dropdown-menu dropdown-menu-right dropdown-content wmin-md-300 mt-0", "border-top-left-radius: 0; border-top-right-radius: 0")
            .forEach(this.notifications, (task, index) => {
                this.renderTask(task);
            })
            .end()
            .appendA("dropdown-item dropdown-footer", this.seeMore.bind(this), "See more")
            .end(ElementType.a);
    }

    renderTask(task) {
        if (!task) {
            return;
        }

        const className = task.StatusId === TaskStateEnum.UnreadStatus.toString() ? "text-danger" : "text-muted";
        const html = new HtmlElementWrapper();

        html.appendA("dropdown-item")
            .appendDiv("media", (event) => {
                this.openNotification(task);
            })
            .appendDiv("media-body")
            .appendH3("dropdown-item-title", task.Title)
            .appendSpan("float-right text-sm " + className)
            .appendI("fas fa-star")
            .end()
            .end()
            .appendP("text-sm", task.Description)
            .end()
            .appendP("text-sm text-muted")
            .appendI("far fa-clock mr-1")
            .end(task.Deadline.toLocaleDateString("en-GB") + " " + task.Deadline.toLocaleTimeString("en-GB"))
            .end(ElementType.a);
    }

    toggleBadgeCount(count) {
        this._countBadge.style.display = count === 0 ? 'none' : 'inline-block';
    }

    seeMore(event) {
        event.preventDefault();
        const lastSeenTask = this.notifications.sort((a, b) => new Date(b.InsertedDate) - new Date(a.InsertedDate))[0];
        if (!lastSeenTask) {
            Toast.warning(NotificationBL.NoMoreTask);
            return;
        }
        Spinner.appendTo(Element, 250);
        const lastSeenDateStr = new Date(lastSeenTask.InsertedDate).toISOString();
        const sql = {
            ComId: "Task",
            Action: "SeeMore",
            MetaConn: Client.MetaConn,
            DataConn: Client.DataConn,
            Params: JSON.stringify({ Date: lastSeenDateStr })
        };
        Client.instance.userSvc(sql).then(ds => {
            const olderItems = ds.length > 0 ? ds[0].map(x => new TaskNotification(x)) : null;
            if (!olderItems || olderItems.length === 0) {
                Toast.warning(NotificationBL.NoMoreTask);
                return;
            }
            const taskList = [...this.notifications, ...olderItems];
            this.notifications = taskList;
        }).catch(err => {
            Toast.warning(err?.message ?? NotificationBL.NoMoreTask);
        });
    }

    markAllAsRead(event) {
        event.preventDefault();
        Client.instance.postAsync(null, `${TaskNotification.name}/MarkAllAsRead`).then(res => {
            this.toggleBadgeCount(this.notifications.filter(x => x.StatusId === TaskStateEnum.UnreadStatus.toString()).length);
            document.querySelectorAll(".task-unread").forEach(task => {
                task.classList.replace("task-unread", "task-read");
            });
        });
    }

    openNotification(notification) {
        this.markAsRead(notification);
    }

    removeDOM() {
        document.querySelector("#notification").innerHTML = '';
    }

    markAsRead(task) {
        task.StatusId = TaskStateEnum.Read.toString();
        Client.instance.patchAsync(task.toPatch()).then(x => {
            this.setBadgeNumber();
        });
    }

    dispose() {
        this._task.classList.add("hide");
    }
}
