import { html } from "./utils/html.js";
import { EditableComponent } from './editableComponent.js';
import { Action, keyCodeEnum, Component } from "./models/";
import { Section } from "./index.js";

export class ConfirmDialog extends EditableComponent {
    constructor() {
        super(null);
        this._yesBtn = null;
        this.pElement = null;
        this.openEditForm = null;
        this.Textbox = null;
        this.number = null;
        this.Datepicker = null;
        this.precision = null;
        this.searchEntry = null;
        this.canceled = null;
        this.ignoreNoButton = false;
        this.multipleLine = true;
        this.yesText = "yes";
        this.noText = "no";
        this.cancelText = "close";
        this.needAnswer = false;
        this.comType = "Textbox";
        this.Component = [];
        this.componentGroup = null;
        this.ignoreCancelButton = true;
        this.populateDirty = false;
        this.disposeAfterYes = true;
        this.title = "confirm";
    }
    /** @type {HTMLElement} */
    divElement;
    /** @type {HTMLElement} */
    bodyElement;
    /** @type {Action} */
    yesConfirmed = new Action();;
    /** @type {Action} */
    noConfirmed = new Action();;
    render() {
        if (this.Component) {
            this.Component = JSON.parse(JSON.stringify(this.Component));
            this.Component.forEach(x => {
                x.focusSearch = false;
                x.width = null;
                x.maxWidth = null;
                x.minWidth = null;
            })
        }
        html.take(this.pElement || document.body);
        html.div.className((this.componentGroup ? "backdrop2" : "backdrop")).style((this.componentGroup ? "" : "align-items: center;"));
        this.element = html.context;
        this.parentElement = this.element.parentElement;
        html.instance.div.escape(() => this.dispose()).className("popup-content " + (this.componentGroup ? "form-dialog" : "confirm-dialog")).style((this.componentGroup ? "" : "top: auto;min-width: 350px;"))
            .div.className("popup-title").div.i.className("fas fa-question-circle mr-1").end.iText("confirm", this.editForm.meta.label).end
            .div.className("icon-box").span.className("fa fa-times")
            .event("click", () => this.closeDispose())
            .endOf(".popup-title")
            .div.className("popup-body");
        this.bodyElement = html.context;
        html.instance.div.className("bold").iText(this.title, this.editForm.meta.label).end.div.className("card card-config").event("keydown", (e) => this.hotKeyHandler(e)).marginRem("top", 1).textAlign(this.componentGroup ? "" : "center");
        this.divElement = html.context;
        if (this.needAnswer) {
            if (this.componentGroup && this.componentGroup.componentType == "Section") {
                var sectionInfo = {
                    children: this.componentGroup.children,
                    column: this.componentGroup ? this.componentGroup.column : 12,
                    isSimple: true,
                    className: 'panel group'
                };
                var _basicSearchGroup = Section.renderSection(this.editForm, sectionInfo);
                this.divElement.insertBefore(_basicSearchGroup.element, this.divElement.firstChild);
                _basicSearchGroup.element.style.width = "100%";
                _basicSearchGroup.element.className = "group";
            }
            else {
                if (this.Component && this.Component.length > 0) {
                    this.Component.forEach(x => {
                        if (!this.componentGroup) {
                            x.column = this.hasDispose ? 12 : x.column || 12;
                            x.style = null;
                            x.disabledExp = null;
                            x.active = true;
                            x.canRead = true;
                            x.canReadAll = true;
                            x.visibility = true;
                            x.canWrite = true;
                            x.canWriteAll = true;
                            x.showLabel = true;
                            x.focusSearch = false;
                            x.width = null;
                            x.maxWidth = null;
                            x.minWidth = null;
                        }
                    });
                }
                else {
                    const com = new Component();
                    com.plainText = "Nhập câu trả lời";
                    com.showLabel = false;
                    com.canRead = true;
                    com.canReadAll = true;
                    com.canWrite = true;
                    com.canWriteAll = true;
                    com.visibility = true;
                    com.showLabel = false;
                    com.componentType = "Textarea";
                    com.fieldName = "reasonOfChange";
                    com.row = 3;
                    com.column = 12;
                    com.xxlCol = 12;
                    com.visibility = true;
                    com.multipleLine = this.multipleLine;
                    this.Component.push(com);
                }
                var sectionInfo = {
                    components: this.Component,
                    column: this.componentGroup ? this.componentGroup.column : 12,
                    isSimple: true,
                    isPublic: true,
                    className: 'card-body panel group'
                };
                var _basicSearchGroup = Section.renderSection(this.editForm, sectionInfo);
                this.divElement.insertBefore(_basicSearchGroup.element, this.divElement.firstChild);
                _basicSearchGroup.element.style.width = "100%";
                _basicSearchGroup.element.className = "group card-body";
            }
        }
        html.take(this.bodyElement).div.style("width: 172px; margin: auto; padding: 1rem; display: flex; justify-content: center; gap: 1rem;").button2(this.yesText, "btn btn-success mt-2", "fal fa-check").event("click", () => {
            this.validateAsync().then((isValid) => {
                if (!isValid) {
                    return;
                }
                try {
                    if (this.yesConfirmed) {
                        this.yesConfirmed?.invoke();
                    }
                } catch (ex) {
                    console.error(ex.stack);
                }
                if (this.disposeAfterYes) {
                    this.dispose();
                }
            })
        }).end.render();
        this._yesBtn = html.context;
        if (!this.ignoreNoButton) {
            html.instance.button2(this.noText, "btn btn-danger btn-sm mt-2", "fal fa-window-close")
                .marginRem("left", 1)
                .event("click", () => {
                    try {
                        if (this.noConfirmed.handler) {
                            this.noConfirmed?.invoke();
                        }
                    } catch (ex) {
                        console.error(ex.stack);
                    }
                    this.closeDispose();
                }).end.render();
        }
        if (!this.ignoreCancelButton) {
            html.instance.button2(this.cancelText, "btn btn-success btn-sm mt-2", "fal fa-times")
                .marginRem("left", 1)
                .event("click", () => this.dispose())
                .render();
        }
    }

