import { EditableComponent } from './editableComponent.js';
import { Utils } from "./utils/utils.js";
import EventType from './models/eventType.js';
import { Action } from "./models/action.js";
import { Component } from './models/component.js';
import { ConfirmDialog } from './confirmDialog.js';
import { Uuid7 } from './structs/uuidv7.js';
import { Spinner } from './spinner.js';
import { Client } from './clients/client.js';
import { Html } from './utils/html.js';

export class Image extends EditableComponent {
    static pathSeparator = "    ";
    static pNGUrlPrefix = "data:image/png;base64,";
    static jpegUrlPrefix = "data:image/jpeg;base64,";
    static guidLength = 36;
    /**
     * Create instance of component
     * @param {Component} ui 
     * @param {HTMLElement } ele 
     */
    constructor(ui, el) {
        super(ui, el);
        this._path = '';
        /** @type {hTMLInputElement} */
        this._input = document.createElement('input');
        this._preview = null;
        this._disabledDelete = false;
        /** @type {hTMLDivElement} */
        this._gallerys = document.createElement('div');
        this.dataSource = this.meta.Template || "image/*";
        this.defaultValue = '';
        this.fileUploaded = new Action();
        this.zoomLevel = 0;
        this.flagZoomIn = 1;
        this.zoomMaxLevel = 3;
    }

    get Path() {
        return this._path;
    }

    set Path(value) {
        this._gallerys.innerHTML = '';
        this._path = value;
        if (this.entity) {
            this.entity[this.Name] = this._path;
        }

        if (!this._path || this._path.trim() === '') {
            return;
        }

        const updatedImages = this._path.split(Image.pathSeparator);
        if (!updatedImages || updatedImages.length === 0) {
            return;
        }

        updatedImages.forEach(x => {
            this.renderFileThumb(x);
        });
    }

    get imageSources() {
        return this.Path ? this.Path.split(Image.pathSeparator) : null;
    }

    Render() {
        this._path = this.entity[this.Name] || null;
        this.renderUploadForm();
        this.Path = this._path;
        this.dOMContentLoaded?.invoke();
        this.element.closest("td")?.addEventListener("keydown", this.listViewItemTab);
    }

    renderFileThumb(path) {
        const gallery = document.createElement('div');
        gallery.className = "gallery";
        this._gallerys.appendChild(gallery);
        const thumbText = this.removeGuid(path).toLowerCase();
        const isImage = Utils.isImage(thumbText);
        var linkF = (path.includes("http") ? path : Client.api + "/" + Utils.decodeSpecialChar(path));
        if (isImage) {
            const img = document.createElement('img');
            img.className = "image";
            Object.assign(img.style, this.meta.childStyle);
            img.src = linkF;
            gallery.appendChild(img);
            img.addEventListener('click', () => this.previewImage(path));
        } else {
            const link = document.createElement('a');
            var icon = document.createElement('i');
            if (thumbText.includes(".pdf")) {
                icon.className = "fal fa-file-pdf mr-1";
                link.addEventListener("click", () => this.previewPDF(linkF));
            } else if (thumbText.includes(".xls") || thumbText.includes(".xlsx")) {
                icon.className = "fal fa-file-excel mr-1";
                link.addEventListener("click", () => this.dowloadPdf(linkF));
            }
            else if (thumbText.includes(".doc") || thumbText.includes(".docx")) {
                icon.className = "fal fa-file-word mr-1";
                link.addEventListener("click", () => this.previewOfficeFile(linkF));
            }
            else if (thumbText.includes(".txt")) {
                icon.className = "fal fa-file-alt mr-1";
                link.addEventListener("click", () => this.previewTextFile(linkF));
            } else {
                icon.className = "fal fa-file mr-1";
            }
            link.appendChild(icon);
            var span = document.createElement('span');
            span.textContent = thumbText;
            link.appendChild(span);
            gallery.appendChild(link);
        }

        if (!this.disabled) {
            const deleteBtn = document.createElement('i');
            deleteBtn.className = "fas fa-trash-alt";
            deleteBtn.addEventListener('click', () => this.removeFile(path, thumbText));
            gallery.appendChild(deleteBtn);
        }

        return this._gallerys;
    }

