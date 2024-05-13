import { GridView } from './gridView.js';
import { Utils } from './utils/utils.js';

import { Html } from "./utils/html.js";
import { Textbox } from './textbox.js'
import { Datepicker } from './datepicker.js'
import EditableComponent from './editableComponent.js';
import { Component } from "./models/component.js";
import { Message } from "./utils/message.js";
import { NumBox } from "./numbox.js";
import { KeyCodeEnum } from "./models/enum.js";


export class ConfirmDialog extends EditableComponent {
    constructor() {
        super(null);
        this._yesBtn = null;
        this.PElement = null;
        this.OpenEditForm = null;
        this.Textbox = null;
        this.Number = null;
        this.Datepicker = null;
        this.Precision = null;
        this.SearchEntry = null;
        this.YesConfirmed = null;
        this.NoConfirmed = null;
        this.Canceled = null;
        this.IgnoreNoButton = false;
        this.MultipleLine = true;
        this.YesText = "Đồng ý";
        this.NoText = "Không";
        this.CancelText = "Đóng";
        this.Content = "Bạn có chắc chắn muốn xóa dữ liệu?";
        this.NeedAnswer = false;
        this.ComType = "Textbox";
        this.IgnoreCancelButton = true;
        this.PopulateDirty = false;
        this.Title = "Xác nhận";
    }
    DisposeAfterYes = true;

    Render() {
        let element = Html.Take(this.PElement || document.body);
        element.Div.ClassName("backdrop").Style("align-items: center;").Escape(() => this.Dispose());
        if (this.PElement) {
            Html.Instance.Style("position: fixed !important;");
        }
        this.Element = Html.Context;
        this.ParentElement = this.Element.parentElement;
        let popupContent = Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: auto;")
            .Div.ClassName("popup-title").IHtml(this.Title)
            .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
            .Event("click", () => this.CloseDispose())
            .EndOf("popup-title")
            .Div.ClassName("popup-body");

        popupContent.P.IHtml(this.Content).End.Div.Event("keydown", (e) => this.HotKeyHandler(e)).MarginRem("top", 1);
        if (this.NeedAnswer) {
            if (this.ComType === "Textbox") {
                const com = new Component();
                com.PlainText = "Nhập câu trả lời";
                com.ShowLabel = false;
                com.FieldName = Message.ReasonOfChange;
                com.Row = 2;
                com.MultipleLine = this.MultipleLine;
                const textbox = new Textbox(com);
                this.AddChild(textbox);
                Html.Instance.End.Render();
            }
            if (this.ComType === "Number") {
                const meta = {
                    PlainText: "Nhập số",
                    FieldName: 'ReasonOfChange',
                    Visibility: true,
                    ShowLabel: false
                };
                const number = new NumBox(meta);
                this.AddChild(number);
            }
            if (this.ComType === "Datepicker") {
                const meta = {
                    plainText: "Chọn ngày",
                    showLabel: false,
                    fieldName: 'ReasonOfChange',
                    row: 2,
                    focusSearch: true,
                    visibility: true,
                    precision: this.Precision
                };
                const datepicker = new Datepicker(meta);
                this.AddChild(datepicker);
            }
        }
        let yesButton = Html.Instance.Button2(this.YesText, "button info small", "fa fa-check")
            .Event("click", async () => {
                let isValid = await this.ValidateAsync();
                if (!isValid) {
                    return;
                }
                try {
                    if (this.YesConfirmed) {
                        this.YesConfirmed();
                    }
                } catch (ex) {
                    console.error(ex.stack);
                }
                if (this.DisposeAfterYes) {
                    this.Dispose();
                }
            }).Render();
        this._yesBtn = Html.Context;

        if (!this.IgnoreNoButton) {
            Html.Instance.Button2(this.NoText, "button alert small", "mif-exit")
                .MarginRem("left", 1)
                .Event("click", () => {
                    try {
                        this.NoConfirmed?.();
                    } catch (ex) {
                        console.error(ex.stack);
                    }
                    this.CloseDispose();
                }).Render();
        }

        if (!this.IgnoreCancelButton) {
            Html.Instance.Button2(this.CancelText, "button info small", "fa fa-times")
                .MarginRem("left", 1)
                .Event("click", () => this.Dispose())
                .Render();
        }
    }

    Dispose() {
        super.Dispose();
    }

    CloseDispose() {
        if (this.Canceled) {
            this.Canceled();
        }
        super.Dispose();
    }

    /**
     * @param {any} content
     * @param {any} yesConfirm
     */
    static RenderConfirm(content, yesConfirm, noConfirm = null) {
        const meta = {
            Content: content,
        };
        const confirm = new ConfirmDialog();
        confirm.Content = content;
        confirm.Render();
        confirm.YesConfirmed = yesConfirm;
        confirm.NoConfirmed = noConfirm;
        return confirm;
    }

    /**
     * 
     * @param {Event} e 
     */
    HotKeyHandler(e) {
        if (e.KeyCode() === KeyCodeEnum.Enter) {
            this._yesBtn.click();
        }
    }
}


