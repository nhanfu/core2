import { EditableComponent } from "./editableComponent";
import { Html } from "./utils/html.js";
import { Component } from "./models/component.js";
import tinymce, { Editor } from "tinymce";
import { Uuid7 } from "./structs/uuidv7.js";
import { Image } from "./image.js";
import EventType from "./models/eventType.js";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
export class RichTextBox extends EditableComponent {
    /**
    * @param {Component} ui
    * @param {HTMLElement} [ele=null] 
    */
    constructor(ui, ele = null) {
        super(ui, ele);
        this.defaultValue = "";
        if (this.meta.row <= 0) {
            this.meta.row = 1;
        }
        if (ele != null) {
            this.parentElement = ele;
            this.bindingWebComponent();
        }
        else {
            this.parentElement = this.parentElement ?? Html.context;
            this.bindingWebComponent();
        }
        this.parentElement.appendChild(this.element);
    }

    setOldTextAndVal() {
        this.originalText = new dOMParser().parseFromString(this.entity[this.meta.fieldName], 'text/html').body.textContent;
        this.oldValue = this.originalText;
    }

    bindingWebComponent() {
        Html.take(this.parentElement).textArea.id("RE_" + Uuid7.Guid());
        this.element = Html.context;
    }

    render() {
        this.initCkEditor().then();
    }
    /**
     * @type {Editor}
     */
    quill;
    async initCkEditor() {
        var self = this;
        this.setDefaultVal();
        this.setOldTextAndVal();
        this.quill = (await tinymce.init({
            license_key: 'gpl',
            selector: '#' + this.element.id,
            plugins: [
                'advlist', 'autolink', 'lists', 'link', 'image', 'charmap', 'preview',
                'anchor', 'searchreplace', 'visualblocks', 'code', 'fullscreen',
                'insertdatetime', 'media', 'table', 'wordcount'
            ],
            content_css: ['https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&display=swap', '/custom.css?v=dhjsjdsjhdjshjdhsjdhjshjhj'],
            toolbar: '',
            font_size_formats: '8pt 9pt 10pt 11pt 12pt 13pt 14pt 15pt 16pt 17pt 18pt 24pt 36pt 48pt',
            contextmenu: "margin-page | link image inserttable | table add-background-img gen-table-excel | tablename groupby | classProp titleProp stylesProp | Viewpdf Viewhistory",
            images_upload_handler: self.imageHandler.bind(self),
            height: this.meta.precision || 250,
            setup: function (editor) {
                self.quill = editor;
                editor.on('init', function () {
                    editor.getDoc().body.style.fontFamily = 'Montserrat';
                    editor.getDoc().body.style.fontSize = '10pt';
                    editor.setContent(self.entity[self.meta.fieldName] || '');
                });
                editor.on('Change', function (e) {
                    self.entity[self.meta.fieldName] = editor.getBody().innerHTML.replace(/<br[^>]*data-mce-bogus="1"[^>]*>/gi, "");
                    self.Dirty = true;
                });
                if (self.Token.roleNames.some(x => x == "BOD" || x == "ADMIN")) {
                    editor.ui.registry.addMenuItem('tablename', {
                        text: 'Table Name',
                        onAction: function () {
                            var selectedTr = editor.dom.getParent(editor.selection.getStart(), 'tbody');
                            if (selectedTr) {
                                var currentCustomData = editor.dom.getAttrib(selectedTr, 'data-table') || '';
                                editor.windowManager.open({
                                    title: 'Table Name',
                                    body: {
                                        type: 'panel',
                                        items: [
                                            {
                                                type: 'input',
                                                name: 'customData',
                                                label: 'Table Name',
                                            }
                                        ]
                                    },
                                    initialData: {
                                        customData: currentCustomData
                                    },
                                    buttons: [
                                        {
                                            type: 'submit',
                                            text: 'Save'
                                        },
                                        {
                                            type: 'cancel',
                                            text: 'Close'
                                        }
                                    ],
                                    onSubmit: function (dialog) {
                                        var data = dialog.getData();
                                        editor.dom.setAttrib(selectedTr, 'data-table', data.customData);
                                        dialog.close();
                                    }
                                });
                            } else {
                                editor.windowManager.alert('Please select a table row to set properties.');
                            }
                        }
                    });
                    editor.ui.registry.addMenuItem('groupby', {
                        text: 'Group by',
                        onAction: function () {
                            var selectedTr = editor.dom.getParent(editor.selection.getStart(), 'tbody');
                            if (selectedTr) {
                                var currentCustomData = editor.dom.getAttrib(selectedTr, 'data-group') || '';
                                editor.windowManager.open({
                                    title: 'Group by',
                                    body: {
                                        type: 'panel',
                                        items: [
                                            {
                                                type: 'input',
                                                name: 'customData',
                                                label: 'Group by',
                                            }
                                        ]
                                    },
                                    initialData: {
                                        customData: currentCustomData // Thiết lập giá trị ban đầu cho input
                                    },
                                    buttons: [
                                        {
                                            type: 'submit',
                                            text: 'Save'
                                        },
                                        {
                                            type: 'cancel',
                                            text: 'Close'
                                        }
                                    ],
                                    onSubmit: function (dialog) {
                                        var data = dialog.getData();
                                        editor.dom.setAttrib(selectedTr, 'data-group', data.customData);
                                        dialog.close();
                                    }
                                });
                            } else {
                                editor.windowManager.alert('Please select a table row to set properties.');
                            }
                        }
                    });
                    editor.ui.registry.addMenuItem('classProp', {
                        text: 'Class Name Table',
                        onAction: function () {
                            var selectedTr = editor.dom.getParent(editor.selection.getStart(), 'table');
                            if (selectedTr) {
                                var currentCustomData = editor.dom.getAttrib(selectedTr, 'class') || '';
                                editor.windowManager.open({
                                    title: 'Class Table Name',
                                    body: {
                                        type: 'panel',
                                        items: [
                                            {
                                                type: 'input',
                                                name: 'customData',
                                                label: 'Class Name',
                                            }
                                        ]
                                    },
                                    initialData: {
                                        customData: currentCustomData // Thiết lập giá trị ban đầu cho input
                                    },
                                    buttons: [
                                        {
                                            type: 'submit',
                                            text: 'Save'
                                        },
                                        {
                                            type: 'cancel',
                                            text: 'Close'
                                        }
                                    ],
                                    onSubmit: function (dialog) {
                                        var data = dialog.getData();
                                        var newClass = data.customData.trim();

                                        if (newClass) {
                                            var currentClassList = currentCustomData.split(' ').filter(Boolean);
                                            if (!currentClassList.includes(newClass)) {
                                                currentClassList.push(newClass);
                                            }
                                            editor.dom.setAttrib(selectedTr, 'class', currentClassList.join(' '));
                                        }
                                        else {
                                            editor.dom.setAttrib(selectedTr, 'class', '');
                                        }
                                        dialog.close();
                                    }
                                });
                            } else {
                                editor.windowManager.alert('Please select a table to set properties.');
                            }
                        }
                    });
                    editor.ui.registry.addMenuItem('margin-page', {
                        text: 'Margin',
                        onAction: function () {
                            const root = editor.getBody();
                            let marginWrapper = root.querySelector('div.m-class');

                            let currentMargin = marginWrapper ? {
                                top: marginWrapper.style.marginTop || '',
                                right: marginWrapper.style.marginRight || '',
                                bottom: marginWrapper.style.marginBottom || '',
                                left: marginWrapper.style.marginLeft || ''
                            } : {
                                top: '0', right: '0', bottom: '0', left: '0'
                            };

                            editor.windowManager.open({
                                title: 'Page Margin Settings',
                                body: {
                                    type: 'panel',
                                    items: [
                                        {
                                            type: 'grid',
                                            columns: 2,
                                            items: [
                                                { type: 'input', name: 'marginTop', label: 'Margin Top (px)', inputMode: 'numeric' },
                                                { type: 'input', name: 'marginBottom', label: 'Margin Bottom (px)', inputMode: 'numeric' },
                                                { type: 'input', name: 'marginLeft', label: 'Margin Left (px)', inputMode: 'numeric' },
                                                { type: 'input', name: 'marginRight', label: 'Margin Right (px)', inputMode: 'numeric' }
                                            ]
                                        }
                                    ]
                                },
                                initialData: {
                                    marginTop: currentMargin.top.replace('px', ''),
                                    marginBottom: currentMargin.bottom.replace('px', ''),
                                    marginLeft: currentMargin.left.replace('px', ''),
                                    marginRight: currentMargin.right.replace('px', '')
                                },
                                buttons: [
                                    { type: 'submit', text: 'Save' },
                                    { type: 'cancel', text: 'Close' }
                                ],
                                onSubmit: function (dialog) {
                                    const data = dialog.getData();
                                    const mt = data.marginTop.trim() || '0';
                                    const mb = data.marginBottom.trim() || '0';
                                    const ml = data.marginLeft.trim() || '0';
                                    const mr = data.marginRight.trim() || '0';

                                    if (marginWrapper) {
                                        marginWrapper.style.marginTop = `${mt}px`;
                                        marginWrapper.style.marginBottom = `${mb}px`;
                                        marginWrapper.style.marginLeft = `${ml}px`;
                                        marginWrapper.style.marginRight = `${mr}px`;
                                    } else {
                                        marginWrapper = editor.dom.create('div', {
                                            class: 'm-class'
                                        });

                                        marginWrapper.style.marginTop = `${mt}px`;
                                        marginWrapper.style.marginBottom = `${mb}px`;
                                        marginWrapper.style.marginLeft = `${ml}px`;
                                        marginWrapper.style.marginRight = `${mr}px`;

                                        while (root.firstChild) {
                                            marginWrapper.appendChild(root.firstChild);
                                        }

                                        root.appendChild(marginWrapper);
                                    }

                                    dialog.close();
                                }
                            });
                        }
                    });
                    editor.ui.registry.addMenuItem('titleProp', {
                        text: 'fieldName',
                        onAction: function () {
                            var selectedTr = editor.selection.getNode();
                            if (selectedTr) {
                                var currentCustomData = editor.dom.getAttrib(selectedTr, 'title') || '';
                                editor.windowManager.open({
                                    title: 'Field Name',
                                    body: {
                                        type: 'panel',
                                        items: [
                                            {
                                                type: 'input',
                                                name: 'customData',
                                                label: 'Field Name',
                                            }
                                        ]
                                    },
                                    initialData: {
                                        customData: currentCustomData
                                    },
                                    buttons: [
                                        {
                                            type: 'submit',
                                            text: 'Save'
                                        },
                                        {
                                            type: 'cancel',
                                            text: 'Close'
                                        }
                                    ],
                                    onSubmit: function (dialog) {
                                        var data = dialog.getData();
                                        var newClass = data.customData.trim();
                                        editor.dom.setAttrib(selectedTr, 'title', newClass);
                                        dialog.close();
                                    }
                                });
                            } else {
                                editor.windowManager.alert('Please select a element to set properties.');
                            }
                        }
                    });
                    editor.ui.registry.addMenuItem('stylesProp', {
                        text: 'Styles',
                        onAction: function () {
                            var selectedTr = editor.selection.getNode();
                            if (selectedTr) {
                                var currentCustomData = editor.dom.getAttrib(selectedTr, 'style') || '';
                                editor.windowManager.open({
                                    title: 'Styles',
                                    body: {
                                        type: 'panel',
                                        items: [
                                            {
                                                type: 'textarea',
                                                name: 'customData',
                                                label: 'CSS Styles',
                                                placeholder: 'e.g., color: red; background-color: yellow;'
                                            }
                                        ]
                                    },
                                    initialData: {
                                        customData: currentCustomData // Thiết lập giá trị ban đầu cho input
                                    },
                                    buttons: [
                                        {
                                            type: 'submit',
                                            text: 'Save'
                                        },
                                        {
                                            type: 'cancel',
                                            text: 'Close'
                                        }
                                    ],
                                    onSubmit: function (dialog) {
                                        var data = dialog.getData();
                                        editor.dom.setAttrib(selectedTr, 'style', data.customData);
                                        dialog.close();
                                    }
                                });
                            } else {
                                editor.windowManager.alert('Please select a table to set properties.');
                            }
                        }
                    });
                    editor.on('execCommand', function (e) {
                        if (e.command === 'mceTableMergeCells') {
                            // Lấy các ô được chọn để merge
                            const selectedCells = editor.dom.select('td.mce-selected, th.mce-selected');
                            let mergedContent = '';

                            // Xử lý nội dung các ô, loại bỏ ký tự xuống dòng
                            selectedCells.forEach((cell) => {
                                const content = cell.innerHTML
                                    .replace(/(\r\n|\n|\r|<br\s*\/?>)/g, ' ') // Loại bỏ xuống dòng
                                    .trim(); // Xóa khoảng trắng thừa
                                mergedContent += content ? content + ' ' : '';
                            });

                            // Gán nội dung đã xử lý vào ô đầu tiên
                            if (selectedCells.length) {
                                selectedCells[0].innerHTML = mergedContent.trim();
                                // Xóa nội dung các ô còn lại
                                for (let i = 1; i < selectedCells.length; i++) {
                                    selectedCells[i].innerHTML = '';
                                }
                            }
                        }
                    });
                    editor.ui.registry.addMenuItem('add-background-img', {
                        text: 'Background Image',
                        onAction: function () {
                            editor.windowManager.open({
                                title: 'Upload and Set Background Image',
                                body: {
                                    type: 'panel',
                                    items: [
                                        {
                                            type: 'htmlpanel',
                                            html: `
                                            <input type="file" id="background-img-upload" accept="image/*" style="margin-top: 10px;" />
                                        `
                                        }
                                    ]
                                },
                                buttons: [
                                    {
                                        type: 'submit',
                                        text: 'Apply'
                                    },
                                    {
                                        type: 'cancel',
                                        text: 'Cancel'
                                    }
                                ],
                                onSubmit: function (dialog) {
                                    const fileInput = document.getElementById('background-img-upload');
                                    const file = fileInput.files[0];
                                    if (file) {
                                        const blobInfo = {
                                            blob: () => file
                                        };

                                        self.imageHandler(
                                            blobInfo,
                                            function success(path) {
                                                const tableNode = editor.dom.getParent(editor.selection.getStart(), 'table');
                                                let wrapperDiv = tableNode.parentNode;
                                                if (wrapperDiv.tagName.toLowerCase() !== 'div') {
                                                    wrapperDiv = document.createElement('div');
                                                    tableNode.parentNode.insertBefore(wrapperDiv, tableNode);
                                                    wrapperDiv.appendChild(tableNode);
                                                }
                                                wrapperDiv.classList.add('a4');
                                                wrapperDiv.style.height = '1122px';
                                                wrapperDiv.style.backgroundImage = `url(${path})`;
                                                wrapperDiv.style.backgroundSize = 'cover';
                                                wrapperDiv.style.backgroundRepeat = 'no-repeat';
                                                wrapperDiv.style.backgroundPosition = 'center';
                                                wrapperDiv.style.display = "flex";
                                                dialog.close();
                                            },
                                            function failure(error) {
                                                console.error('Upload failed:', error);
                                                editor.windowManager.alert('Failed to upload image. Please try again.');
                                            }
                                        );
                                    } else {
                                        editor.windowManager.alert('No file selected. Please choose an image to upload.');
                                    }
                                }
                            });
                        }
                    });
                    editor.ui.registry.addMenuItem('gen-table-excel', {
                        text: 'Gen table excel',
                        onAction: function () {
                            const a4WidthPx = 793.7;
                            const a4HeightPx = 1088;
                            const columnWidthPx = 80;
                            const rowHeightPx = 17;
                            const columns = Math.floor(a4WidthPx / columnWidthPx);
                            const rows = Math.floor(a4HeightPx / rowHeightPx);
                            let tableHtml = `<table border="0" style="width:100%; border-collapse:collapse;height:${a4HeightPx}px;max-height:${a4HeightPx}px">`;
                            for (let r = 0; r < rows; r++) {
                                tableHtml += '<tr>';
                                for (let c = 0; c < columns; c++) {
                                    tableHtml += '<td style="width:' + columnWidthPx + 'px; height:' + rowHeightPx + 'px;"><span>&nbsp;</span></td>';
                                }
                                tableHtml += '</tr>';
                            }
                            tableHtml += '</table>';
                            editor.insertContent(tableHtml);
                        }
                    });
                    editor.ui.registry.addMenuItem('Viewpdf', {
                        text: 'View PDF',
                        onAction: function () {
                            var btn = self.editForm.openFrom.childCom.find(x => x.meta.id == self.entity.id);
                            btn.element.click();
                        }
                    });
                    editor.ui.registry.addMenuItem('Viewhistory', {
                        text: 'View History',
                        onAction: function () {
                            self.renderPopup();
                        }
                    });
                }
            }
        }))[0];
    }