    dispose() {
        if ((this.Component || this.componentGroup) && !this.hasDispose) {
            var componentIds = this.Component.map(element => element.id || element.fieldName);
            if (componentIds.length == 0 && this.componentGroup && this.componentGroup.components) {
                componentIds = this.componentGroup.components.map(element => element.id || element.fieldName);
            }
            if (componentIds.length == 0 && this.componentGroup) {
                componentIds = this.componentGroup.children.flatMap(x => x.components).map(element => element.id || element.fieldName);
            }
            if (componentIds && componentIds.length > 0) {
                var realComs = this.editForm.childCom.filter(child => componentIds.includes(child.meta.id || child.meta.fieldName) && child.parent.meta.isSimple)
                realComs.forEach(x => x.dispose());
            }
        }
        super.dispose();
    }

    closeDispose() {
        if (this.canceled) {
            this.canceled();
        }
        if ((this.Component || this.componentGroup) && !this.hasDispose) {
            var componentIds = this.Component.map(element => element.id || element.fieldName);
            if (componentIds.length == 0 && this.componentGroup && this.componentGroup.components) {
                componentIds = this.componentGroup.components.map(element => element.id || element.fieldName);
            }
            if (componentIds.length == 0 && this.componentGroup) {
                componentIds = this.componentGroup.children.flatMap(x => x.components).map(element => element.id || element.fieldName);
            }
            if (componentIds && componentIds.length > 0) {
                var realComs = this.editForm.childCom.filter(child => componentIds.includes(child.meta.id || child.meta.fieldName) && child.parent.meta.isSimple)
                realComs.forEach(x => x.dispose());
            }
        }
        super.dispose();
    }

    /**
     * @param {any} content
     * @param {any} yesConfirm
     */
    static renderConfirm(content, yesConfirm, noConfirm = null) {
        const meta = {
            content: content,
        };
        const confirm = new ConfirmDialog();
        confirm.content = content;
        confirm.render();
        confirm.yesConfirmed = yesConfirm;
        confirm.noConfirmed = noConfirm;
        return confirm;
    }

    /**
     * 
     * @param {event} e 
     */
    hotKeyHandler(e) {
        if (e.keyCode() === keyCodeEnum.enter) {
            this._yesBtn.click();
        }
    }
}


