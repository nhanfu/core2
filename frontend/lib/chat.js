import { EditableComponent } from "./editableComponent.js";
import EventType from "./models/eventType.js";
import { PatchVM } from "./models/patch.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { html } from "./utils/html";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import { keyCodeEnum } from "./models/index.js";
import { Picker, Data as data } from 'emoji-mart';
import { ComponentExt } from "./utils/componentExt.js";
import { Spinner } from "./spinner.js";
import { ComponentFactory } from "./utils/componentFactory.js";

export class Chat extends EditableComponent {
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.meta = ui;
    }

    /**
     * @type {HTMLElement}
     */
    htmlContentChat;
    /**
     * @type {HTMLElement}
     */
    htmlEmoji;
    /**
     * @type {hTMLInputElement}
     */
    userParentElement;
    /**
     * @type {hTMLInputElement}
     */
    htmlIputChat;
    /**
     * @type {HTMLElement}
     */
    htmlHeaderChat;
    /**
     * @type {[]}
     */
    chatData;
    /**
    * @type {[]}
    */
    conversation;

    render() {
        this.title = Utils.formatEntity(this.meta.formatData, this.entity);
        html.take(this.parentElement).div.className("chat-container");
        this.element = html.context;
        this.loadData();
    }

    handleMessage(data) {
        var evt = "updateViewEntity" + this.entity.id.replaceAll("-", "");
        if (data.queueName != evt) {
            this.updateData().then(() => {
                this.renderBodyDiscussions();
            });
            window.setTimeout(() => {
                this.updateBadge();
            }, 500);
            return;
        }
        const message = data.Message;
        const existingMessage = this.chatData.find(msg => msg.id === message.id);
        if (existingMessage) {
            existingMessage.Message = message.Message;
            const existingElement = document.querySelector(`[data-id="${message.id}"] p`);
            if (existingElement) {
                existingElement.innerHTML = message.Message;
            }
        } else {
            const lastMessage = this.chatData.length ? this.chatData[this.chatData.length - 1] : null;
            if (lastMessage && lastMessage.fromId === message.fromId) {
                const prevEl = document.querySelector(`[data-id="${lastMessage.id}"]`);
                if (prevEl) {
                    const photoEl = prevEl.querySelector(".photo");
                    if (photoEl) {
                        photoEl.classList.add("spacer");
                        photoEl.innerHTML = "";
                    }
                    const timeEl = prevEl.nextElementSibling;
                    if (timeEl && timeEl.classList.contains("time")) timeEl.remove();
                    if (timeEl && timeEl.classList.contains("response-time")) timeEl.remove();

                    prevEl.classList.add("message-grouped");
                }
            }

            this.chatData.push(message);
            this.addMessageToDOM(message, true);
        }
        window.setTimeout(() => {
            if (this.htmlContentChat && this.htmlContentChat.parentElement) {
                this.htmlContentChat.parentElement.scrollTop = this.htmlContentChat.clientHeight;
            }
        }, 100);
        this.htmlIputChat.value = "";
        this.lastUserId = message.fromId;
        this.updateData().then(() => {
            this.renderBodyDiscussions();
        });
        window.setTimeout(() => {
            this.updateBadge();
        }, 500);
    }

    /**
     * add message vào dOM.
     * isLastInGroup: nếu true thì render avatar + time/name, ngược lại render spacer thay avatar (để giữ căn lề) và ẩn time.
     */
    addMessageToDOM(item, isLastInGroup = true) {
        html.take(this.htmlContentChat);
        const isImage = Utils.isImage(item.Message);

        if (this.Token.userId == item.fromId) {
            html.instance.div.dataAttr("id", item.id).className("message text-only").div.className("response")
                .p.className("text2");
            if (isImage) {
                html.instance.event(EventType.click, () => this.showPreview(item));
            }
            html.instance.innerHTML(item.Message).end.render();
            if (item.Message != "tin nhắn đã được thu hồi") {
                html.instance.i.className("icon fa fa-trash clickable").event(EventType.click, async () => await this.deleteMessage(item)).end.render();
            }
            html.instance.end.end.render();

            if (isLastInGroup) {
                html.instance.p.className("response-time time").i.text(this.dayjs(item.insertedDate).format("hH:mm DD/MM/YYYY")).end.end.render();
            }
        }
        else {
            html.instance.div.dataAttr("id", item.id).className("message");

            if (isLastInGroup) {
                html.instance.div.className("photo").style(`background-image: url('${item.avatar}');`)
                    .div.className("online").end.end;
            } else {
                html.instance.div.className("photo spacer").end;
            }

            html.instance.p.className("text");
            html.instance.innerHTML(item.Message);
            if (isImage) {
                html.instance.event(EventType.click, () => this.showPreview(item));
            }
            html.instance.end.end.render();

            if (isLastInGroup) {
                html.instance.p.className("time").i.text(item.fromName + ' - ' + this.dayjs(item.insertedDate).format("hH:mm DD/MM/YYYY")).end.end.render();
            }
        }
    }

    loadData() {
        this.runQuerys().then(data => {
            this.chatData = data[0];
            this.conversation = data[1];
            this.users = data[2];
            this.renderDiscussions();
            this.renderChat();
            if (!this.entity || !this.entityId || this.entityId.startsWith("-")) {
                this.handlerClickBot();
            }
        })
    }

    async updateData() {
        var data = await this.runQuerys();
        this.chatData = data[0];
        this.conversation = data[1];
        this.users = data[2];
    }

    renderMenu() {
        html.take(this.element).nav.className("chat-menu").ul.className("chat-items")
            .li.className("chat-item").i.className("fal fa-home").end.end
            .li.className("chat-item").i.className("fal fa-user").end.end
            .li.className("chat-item").i.className("fal fa-pencil").end.end
            .li.className("chat-item").i.className("fal fa-comment").end.end
            .li.className("chat-item").i.className("fal fa-file").end.end
            .li.className("chat-item").i.className("fal fa-cog").end.endOf(".chat-menu");
    }

    /**
     * @type {HTMLElement}
     */
    bodyDiscussions;

    renderDiscussions() {
        html.take(this.element).section.className("discussions").div.className("header-discussions").div.tabIndex(-1).event(EventType.click, (evt) => this.handlerClickBot(evt)).className("discussion " + (("-1" == this.entity.id) ? "message-active" : "") + "")
            .div.className("photo").style("background-image: url('https://forwardx.vn/wp-content/uploads/2025/03/cropped-icon-logo-180x180.png');").end
            .div.className("desc-contact")
            .span.className("name").iText("forwardx").end
            .span.className("description").text("bot assistant").end
            .p.className("message").innerHTML('....').end
            .p.className("message").innerHTML('').end.end
            .end.render();
        html.instance.div.render();
        this.bodyDiscussions = html.context;
        this.renderBodyDiscussions();
        html.instance.endOf(".discussions");
    }

    handlerClickBot() {
        document.querySelector(".footer-chat").classList.add("d-none");
        this.optionsElement.classList.add("d-none");
        html.take(this.htmlContentChat).clear();
        html.take(this.titleText).clear().text("forwardx");
        html.take(this.featureText).clear().iText("bot assistant");
        var rsSaleFunction = localStorage.getItem("salesFunction2") ? JSON.parse(localStorage.getItem("salesFunction2")) : [];
        const aiEntry = rsSaleFunction.find((x) => x.code == "aI_ID");
        const chatbotId = aiEntry && aiEntry.value;
        const el = document.createElement("zapier-interfaces-chatbot-embed");
        el.setAttribute("chatbot-id", chatbotId.toString());
        el.style.height = "calc(100vh - 14rem)";
        this.htmlContentChat.appendChild(el);
    }

    lastFromId;
    lastUserId;
    /**
     * @type {string}
     */
    title = null;

    renderBodyChat() {
        html.take(this.htmlContentChat).clear();
        this.lastFromId = this.chatData.find(x => x.fromId != this.Token.userId);
        this.lastUserId = this.chatData.find(x => x.fromId != this.Token.userId);

        // render theo nhóm: nếu message tiếp theo cùng fromId -> current không phải cuối nhóm
        for (let i = 0; i < (this.chatData || []).length; i++) {
            const item = this.chatData[i];
            const next = (i + 1 < this.chatData.length) ? this.chatData[i + 1] : null;
            const isLastInGroup = !(next && next.fromId === item.fromId);
            this.addMessageToDOM(item, isLastInGroup);
        }

        window.setTimeout(() => {
            if (this.htmlContentChat && this.htmlContentChat.parentElement) {
                this.htmlContentChat.parentElement.scrollTop = this.htmlContentChat.clientHeight;
            }
        }, 100);
    }

    /**
     * @type {HTMLElement}
     */
    titleText;
    /**
     * @type {HTMLElement}
     */
    featureText;
    /**
     * @type {HTMLElement}
     */
    containerPicker;
    /**
     * @type {HTMLElement}
     */
    userElement;
    /**
     * @type {HTMLElement}
     */
    optionsElement;
    /**
     * @type {HTMLElement}
     */
    darkOverlay;
    /**
    * @type {Picker}
    */
    Picker;

    renderChat() {
        html.take(this.element).section.className("chat")
            .div.className("header-chat");
        this.htmlHeaderChat = html.context;
        html.instance.div.className("name2").style("width: 100%; display: flex ; align-items: center; gap: 10px;").span.iText(this.entity.Label)
        this.featureText = html.context;
        html.instance.end.span.text(" : ").end.a.style("color:#fff").className("mr-1").event(EventType.click, this.openPopup.bind(this)).text(this.entity.formatChat ? this.entity.formatChat.replaceAll("<br>", "") : "");
        this.titleText = html.context;
        html.end.div.className("d-flex align-items-center");
        this.optionsElement = html.context;
        html.span.className("d-flex").render();
        this.userElement = html.context;
        html.instance.end.i.event(EventType.click, this.addUser.bind(this)).className("fas fa-user-plus").end.render();
        html.instance.end.end.end.div.className("messages-chat").div.render();
        this.htmlContentChat = html.context;
        this.renderBodyChat();
        html.instance.end.end.div.className("footer-chat")
            .input.type("file").className("attach-file").style("display: none;").event(EventType.change, this.attachFile.bind(this)).end
            .i.className("icon fa fa-paperclip clickable").event(EventType.click, () => this.element.querySelector(".attach-file").click()).end
            .i.className("icon fa fa-smile clickable").event(EventType.click, this.showEmojiPicker.bind(this)).end
            .textArea.className("write-message").placeHolder("type your message here");
        this.htmlIputChat = html.context;
        this.htmlIputChat.addEventListener("keydown", (e) => {
            if (e.keyCodeEnum() == keyCodeEnum.enter && !e.shiftKey) {
                e.preventDefault();
                this.sendChat();
            }
        });
        this.htmlIputChat.addEventListener("paste", this.handlePaste.bind(this));
        this.htmlIputChat.addEventListener("input", function () {
            this.style.height = "36px";
            this.style.height = this.scrollHeight + "px";
        });
        html.instance.end.i.className("icon send fal fa-arrow-circle-right clickable").event(EventType.click, this.sendChat.bind(this)).end.render();
        this.renderUsers();
    }

    renderUsers() {
        html.take(this.userElement).clear();
        html.take(this.userElement).forEach(this.users || [], (item) => {
            html.instance.div.className("photo").style("background-image: url('" + item.avatar + "');").end.render();
        })
    }

    openPopup() {
        if (this.entity && this.entity.featureName) {
            this.tabEditor.openPopup(this.entity.featureName2 || this.entity.featureName, { id: this.entity.recordId }, true);
        }
    }

    addUser() {
        if (this.entity && this.entity.featureName) {
            var com = this.tabEditor.meta.gridPolicies.find(x => x.fieldName == "receiverIds");
            this.editForm.openConfig("invite user to the conversation", async () => {
                if (!this.editForm.dirty) {
                    return;
                }
                var com = this.gET("receiverIds");
                if (com !== null) {
                    com.dispose();
                }
                let dirtyPatchDetail = [
                    {
                        Label: "id",
                        field: "id",
                        oldVal: null,
                        value: this.entity.id,
                    },
                    {
                        Label: "isSend",
                        field: "isSend",
                        oldVal: null,
                        value: 0,
                    },
                    {
                        Label: "featureName",
                        field: "featureName",
                        oldVal: null,
                        value: this.entity.Label,
                    },
                    {
                        Label: "featureName2",
                        field: "featureName2",
                        oldVal: null,
                        value: this.entity.featureName2,
                    },
                    {
                        Label: "featureName3",
                        field: "featureName3",
                        oldVal: null,
                        value: this.entity.featureName3,
                    },
                    {
                        Label: "formatChat",
                        field: "formatChat",
                        oldVal: null,
                        value: this.entity.formatChat,
                    },
                    {
                        Label: "voucherTypeId",
                        field: "voucherTypeId",
                        oldVal: null,
                        value: 20,
                    },
                    {
                        Label: "receiverIds",
                        field: "receiverIds",
                        oldVal: null,
                        value: this.editForm.entity.receiverIds,
                    }
                ]
                let patchModelDetail = {
                    changes: dirtyPatchDetail,
                    table: "conversation",
                    notMessage: true
                };
                await Client.instance.patchAsync(patchModelDetail);
                this.dirty = false;
                this.updateView(true);
            }, () => { }, true, [com], null, null);
        }
    }

    showPreview(item) {
        var rotate = 0;
        var img = null;
        html.take(document.body).div.className("dark-overlay zoom");
        this.darkOverlay = html.context;
        html.instance.innerHTML(item.Message);
        img = html.context.querySelector("img");
        html.instance.span.className("close").event(EventType.click, () => {
            this.darkOverlay.remove();
        }).i.className("fa fa-times").end.end
            .div.className("toolbar")
            .span.className("icon fa fa-undo ro-left").event(EventType.click, () => {
                rotate -= 90;
                img.style.transform = `rotate(${rotate}deg)`;
            }).end
            .span.className("icon fa fa-cloud-download-alt").event(EventType.click, () => this.downloadFile(item)).end
            .span.className("icon fa fa-redo ro-right").event(EventType.click, () => {
                rotate += 90;
                img.style.transform = `rotate(${rotate}deg)`;
            }).end.end
    }

    downloadFile(item) {
        var file = document.querySelector(".dark-overlay img");
        Client.download(file.getAttribute("src"));
    }

    showEmojiPicker() {
        const pickerContainer = document.createElement('div');
        pickerContainer.style.position = 'absolute';
        pickerContainer.style.bottom = '50px';
        pickerContainer.style.left = '10px';
        pickerContainer.style.zIndex = '1000';

        this.Picker = new Picker({
            data: data,
            onEmojiSelect: emoji => {
                this.htmlIputChat.value += emoji.native;
            },
        });

        pickerContainer.appendChild(this.Picker);
        document.body.appendChild(pickerContainer);
        this.containerPicker = pickerContainer;
        this.calcPosition();
        document.addEventListener('click', this.hideEmojiPicker.bind(this), true);
    }

    hideEmojiPicker(event) {
        if (this.containerPicker && !this.containerPicker.contains(event.target) && !this.htmlIputChat.contains(event.target)) {
            this.containerPicker.remove();
            document.removeEventListener('click', this.hideEmojiPicker.bind(this), true);
        }
    }

    calcPosition() {
        ComponentExt.alterPosition(this.containerPicker, this.htmlIputChat);
    }

    attachFile(event) {
        const files = event.target.files;
        this.uploadAllFiles(files).then(() => {
        }).catch(error => {
            console.error("failed to upload files:", error);
        });
    }

    removeGuid(path) {
        let fileName = path.replace(/^.*[\\\/]/, '');
        let extension = '';
        let nameWithoutExt = fileName;
        const lastDotIndex = fileName.lastIndexOf('.');
        if (lastDotIndex !== -1) {
            extension = fileName.substring(lastDotIndex + 1);
            nameWithoutExt = fileName.substring(0, lastDotIndex);
        }
        const uuidRegex = /[0-9a-fA-f]{8}-[0-9a-fA-f]{4}-[0-9a-fA-f]{4}-[0-9a-fA-f]{4}-[0-9a-fA-f]{12}/g;
        const cleanedName = nameWithoutExt.replace(uuidRegex, '').replace(/\s+/g, ' ').trim();
        return `${cleanedName}.${extension}`;
    }

    async uploadAllFiles(filesSelected) {
        Spinner.appendTo();
        const files = array.from(filesSelected).map(this.uploadFile.bind(this));
        let allPath = await promise.all(files);
        var thumbText = allPath[0];
        const isImage = Utils.isImage(thumbText);
        const fileName = this.removeGuid(thumbText);
        var format = `<a href="${thumbText}" target="_blank" style="color: red; font-weight: 700;"><i class="fal fa-file-alt mr-1"></i>${fileName}</a>`;
        if (isImage) {
            format = `<img src="${thumbText}">`;
        }
        var patch = new PatchVM();
        patch.table = "conversationDetail";
        patch.changes = [{
            field: "id",
            value: Uuid7.newGuid(),
        },
        {
            field: "fromId",
            value: this.Token.userId,
        },
        {
            field: "fromName",
            value: this.Token.nickName,
        },
        {
            field: "Message",
            value: format,
        },
        {
            field: "recordId",
            value: this.entity.recordId,
        },
        {
            field: "formatChat",
            value: this.title,
        },
        {
            field: "icon",
            value: this.entity.icon,
        },
        {
            field: "avatar",
            value: this.Token.avatar || "/assets/images/avatar1.png",
        },
        {
            field: "conversationId",
            value: this.entity.id,
        },
        {
            field: "entityId",
            value: this.entity.entityId,
        }];
        await Client.instance.patchAsync(patch);
        Spinner.hide();
    }

    async updateBadge() {
        const response = await Client.instance.postAsync(null, "/api/chatBadge");
        document.querySelector("#badgeMessage").textContent = response > 0 ? response.toString() : "";
    };

    /**
     * @param {file} file
     */
    async uploadFile(file) {
        try {
            const path = await Client.instance.postFilesAsync(file, Utils.fileSvc);
            return path;
        } catch (error) {
            console.error("error posting file:", error);
            throw error;
        }
    }

    async deleteMessage(item) {
        Spinner.appendTo();
        var patch = new PatchVM();
        patch.table = "conversationDetail";
        patch.changes = [{
            field: "id",
            value: item.id,
        },
        {
            field: "Message",
            value: "tin nhắn đã được thu hồi",
        }];
        await Client.instance.patchAsync(patch);
        Spinner.hide();
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.updateData().then(() => {
            this.title = this.entity.formatChat ? this.entity.formatChat.replaceAll("<br>", "") : "";
            this.renderBodyDiscussions();
            if (force) {
                this.renderBodyChat();
                html.take(this.titleText).clear().text(this.entity.formatChat ? this.entity.formatChat.replaceAll("<br>", "") : "");
                html.take(this.featureText).clear().iText(this.entity.Label);
                this.renderUsers();
            }
        });
    }

    renderBodyDiscussions() {
        html.take(this.bodyDiscussions).clear();
        this.conversation.forEach(item => {
            html.instance.div.tabIndex(-1).event(EventType.click, (evt) => this.handlerClick(evt, item)).className("discussion " + ((item.id == this.entity.id) ? "message-active" : "") + ((!item.read) ? "text-unread" : ""))
                .div.className("photo").style("background-image: url('" + item.icon + "');").end
                .div.className("desc-contact")
                .span.className("name").iText(item.Label).end
                .span.className("description").text(item.formatChat ? item.formatChat.replaceAll("<br>", "") : "").end
                .p.className("message").innerHTML(item.Message || '').end
                .p.className("message").innerHTML(item.time).end.end
                .end.render();
        });
    }

    /**
    * @param {event} e 
    * @param {{}} item
    */
    handlerClick(e, item) {
        document.querySelector(".footer-chat").classList.remove("d-none");
        this.optionsElement.classList.remove("d-none");
        if (item.conversationReadId) {
            var patch = new PatchVM();
            patch.table = "conversationRead";
            patch.changes = [{
                field: "id",
                value: item.conversationReadId,
            },
            {
                field: "read",
                value: "1",
            }];
            Client.instance.patchAsync(patch).then(async () => {
                this.element.querySelectorAll(".discussion").forEach(x => x.classList.remove("message-active"));
                e.target.closest(".discussion").classList.add("message-active");
                this.entity = item;
                this.editForm.entity = item;
                this.updateView(true);
                await this.updateBadge();
            });
        }
        else {
            this.element.querySelectorAll(".discussion").forEach(x => x.classList.remove("message-active"));
            e.target.closest(".discussion").classList.add("message-active");
            this.entity = item;
            this.editForm.entity = item;
            this.updateView(true);
            this.updateBadge();
        }
    }

    sendChat() {
        this.title = Utils.formatEntity(this.meta.formatData, this.entity);
        var text = this.htmlIputChat.value;
        if (Utils.isNullOrWhiteSpace(text)) {
            return;
        }
        var patch = new PatchVM();
        patch.table = "conversationDetail";
        patch.changes = [{
            field: "id",
            value: Uuid7.newGuid(),
        },
        {
            field: "fromId",
            value: this.Token.userId,
        },
        {
            field: "fromName",
            value: this.Token.nickName,
        },
        {
            field: "Message",
            value: text,
        },
        {
            field: "recordId",
            value: this.entity.recordId,
        },
        {
            field: "formatChat",
            value: this.title,
        },
        {
            field: "icon",
            value: this.entity.icon,
        },
        {
            field: "avatar",
            value: this.Token.avatar || "/assets/images/avatar1.png",
        },
        {
            field: "conversationId",
            value: this.entity.id,
        },
        {
            field: "entityId",
            value: this.entity.entityId,
        }];
        Client.instance.patchAsync(patch).then();
        window.setTimeout(() => {
            this.updateBadge();
        }, 500);
    }

    async handlePaste(event) {
        const items = (event.clipboardData || window.clipboardData).items;
        if (items[0].type.indexOf("image") !== -1) {
            const file = items[0].getAsFile();
            this.uploadAllFiles([file]).then(() => {
            }).catch(error => {
                console.error("failed to upload files:", error);
            });
        }
    }
}
