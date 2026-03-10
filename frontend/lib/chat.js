import { EditableComponent } from "./editableComponent.js";
import EventType from "./models/eventType.js";
import { PatchVM } from "./models/patch.js";
import { Uuid7 } from "./structs/uuidv7.js";
import { Html } from "./utils/html";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import { KeyCodeEnum } from "./models/index.js";
import { Picker, Data } from 'emoji-mart';
import { ComponentExt } from "./utils/componentExt.js";
import { Spinner } from "./spinner.js";
import { ComponentFactory } from "./utils/componentFactory.js";

export class Chat extends EditableComponent {
    constructor(ui, ele = null) {
        super(ui);
        /** @type {Component} */
        this.Meta = ui;
    }

    /**
     * @type {HTMLElement}
     */
    HtmlContentChat;
    /**
     * @type {HTMLElement}
     */
    HtmlEmoji;
    /**
     * @type {HTMLInputElement}
     */
    UserParentElement;
    /**
     * @type {HTMLInputElement}
     */
    HtmlIputChat;
    /**
     * @type {HTMLElement}
     */
    HtmlHeaderChat;
    /**
     * @type {[]}
     */
    ChatData;
    /**
    * @type {[]}
    */
    Conversation;

    Render() {
        this.Title = Utils.FormatEntity(this.Meta.FormatData, this.Entity);
        Html.take(this.ParentElement).div.className("chat-container");
        this.Element = Html.Context;
        this.LoadData();
    }