    /**
    @type {hTMLIFrameElement}
    */
    iFrameElement
    /**
     */
    /**
    @type {HTMLElement}
    */
    darkOverlay
    /**
     */
    openPopupIFrame(url, img) {
        var rotate = 0;
        var img2 = null;
        Html.take(document.body).div.className("dark-overlay zoom");
        this.darkOverlay = Html.context;
        if (img) {
            Html.instance.img.src(url);
            img2 = Html.context;
            Html.instance.end.render();
            Html.instance.span.className("close").event(EventType.Click, () => {
                this.darkOverlay.remove();
            }).i.className("fa fa-times").end.end
                .div.className("toolbar")
                .span.className("icon fa fa-undo ro-left").event(EventType.Click, () => {
                    rotate -= 90;
                    img2.style.transform = `rotate(${rotate}deg)`;
                }).end
                .span.className("icon fa fa-cloud-download-alt").event(EventType.Click, () => this.downloadFile()).end
                .span.className("icon fa fa-redo ro-right").event(EventType.Click, () => {
                    rotate += 90;
                    img2.style.transform = `rotate(${rotate}deg)`;
                }).end.end.render();
        }
        else {
            Html.instance.span.className("close").event(EventType.Click, () => {
                this.darkOverlay.remove();
            }).i.className("fa fa-times").end.end.render();
            Html.instance.iFrame.className("container-rpt").style("margin-top: 4rem; background: rgb(255, 255, 255); overflow: auto; min-height: calc(-4rem + 100vh); width: 100%;").width("100%");
            this.iFrameElement = Html.context;
            this.iFrameElement.src = url;
        }
    }

    downloadFile() {
        var file = document.querySelector(".dark-overlay img");
        Client.download(file.getAttribute("src"));
    }

    closePreview() {
        this.Preview.remove();
    }

    dowloadPdf(url) {
        Client.download(url);
    }

    previewPDF(link) {
        window.open(link, "_blank");
    }

    previewImage(link) {
        this.openPopupIFrame(link, true);
    }

    previewOfficeFile(link) {
        this.openPopupIFrame(`https://docs.google.com/viewer?url=${encodeURIComponent(link)}&embedded=true`);
    }

    previewTextFile(link) {
        this.openPopupIFrame(link);
    }