    /**@type {HTMLElement} */
    _backdrop;
    /**@type {HTMLElement} */
    bodyElement;
    renderPopup() {
        Html.take(this.tabEditor.element).div.className("backdrop").tabIndex(-1).trigger(EventType.Focus);
        this._backdrop = Html.context;
        Html.instance.div.className("popup-content").div.className("popup-title").span.iText("History change", this.editForm.meta.label);
        this.titleElement = Html.context;
        Html.instance.end.div.className("icon-box").span.className("fa fa-times")
            .event(EventType.Click, () => {
                this._backdrop.remove();
            }).end.end.end.div.className("popup-body").div.className("wrapper scroll-content");
        this.bodyElement = Html.context;
        Html.instance.end.div.className("popup-footer");
        if (this._backdrop.outOfViewport().top) {
            this._backdrop.scrollIntoView(true);
        }
        const res = {
            comId: this.meta.id,
            params: JSON.stringify(Utils.isFunction(this.meta.preQuery, true, this)),
            orderBy: (!this.meta.orderBy ? "ds.insertedDate desc" : this.meta.orderBy),
            Count: false,
            Skip: 0,
            Top: 10,
        };
        Client.instance.submitAsync({
            noQueue: true,
            url: `/api/feature/com`,
            method: "POST",
            jsonData: JSON.stringify(res),
        }).then(data => {
            /**@type {[]} */
            var dataa = data.value;
            dataa.forEach(item => {
                Html.take(this.bodyElement);
                Html.instance.div.label.className("header").text(this.dayjs(item.insertedDate).format("DD/MM/YYYY HH:mm")).end.div.className("diff-container").style("height:250px");
                const modifiedModel = monaco.editor.createModel(
                    item.value ?? ``,
                    this.meta.lang ?? 'javascript'
                );
                const originalModel = monaco.editor.createModel(
                    item.oldValue ?? ``,
                    this.meta.lang ?? 'javascript'
                );
                const diffEditor = monaco.editor.createDiffEditor(
                    Html.context,
                    {
                        originalEditable: true,
                        automaticLayout: true,
                        reareadOnly: true
                    }
                );
                diffEditor.setModel({
                    original: originalModel,
                    modified: modifiedModel,
                });
            });
        });
    }
    /**
     * Handles the image upload process for tinyMCE editor.
     * 
     * This function takes the image selected by the user, uploads it to the server, 
     * and then provides the uploaded image's URL to tinyMCE to be embedded into the editor content.
     *
     * @param {blobInfo} blobInfo - Object containing information about the image blob.
     * @param {Function} success - Callback function to call on a successful upload. Receives the uploaded image URL.
     * @param {Function} failure - Callback function to call on a failed upload. Receives an error message.
     */
    imageHandler(blobInfo, success, failure) {
        const file = blobInfo.blob();
        try {
            const uploader = new Image({ Template: "image/*" });
            return uploader.uploadFile(file).then(path => {
                if (path) {
                    if (success) success(path);
                    return path;
                } else {
                    if (failure) failure('No path returned from upload');
                    return null;
                }
            }).catch(error => {
                console.error('Error during image upload:', error);
                if (failure) failure('Upload failed');
                return null;
            });
        } catch (error) {
            console.error('Error during image upload:', error);
            if (failure) failure('Upload failed');
            return null;
        }
    }

    getValueText() {
        return new dOMParser().parseFromString(this.quill.getContent(), 'text/html').body.textContent;
    }

    updateView(force = false, dirty = null, ...componentNames) {
        this.value = this.entity[this.meta.fieldName] || '';
        if (this.quill) {
            this.quill.setContent(this.value || '');
        }
        if (!this.Dirty) {
            this.originalText = this.value;
            this.oldValue = this.value;
        }
    }
    awaitTime;
    /**
     * @param {boolean} [disabled]
     */
    setDisableUI(disabled) {
        this.awaitTime = window.clearTimeout(this.awaitTime);
        this.awaitTime = window.setTimeout(() => {
            if (!this.quill) {
                return;
            }
            if (disabled) {
                this.quill.readonly = true;
            }
            else {
                this.quill.readonly = false;
            }
        }, 500)
    }

    Dispose() {
        tinymce.remove(this.quill);
        this.quill.remove();
        super.dispose();
    }
}