    HandleMessage(data) {
        var evt = "UpdateViewEntity" + this.Entity.Id.replaceAll("-", "");
        if (data.QueueName != evt) {
            this.UpdateData().then(() => {
                this.RenderBodyDiscussions();
            });
            window.setTimeout(() => {
                this.updateBadge();
            }, 500);
            return;
        }
        const message = data.Message;
        const existingMessage = this.ChatData.find(msg => msg.Id === message.Id);
        if (existingMessage) {
            existingMessage.Message = message.Message;
            const existingElement = document.querySelector(`[data-id="${message.Id}"] p`);
            if (existingElement) {
                existingElement.innerHTML = message.Message;
            }
        } else {
            const lastMessage = this.ChatData.length ? this.ChatData[this.ChatData.length - 1] : null;
            if (lastMessage && lastMessage.FromId === message.FromId) {
                const prevEl = document.querySelector(`[data-id="${lastMessage.Id}"]`);
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

            this.ChatData.push(message);
            this.AddMessageToDOM(message, true);
        }
        window.setTimeout(() => {
            if (this.HtmlContentChat && this.HtmlContentChat.parentElement) {
                this.HtmlContentChat.parentElement.scrollTop = this.HtmlContentChat.clientHeight;
            }
        }, 100);
        this.HtmlIputChat.value = "";
        this.LastUserId = message.FromId;
        this.UpdateData().then(() => {
            this.RenderBodyDiscussions();
        });
        window.setTimeout(() => {
            this.updateBadge();
        }, 500);
    }

    /**
     * Add message vào DOM.
     * isLastInGroup: nếu true thì render avatar + time/name, ngược lại render spacer thay avatar (để giữ căn lề) và ẩn time.
     */
    AddMessageToDOM(item, isLastInGroup = true) {
        Html.take(this.HtmlContentChat);
        const isImage = Utils.IsImage(item.Message);

        if (this.Token.UserId == item.FromId) {
            Html.Instance.div.dataAttr("id", item.Id).className("message text-only").div.className("response")
                .p.className("text2");
            if (isImage) {
                Html.Instance.event(EventType.Click, () => this.ShowPreview(item));
            }
            Html.Instance.innerHTML(item.Message).end.render();
            if (item.Message != "Tin nhắn đã được thu hồi") {
                Html.Instance.i.className("icon fa fa-trash clickable").event(EventType.Click, async () => await this.DeleteMessage(item)).end.render();
            }
            Html.Instance.end.end.render();

            if (isLastInGroup) {
                Html.Instance.p.className("response-time time").i.text(this.dayjs(item.InsertedDate).format("HH:mm DD/MM/YYYY")).end.end.render();
            }
        }
        else {
            Html.Instance.div.dataAttr("id", item.Id).className("message");

            if (isLastInGroup) {
                Html.Instance.div.className("photo").style(`background-image: url('${item.Avatar}');`)
                    .div.className("online").end.end;
            } else {
                Html.Instance.div.className("photo spacer").end;
            }

            Html.Instance.p.className("text");
            Html.Instance.innerHTML(item.Message);
            if (isImage) {
                Html.Instance.event(EventType.Click, () => this.ShowPreview(item));
            }
            Html.Instance.end.end.render();

            if (isLastInGroup) {
                Html.Instance.p.className("time").i.text(item.FromName + ' - ' + this.dayjs(item.InsertedDate).format("HH:mm DD/MM/YYYY")).end.end.render();
            }
        }
    }

    LoadData() {
        this.RunQuerys().then(data => {
            this.ChatData = data[0];
            this.Conversation = data[1];
            this.Users = data[2];
            this.RenderDiscussions();
            this.RenderChat();
            if (!this.Entity || !this.EntityId || this.EntityId.startsWith("-")) {
                this.HandlerClickBot();
            }
        })
    }

    async UpdateData() {
        var data = await this.RunQuerys();
        this.ChatData = data[0];
        this.Conversation = data[1];
        this.Users = data[2];
    }

    RenderMenu() {
        Html.take(this.Element).nav.className("chat-menu").ul.className("chat-items")
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
    BodyDiscussions;

    RenderDiscussions() {
        Html.take(this.Element).section.className("discussions").div.className("header-discussions").div.tabIndex(-1).event(EventType.Click, (evt) => this.HandlerClickBot(evt)).className("discussion " + (("-1" == this.Entity.Id) ? "message-active" : "") + "")
            .div.className("photo").style("background-image: url('https://forwardx.vn/wp-content/uploads/2025/03/cropped-Icon-Logo-180x180.png');").end
            .div.className("desc-contact")
            .span.className("name").iText("Forwardx").end
            .span.className("description").text("Bot Assistant").end
            .p.className("message").innerHTML('....').end
            .p.className("message").innerHTML('').end.end
            .end.render();
        Html.Instance.div.render();
        this.BodyDiscussions = Html.Context;
        this.RenderBodyDiscussions();
        Html.Instance.endOf(".discussions");
    }

    HandlerClickBot() {
        document.querySelector(".footer-chat").classList.add("d-none");
        this.OptionsElement.classList.add("d-none");
        Html.take(this.HtmlContentChat).clear();
        Html.take(this.TitleText).clear().text("Forwardx");
        Html.take(this.FeatureText).clear().iText("Bot Assistant");
        var rsSaleFunction = localStorage.getItem("SalesFunction2") ? JSON.parse(localStorage.getItem("SalesFunction2")) : [];
        const aiEntry = rsSaleFunction.find((x) => x.Code == "AI_ID");
        const chatbotId = aiEntry && aiEntry.Value;
        const el = document.createElement("zapier-interfaces-chatbot-embed");
        el.setAttribute("chatbot-id", chatbotId.toString());
        el.style.height = "calc(100vh - 14rem)";
        this.HtmlContentChat.appendChild(el);
    }

    LastFromId;
    LastUserId;
    /**
     * @type {String}
     */
    Title = null;

    RenderBodyChat() {
        Html.take(this.HtmlContentChat).clear();
        this.LastFromId = this.ChatData.find(x => x.FromId != this.Token.UserId);
        this.LastUserId = this.ChatData.find(x => x.FromId != this.Token.UserId);

        // render theo nhóm: nếu message tiếp theo cùng FromId -> current không phải cuối nhóm
        for (let i = 0; i < (this.ChatData || []).length; i++) {
            const item = this.ChatData[i];
            const next = (i + 1 < this.ChatData.length) ? this.ChatData[i + 1] : null;
            const isLastInGroup = !(next && next.FromId === item.FromId);
            this.AddMessageToDOM(item, isLastInGroup);
        }

        window.setTimeout(() => {
            if (this.HtmlContentChat && this.HtmlContentChat.parentElement) {
                this.HtmlContentChat.parentElement.scrollTop = this.HtmlContentChat.clientHeight;
            }
        }, 100);
    }

    /**
     * @type {HTMLElement}
     */
    TitleText;
    /**
     * @type {HTMLElement}
     */
    FeatureText;
    /**
     * @type {HTMLElement}
     */
    ContainerPicker;
    /**
     * @type {HTMLElement}
     */
    UserElement;
    /**
     * @type {HTMLElement}
     */
    OptionsElement;
    /**
     * @type {HTMLElement}
     */
    DarkOverlay;
    /**
    * @type {Picker}
    */
    Picker;

    RenderChat() {
        Html.take(this.Element).section.className("chat")
            .div.className("header-chat");
        this.HtmlHeaderChat = Html.Context;
        Html.Instance.div.className("name2").style("width: 100%; display: flex ; align-items: center; gap: 10px;").span.iText(this.Entity.Label)
        this.FeatureText = Html.Context;
        Html.Instance.end.span.text(" : ").end.a.style("color:#fff").className("mr-1").event(EventType.Click, this.OpenPopup.bind(this)).text(this.Entity.FormatChat ? this.Entity.FormatChat.replaceAll("<br>", "") : "");
        this.TitleText = Html.Context;
        Html.end.div.className("d-flex align-items-center");
        this.OptionsElement = Html.Context;
        Html.span.className("d-flex").render();
        this.UserElement = Html.Context;
        Html.Instance.end.i.event(EventType.Click, this.AddUser.bind(this)).className("fas fa-user-plus").end.render();
        Html.Instance.end.end.end.div.className("messages-chat").div.render();
        this.HtmlContentChat = Html.Context;
        this.RenderBodyChat();
        Html.Instance.end.end.div.className("footer-chat")
            .input.type("file").className("attach-file").style("display: none;").event(EventType.Change, this.AttachFile.bind(this)).end
            .i.className("icon fa fa-paperclip clickable").event(EventType.Click, () => this.Element.querySelector(".attach-file").click()).end
            .i.className("icon fa fa-smile clickable").event(EventType.Click, this.ShowEmojiPicker.bind(this)).end
            .textArea.className("write-message").placeHolder("Type your message here");
        this.HtmlIputChat = Html.Context;
        this.HtmlIputChat.addEventListener("keydown", (e) => {
            if (e.KeyCodeEnum() == KeyCodeEnum.Enter && !e.shiftKey) {
                e.preventDefault();
                this.SendChat();
            }
        });
        this.HtmlIputChat.addEventListener("paste", this.HandlePaste.bind(this));
        this.HtmlIputChat.addEventListener("input", function () {
            this.style.height = "36px";
            this.style.height = this.scrollHeight + "px";
        });
        Html.Instance.end.i.className("icon send fal fa-arrow-circle-right clickable").event(EventType.Click, this.SendChat.bind(this)).end.render();
        this.RenderUsers();
    }

    RenderUsers() {
        Html.take(this.UserElement).clear();
        Html.take(this.UserElement).forEach(this.Users || [], (item) => {
            Html.Instance.div.className("photo").style("background-image: url('" + item.Avatar + "');").end.render();
        })
    }

    OpenPopup() {
        if (this.Entity && this.Entity.FeatureName) {
            this.TabEditor.OpenPopup(this.Entity.FeatureName2 || this.Entity.FeatureName, { Id: this.Entity.RecordId }, true);
        }
    }

    AddUser() {
        if (this.Entity && this.Entity.FeatureName) {
            var com = this.TabEditor.Meta.GridPolicies.find(x => x.FieldName == "ReceiverIds");
            this.EditForm.OpenConfig("Invite user to the conversation", async () => {
                if (!this.EditForm.Dirty) {
                    return;
                }
                var com = this.GET("ReceiverIds");
                if (com !== null) {
                    com.Dispose();
                }
                let dirtyPatchDetail = [
                    {
                        Label: "Id",
                        Field: "Id",
                        OldVal: null,
                        Value: this.Entity.Id,
                    },
                    {
                        Label: "IsSend",
                        Field: "IsSend",
                        OldVal: null,
                        Value: 0,
                    },
                    {
                        Label: "FeatureName",
                        Field: "FeatureName",
                        OldVal: null,
                        Value: this.Entity.Label,
                    },
                    {
                        Label: "FeatureName2",
                        Field: "FeatureName2",
                        OldVal: null,
                        Value: this.Entity.FeatureName2,
                    },
                    {
                        Label: "FeatureName3",
                        Field: "FeatureName3",
                        OldVal: null,
                        Value: this.Entity.FeatureName3,
                    },
                    {
                        Label: "FormatChat",
                        Field: "FormatChat",
                        OldVal: null,
                        Value: this.Entity.FormatChat,
                    },
                    {
                        Label: "VoucherTypeId",
                        Field: "VoucherTypeId",
                        OldVal: null,
                        Value: 20,
                    },
                    {
                        Label: "ReceiverIds",
                        Field: "ReceiverIds",
                        OldVal: null,
                        Value: this.EditForm.Entity.ReceiverIds,
                    }
                ]
                let patchModelDetail = {
                    Changes: dirtyPatchDetail,
                    Table: "Conversation",
                    NotMessage: true
                };
                await Client.instance.patchAsync(patchModelDetail);
                this.Dirty = false;
                this.UpdateView(true);
            }, () => { }, true, [com], null, null);
        }
    }

    ShowPreview(item) {
        var rotate = 0;
        var img = null;
        Html.take(document.body).div.className("dark-overlay zoom");
        this.DarkOverlay = Html.Context;
        Html.Instance.innerHTML(item.Message);
        img = Html.Context.querySelector("img");
        Html.Instance.span.className("close").event(EventType.Click, () => {
            this.DarkOverlay.remove();
        }).i.className("fa fa-times").end.end
            .div.className("toolbar")
            .span.className("icon fa fa-undo ro-left").event(EventType.Click, () => {
                rotate -= 90;
                img.style.transform = `rotate(${rotate}deg)`;
            }).end
            .span.className("icon fa fa-cloud-download-alt").event(EventType.Click, () => this.DownloadFile(item)).end
            .span.className("icon fa fa-redo ro-right").event(EventType.Click, () => {
                rotate += 90;
                img.style.transform = `rotate(${rotate}deg)`;
            }).end.end
    }

    DownloadFile(item) {
        var file = document.querySelector(".dark-overlay img");
        Client.download(file.getAttribute("src"));
    }

    ShowEmojiPicker() {
        const pickerContainer = document.createElement('div');
        pickerContainer.style.position = 'absolute';
        pickerContainer.style.bottom = '50px';
        pickerContainer.style.left = '10px';
        pickerContainer.style.zIndex = '1000';

        this.Picker = new Picker({
            data: Data,
            onEmojiSelect: emoji => {
                this.HtmlIputChat.value += emoji.native;
            },
        });

        pickerContainer.appendChild(this.Picker);
        document.body.appendChild(pickerContainer);
        this.ContainerPicker = pickerContainer;
        this.CalcPosition();
        document.addEventListener('click', this.HideEmojiPicker.bind(this), true);
    }

    HideEmojiPicker(event) {
        if (this.ContainerPicker && !this.ContainerPicker.contains(event.target) && !this.HtmlIputChat.contains(event.target)) {
            this.ContainerPicker.remove();
            document.removeEventListener('click', this.HideEmojiPicker.bind(this), true);
        }
    }

    CalcPosition() {
        ComponentExt.AlterPosition(this.ContainerPicker, this.HtmlIputChat);
    }

    AttachFile(event) {
        const files = event.target.files;
        this.UploadAllFiles(files).then(() => {
        }).catch(error => {
            console.error("Failed to upload files:", error);
        });
    }

    RemoveGuid(path) {
        let fileName = path.replace(/^.*[\\\/]/, '');
        let extension = '';
        let nameWithoutExt = fileName;
        const lastDotIndex = fileName.lastIndexOf('.');
        if (lastDotIndex !== -1) {
            extension = fileName.substring(lastDotIndex + 1);
            nameWithoutExt = fileName.substring(0, lastDotIndex);
        }
        const uuidRegex = /[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/g;
        const cleanedName = nameWithoutExt.replace(uuidRegex, '').replace(/\s+/g, ' ').trim();
        return `${cleanedName}.${extension}`;
    }

    async UploadAllFiles(filesSelected) {
        Spinner.AppendTo();
        const files = Array.from(filesSelected).map(this.UploadFile.bind(this));
        let allPath = await Promise.all(files);
        var thumbText = allPath[0];
        const isImage = Utils.IsImage(thumbText);
        const fileName = this.RemoveGuid(thumbText);
        var format = `<a href="${thumbText}" target="_blank" style="color: red; font-weight: 700;"><i class="fal fa-file-alt mr-1"></i>${fileName}</a>`;
        if (isImage) {
            format = `<img src="${thumbText}">`;
        }
        var patch = new PatchVM();
        patch.Table = "ConversationDetail";
        patch.Changes = [{
            Field: "Id",
            Value: Uuid7.NewGuid(),
        },
        {
            Field: "FromId",
            Value: this.Token.UserId,
        },
        {
            Field: "FromName",
            Value: this.Token.NickName,
        },
        {
            Field: "Message",
            Value: format,
        },
        {
            Field: "RecordId",
            Value: this.Entity.RecordId,
        },
        {
            Field: "FormatChat",
            Value: this.Title,
        },
        {
            Field: "Icon",
            Value: this.Entity.Icon,
        },
        {
            Field: "Avatar",
            Value: this.Token.Avatar || "/assets/images/avatar1.png",
        },
        {
            Field: "ConversationId",
            Value: this.Entity.Id,
        },
        {
            Field: "EntityId",
            Value: this.Entity.EntityId,
        }];
        await Client.instance.patchAsync(patch);
        Spinner.Hide();
    }

    async updateBadge() {
        const response = await Client.instance.postAsync(null, "/api/ChatBadge");
        document.querySelector("#badgeMessage").textContent = response > 0 ? response.toString() : "";
    };

    /**
     * @param {File} file
     */
    async UploadFile(file) {
        try {
            const path = await Client.instance.postFilesAsync(file, Utils.FileSvc);
            return path;
        } catch (error) {
            console.error("Error posting file:", error);
            throw error;
        }
    }

    async DeleteMessage(item) {
        Spinner.AppendTo();
        var patch = new PatchVM();
        patch.Table = "ConversationDetail";
        patch.Changes = [{
            Field: "Id",
            Value: item.Id,
        },
        {
            Field: "Message",
            Value: "Tin nhắn đã được thu hồi",
        }];
        await Client.instance.patchAsync(patch);
        Spinner.Hide();
    }

    UpdateView(force = false, dirty = null, ...componentNames) {
        this.UpdateData().then(() => {
            this.Title = this.Entity.FormatChat ? this.Entity.FormatChat.replaceAll("<br>", "") : "";
            this.RenderBodyDiscussions();
            if (force) {
                this.RenderBodyChat();
                Html.take(this.TitleText).clear().text(this.Entity.FormatChat ? this.Entity.FormatChat.replaceAll("<br>", "") : "");
                Html.take(this.FeatureText).clear().iText(this.Entity.Label);
                this.RenderUsers();
            }
        });
    }

    RenderBodyDiscussions() {
        Html.take(this.BodyDiscussions).clear();
        this.Conversation.forEach(item => {
            Html.Instance.div.tabIndex(-1).event(EventType.Click, (evt) => this.HandlerClick(evt, item)).className("discussion " + ((item.Id == this.Entity.Id) ? "message-active" : "") + ((!item.Read) ? "text-unread" : ""))
                .div.className("photo").style("background-image: url('" + item.Icon + "');").end
                .div.className("desc-contact")
                .span.className("name").iText(item.Label).end
                .span.className("description").text(item.FormatChat ? item.FormatChat.replaceAll("<br>", "") : "").end
                .p.className("message").innerHTML(item.Message || '').end
                .p.className("message").innerHTML(item.Time).end.end
                .end.render();
        });
    }

    /**
    * @param {Event} e 
    * @param {{}} item
    */
    HandlerClick(e, item) {
        document.querySelector(".footer-chat").classList.remove("d-none");
        this.OptionsElement.classList.remove("d-none");
        if (item.ConversationReadId) {
            var patch = new PatchVM();
            patch.Table = "ConversationRead";
            patch.Changes = [{
                Field: "Id",
                Value: item.ConversationReadId,
            },
            {
                Field: "Read",
                Value: "1",
            }];
            Client.instance.patchAsync(patch).then(async () => {
                this.Element.querySelectorAll(".discussion").forEach(x => x.classList.remove("message-active"));
                e.target.closest(".discussion").classList.add("message-active");
                this.Entity = item;
                this.EditForm.Entity = item;
                this.UpdateView(true);
                await this.updateBadge();
            });
        }
        else {
            this.Element.querySelectorAll(".discussion").forEach(x => x.classList.remove("message-active"));
            e.target.closest(".discussion").classList.add("message-active");
            this.Entity = item;
            this.EditForm.Entity = item;
            this.UpdateView(true);
            this.updateBadge();
        }
    }

    SendChat() {
        this.Title = Utils.FormatEntity(this.Meta.FormatData, this.Entity);
        var text = this.HtmlIputChat.value;
        if (Utils.isNullOrWhiteSpace(text)) {
            return;
        }
        var patch = new PatchVM();
        patch.Table = "ConversationDetail";
        patch.Changes = [{
            Field: "Id",
            Value: Uuid7.NewGuid(),
        },
        {
            Field: "FromId",
            Value: this.Token.UserId,
        },
        {
            Field: "FromName",
            Value: this.Token.NickName,
        },
        {
            Field: "Message",
            Value: text,
        },
        {
            Field: "RecordId",
            Value: this.Entity.RecordId,
        },
        {
            Field: "FormatChat",
            Value: this.Title,
        },
        {
            Field: "Icon",
            Value: this.Entity.Icon,
        },
        {
            Field: "Avatar",
            Value: this.Token.Avatar || "/assets/images/avatar1.png",
        },
        {
            Field: "ConversationId",
            Value: this.Entity.Id,
        },
        {
            Field: "EntityId",
            Value: this.Entity.EntityId,
        }];
        Client.instance.patchAsync(patch).then();
        window.setTimeout(() => {
            this.updateBadge();
        }, 500);
    }

    async HandlePaste(event) {
        const items = (event.clipboardData || window.clipboardData).items;
        if (items[0].type.indexOf("image") !== -1) {
            const file = items[0].getAsFile();
            this.UploadAllFiles([file]).then(() => {
            }).catch(error => {
                console.error("Failed to upload files:", error);
            });
        }
    }
}