    removeGuid(path) {
        let fileName = path.replace(/^.*[\\\/]/, '');
        let extension = '';
        let nameWithoutExt = fileName;

        // Tách đuôi file
        const lastDotIndex = fileName.lastIndexOf('.');
        if (lastDotIndex !== -1) {
            extension = fileName.substring(lastDotIndex + 1);
            nameWithoutExt = fileName.substring(0, lastDotIndex);
        }

        // Regex tìm chuỗi UUID dạng 8-4-4-4-12
        const uuidRegex = /[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/g;

        // Xóa các UUID khỏi tên file
        const cleanedName = nameWithoutExt.replace(uuidRegex, '').replace(/\s+/g, ' ').trim();

        return `${cleanedName}.${extension}`;
    }

    setCanDeleteImage(canDelete) {
        this._disabledDelete = !canDelete;
        if (canDelete) {
            this.updateView();
        }
    }

    Preview(path) {
        this.openPopupIFrame(path);
    }

    renderUploadForm() {
        const handler = this.uploadSelectedImages.bind(this);
        Html.take(this.parentElement).div.className("ms-upload")
            .span
            .div.className("ms-img-upload")
            .div.className("ms-input-upload")
        Html.instance.div.className("w-full-100").span.className("text-input far fa-cloud-upload").input.type("file").attr("title", "").attr("accept", "");
        if (this.meta.isMultiple) {
            Html.instance.attr("multiple", "multiple");
        }
        this.element = this._input = Html.context;
        this._input.accept = ".txt, .jpg, .jpeg, .png, .doc, .docx, .xls, .xlsx, .pdf";
        this.element.addEventListener("drop", (e) => this.uploadDropImages(e));
        this.element.addEventListener("change", handler);
        if (!this.canWrite) {
            this._input.readOnly = true;
        }
        Html.instance.end.end.div.className("img-upload");
        this._gallerys = Html.context;
    }

    removeFile(removedPath, thumbText) {
        if (this.disabled) {
            return;
        }
        if (!removedPath || removedPath.trim() === '') {
            return;
        }
        const message = `Do you want delete ${thumbText}`;
        const confirmDialog = new ConfirmDialog();
        confirmDialog.Title = message;
        confirmDialog.editForm = this.editForm;
        confirmDialog.pElement = this.editForm.element;
        confirmDialog.render();
        confirmDialog.yesConfirmed.add(() => {
            const oldVal = this._path;
            const newPath = this._path.replace(removedPath, Image.pathSeparator)
                .replace(Image.pathSeparator + Image.pathSeparator, Image.pathSeparator)
                .split(Image.pathSeparator).filter(x => x != null && x != "");
            this.Path = newPath.join(Image.pathSeparator);
            this.Dirty = true;
            const observable = { newData: this._path, oldData: oldVal, fieldName: this.Name, evType: EventType.Change };
            this.userInput?.invoke(observable);
            this.dispatchEvent(this.meta.Events, EventType.Change, this, this.entity).then();
        });
    }

    uploadSelectedImages(event) {
        event.preventDefault();
        if (this.editForm.isLock) {
            console.log("Edit form is locked.");
            return;
        }

        const files = event.target.files;
        if (!files.length) {
            console.log("No files selected.");
            return;
        }
        const oldVal = this._path;
        this.uploadAllFiles(files).then(() => {
            this.Dirty = true;
            this._input.value = '';
            const observable = { newData: this._path, oldData: oldVal, fieldName: this.Name, evType: EventType.Change };
            this.userInput?.invoke(observable);
            this.dispatchEvent(this.meta.Events, EventType.Change, this, this.entity).then();
        }).catch(error => {
            console.error("Failed to upload files:", error);
        });
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.Path = this.entity[this.meta.fieldName];
        super.updateView(force, dirty, ...componentNames);
    }

    /**
     * @param {File} file
     */
    async uploadFile(file) {
        try {
            const path = await Client.instance.postFilesAsync(file, Utils.fileSvc);
            await Client.instance.patchAsync({
                Table: "fileUpload",
                Changes: [
                    { Field: "Id", Value: Uuid7.newGuid() },
                    { Field: "entityName", Value: this.meta.refName },
                    { Field: "recordId", Value: this.entityId },
                    { Field: "sectionId", Value: this.meta.componentGroupId },
                    { Field: "fieldName", Value: this.Name },
                    { Field: "fileName", Value: file.name },
                    { Field: "filePath", Value: path }
                ],
            });
            return path;
        } catch (error) {
            console.error("Error posting file:", error);
            throw error;
        }
    }

    /**
     * @param {Iterable<any> | arrayLike<any>} filesSelected
     */
    async uploadAllFiles(filesSelected) {
        Spinner.appendTo();
        const files = Array.from(filesSelected).map(this.uploadFile.bind(this));
        let allPath = await Promise.all(files);
        if (!allPath.length) {
            return;
        }
        if (this.meta.isMultiple) {
            if (Utils.isNullOrWhiteSpace(this.Path)) {
                allPath = [...new Set(allPath.join(Image.pathSeparator).trim().split(Image.pathSeparator))];
            }
            else {
                allPath = [...new Set((this.Path + Image.pathSeparator + allPath.join(Image.pathSeparator)).trim().split(Image.pathSeparator))];
            }
        }
        this.Path = allPath.join(Image.pathSeparator);
        Spinner.Hide();
        this.fileUploaded?.invoke();
    }

    getValueText() {
        if (!this.imageSources || this.imageSources.length === 0) {
            return null;
        }
        return this.imageSources.map(path => {
            const label = this.removeGuid(path);
            return `<a target="_blank" href="${path}">${label}</a>`;
        }).join(",");
    }
}
