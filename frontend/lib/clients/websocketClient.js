import { Client } from "./client.js";
import ReconnectingWebSocket from "reconnecting-websocket";
import EntityAction from "../models/entityAction.js";

export class WebSocketClient {
    /**
     * @type {EntityAction[]}
     */
    EntityActionList;

    /**
     * @param {string} url
     */
    constructor(url) {
        this.EntityActionList = [];

        const token = Client.Token ? Client.Token.AccessToken : null;
        if (!token) return;

        const ws = Client.api.includes("https") ? "wss" : "ws";
        const wsUri = `${ws}://${url}?access_token=${token}`;

        const options = {
            maxRetries: 10,
            reconnectInterval: 3000,
        };

        if (typeof ReconnectingWebSocket !== 'undefined') {
            this.socket = new ReconnectingWebSocket(wsUri, [], options);
        } else {
            this.socket = new WebSocket(wsUri);
        }

        this.socket.onmessage = this.HandleMessage.bind(this);
        this.socket.onopen = this.HandleOpen.bind(this);
        this.socket.onclose = this.HandleClose.bind(this);
        this.socket.onerror = this.HandleError.bind(this);
        this.socket.binaryType = "arraybuffer";

        // Lắng nghe thay đổi trạng thái mạng
        window.addEventListener('online', this.HandleOnline.bind(this));
        window.addEventListener('offline', this.HandleOffline.bind(this));

        // Kiểm tra trạng thái ngay lúc khởi tạo
        if (!navigator.onLine) this.HandleOffline();
    }

    /**
     * @param {{ data: { toString: () => any; }; }} event
     */
    HandleMessage(event) {
        const responseStr = event.data.toString();
        try {
            const objRs = JSON.parse(responseStr);
            if (objRs.QueueName == "ShipmentTask") {
                this.EntityActionList.filter(x => x.QueueName.startsWith(objRs.QueueName)).forEach(x => {
                    x.action(objRs);
                });
            }
            else {
                this.EntityActionList.filter(x => x.QueueName == objRs.QueueName).forEach(x => {
                    x.action(objRs);
                });
            }
            if (objRs.Action == "ConversationDetail") {
                this.EntityActionList.filter(x => x.QueueName == "ChatBadge").forEach(x => {
                    x.action(objRs);
                });
            }
            window.dispatchEvent(new CustomEvent(objRs.QueueName, { detail: objRs }));
        } catch (error) {
            this.deviceKey = responseStr;
        }
    }

    HandleOpen() {
        console.log("✅ WebSocket connected");
        this.HideOfflineBanner();
    }

    HandleClose() {
        console.warn("🔌 WebSocket disconnected");
        this.ShowOfflineBanner();
    }

    HandleError(error) {
        console.error("⚠️ WebSocket error:", error);
        this.ShowOfflineBanner();
    }

    HandleOnline() {
        console.log("📶 Network connected");
        this.HideOfflineBanner();
    }

    HandleOffline() {
        console.warn("🚫 Network offline");
        this.ShowOfflineBanner();
    }

    ShowOfflineBanner() {
        const banner = document.getElementById('offline-banner');
        if (banner) banner.style.display = 'block';
    }

    HideOfflineBanner() {
        const banner = document.getElementById('offline-banner');
        if (banner) banner.style.display = 'none';
    }

    AddListener(queueName, entityAction) {
        const index = this.EntityActionList.findIndex(item => item.QueueName === queueName);
        if (index !== -1) {
            this.EntityActionList[index].action = entityAction;
        } else {
            this.EntityActionList.push({
                QueueName: queueName,
                action: entityAction
            });
        }
    }

    RemoveListener(queueName) {
        const index = this.EntityActionList.findIndex(item => item.QueueName === queueName);
        if (index !== -1) {
            this.EntityActionList.splice(index, 1);
        }
    }

    /**
     * @param {string | ArrayBufferLike | Blob | ArrayBufferView} message
     */
    Send(message) {
        this.socket.send(message);
    }

    Close() {
        this.socket?.close();
    }
}
