import EditableComponent from './editableComponent.js';
import { Html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { ValidationRule } from "./models/validationRule.js";
import { LangSelect } from "./utils/langSelect.js";
import { Client } from "./clients/client.js";
import EventType from './models/eventType.js';
import { ComponentType } from './models/componentType.js';
import { string } from './utils/ext.js';
import EditableComponent from './editableComponent.js';
import ObservableArgs from './models/observable.js';

class Image extends EditableComponent {
    static PathSeparator = "    ";
    static PNGUrlPrefix = "data:image/png;base64,";
    static JpegUrlPrefix = "data:image/jpeg;base64,";
    static GuidLength = 36;
    /**
     * @param {Component} ui
     */

    constructor(ui) {
        super(ui);
        this._path = '';
        /** @type {HTMLInputElement} */
        this._input = document.createElement('input');
        this._preview = null;
        this._disabledDelete = false;
        /** @type {HTMLDivElement} */
        this._gallerys = document.createElement('div');
        this.DataSource = this.Meta.Template || "image/*";
        this.DefaultValue = '';
        this.fileUploaded = new Event('FileUploaded');
        this.zoomLevel = 0;
        this.flagZoomIn = 1;
        this.zoomMaxLevel = 3;
    }

    get path() {
        return this._path;
    }

    set path(value) {
        const galleryElements = this.Element.parentElement.querySelectorAll(".gallery");
        galleryElements.forEach(el => el.remove());
        this._path = value;
        if (this.Entity) {
            this.Entity.setComplexPropValue(this.FieldName, this._path);
        }

        if (!this._path || this._path.trim() === '') {
            return;
        }

        const updatedImages = this._path.split(Image.PathSeparator);
        if (!updatedImages || updatedImages.length === 0) {
            return;
        }

        updatedImages.forEach(x => {
            this.renderFileThumb(x);
        });
    }

    get disabled() {
        return this.disabled;
    }

    set disabled(value) {
        if (this._input !== null) {
            this._input.disabled = value; 
        }

        this.disabled = value; 

        if (value) {
            this.ParentElement.setAttribute("disabled", ""); 
        } else {
            this.ParentElement.removeAttribute("disabled");
        }
    }

    render() {
        this._path = Utils.GetPropValue(this.Entity,this.FieldName)?.toString();
        const paths = this._path ? this._path.split(Image.PathSeparator) : [];
        this.renderUploadForm();
        this.Path = this._path; 
        this.DOMContentLoaded?.invoke();
        this.Element.closest("td")?.addEventListener("keydown", this.ListViewItemTab);
    }

    renderFileThumb(path) {
        const gallery = document.createElement('div');
        gallery.className = "gallery";
        this._gallerys.appendChild(gallery);
    
        const thumbText = this.removeGuid(path);
        const isImage = this.isImage(path);
    
        if (isImage) {
            const img = document.createElement('img');
            img.className = "image";
            Object.assign(img.style, this.Meta.ChildStyle); 
            img.src = (path.includes("http") ? path : Client.Origin + this.decodeSpecialChar(path));
            gallery.appendChild(img);
            img.addEventListener('click', () => this.preview(path)); 
        } else {
            const link = document.createElement('a');
            link.className = thumbText.includes("pdf") ? "fal fa-file-pdf" : "fal fa-file";
            link.title = this.decodeSpecialChar(thumbText);
            Object.assign(link.style, this.Meta.ChildStyle);
            link.href = (path.includes("http") ? path : Client.Origin + this.decodeSpecialChar(path));
            gallery.appendChild(link);
        }
    
        if (!this.disabled) {
            const deleteBtn = document.createElement('i');
            deleteBtn.className = "fas fa-trash-alt";
            deleteBtn.addEventListener('click', () => this.removeFile(path));
            gallery.appendChild(deleteBtn);
        }
    
        return this._gallerys;
    }

    removeGuid(path) {
        let thumbText = path;
        if (path.length > Image.GuidLength) {
            const fileName = Utils.GetFileNameWithoutExtension(path);
            thumbText = fileName.substring(0, fileName.length - Image.GuidLength) + '.' + path.split('.').pop();
        }
        return thumbText;
    }

    setCanDeleteImage(canDelete) {
        this._disabledDelete = !canDelete;
        if (canDelete) {
            this.updateView();
        }
    }

    preview(path) {
        if (!this.isImage(this.path)) {
            console.log("Not an image: Downloading file.");
            window.location.href = path; 
            return;
        }

        let img = document.createElement('img');
        img.src = this.path;
        img.style = "width:100%;"; 

        if (this._preview) {
            this._preview.remove();
        }

        this._preview = img;
        document.body.appendChild(img);

        img.addEventListener('click', () => {
            img.remove();
            this._preview = null;
        });
    }

    zoomImage(img) {
        if (this.flagZoomIn < this.zoomMaxLevel) {
            if (this.zoomLevel === 0 || this.zoomLevel < this.zoomMaxLevel) {
                img.style.cursor = "zoom-in";
                img.height += 450;
                img.width += 400;
                this.zoomLevel++;
                this.flagZoomIn++;
            }
        } else if (this.flagZoomIn === this.zoomMaxLevel) {
            if (this.zoomLevel === 1) {
                this.flagZoomIn = 1;
            }
            img.style.cursor = "zoom-out";
            img.height -= 450;
            img.width -= 400;
            this.zoomLevel--;
        }
    }

    moveAround(event, path) {
        const keyCode = event.keyCode;
        if (![37, 39].includes(keyCode)) {
            return;
        }
    
        const img = event.target.querySelector('img');
        if (!img) {
            return;
        }
    
        if (keyCode === 37) {  // LeftArrow
            this.moveLeft(path, img);
        } else if (keyCode === 39) {  // RightArrow
            this.moveRight(path, img);
        }
    }

    moveLeft(path, img) {
        const imageSources = this.path.split("    ");
        let index = imageSources.indexOf(path);
        if (index === 0) {
            index = imageSources.length - 1;
        } else {
            index--;
        }
        img.Src = (this.path.includes("http") ? "" : Client.Origin) + imageSources[index];
        return imageSources[index];
    }

    moveRight(path, img) {
        const imageSources = this.path.split("    ");
        let index = imageSources.indexOf(path);
        if (index === imageSources.length - 1) {
            index = 0; 
        } else {
            index++; 
        }
    
        img.Src = (this.path.includes("http") ? "" : Client.Origin) + imageSources[index];
        return imageSources[index];
    }

    openFileDialog(event) {
        if (this.disabled) {
            return;
        }
    
        this.openNativeFileDialog(event);
    
        // if (typeof navigator.camera === 'undefined') {
        //     this._input.click();
        // } else {
        //     this.renderImageSourceChooser();
        // }
    }

    renderUploadForm() {
        // Render form để chọn tệp tin
        const isMultiple = this.Meta.Precision === 0;
        this._input.type = "file";
        this._input.name = "files";
        this._input.className = "d-none";
        this._input.accept = this.DataSource;
        if (isMultiple) {
            this._input.multiple = true;
        }
        this._input.addEventListener('change', this.uploadSelectedImages.bind(this));

        const fileUploadDiv = document.createElement('div');
        fileUploadDiv.className = "file-upload";
        const fileIcon = document.createElement('i');
        fileIcon.className = "fal fa-file-alt";
        fileUploadDiv.appendChild(fileIcon);
        fileUploadDiv.addEventListener('click', this.openFileDialog.bind(this));

        const galleryDiv = document.createElement('div');
        galleryDiv.className = "gallerys";
        fileUploadDiv.appendChild(galleryDiv);

        this._gallerys = galleryDiv;
        this.ParentElement.appendChild(fileUploadDiv);
    }

    // removeFile(event, removedPath) {
    //     if (this.disabled) {
    //         return;
    //     }
    //     event.StopPropagation()
    // }

    uploadSelectedImages(event) {
        event.preventDefault();
        if (this.EditForm.isLock) {
            console.log("Edit form is locked.");
            return;
        }
    
        const files = event.target.files;
        if (!files.length) {
            console.log("No files selected.");
            return;
        }
    
        const oldVal = this._path; 
    
        // Giả định uploadAllFiles là hàm bạn định nghĩa riêng để tải lên tất cả các tệp
        uploadAllFiles(files).then(() => {
            this.Dirty = true;  // Giả định dirty là biến trạng thái đã được định nghĩa
            this._input.value = '';  // Giả định _input là tham chiếu đến input element
            this.UserInput?.Invoke(new ObservableArgs ({ NewData : this._path, OldData : oldVal, FieldName : this.FieldName, EvType : EventType.Change }));
            dispatchEvent(this.Meta.Events, 'change', this.Entity);  
        }).catch(error => {
            console.error("Failed to upload files:", error);
        });
    }

    uploadBase64Image(base64Image, fileName) {
        return Client.Instance.SubmitAsync(
            {
                Value : base64Image,
                Url : `/user/image/?name=${fileName}`,
                IsRawString : true,
                Method : Enums.HttpMethod.POST
            });
    }

    // updateView(force = false, dirty = null, ...componentNames) {
    //     this.path = Utils.GetPropValue(this.Entity, this.fieldName)?.toString();
    //     this.updateView(force, dirty, ...componentNames);
    // }

    // isImage(path) {
    //     return path.match(/\.(jpeg|jpg|gif|png)$/i) != null;
    // }

    // decodeSpecialChar(text) {
    //     return decodeURIComponent(text);
    // }
   
}

window.Core2 = window.Core2 || {};
window.Core2.Image = Image;

export default Image;
