"use strict";

(function () {
    var getValue = System.Nullable.getValue;
    System.Nullable.getValue = function (obj) {
        if (obj === null) {
            return null;
        }
        if (obj && obj.m_dateTime) {
            return obj.m_dateTime;
        }
        return getValue(obj);
    };
    System.DateTimeOffset.prototype.getUTCFullYear = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getUTCFullYear();
    };
    System.DateTimeOffset.prototype.getFullYear = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getFullYear();
    };
    System.DateTimeOffset.prototype.getYear = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getFullYear();
    };
    System.DateTimeOffset.prototype.getDay = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getDay();
    };
    System.DateTimeOffset.prototype.getUTCMonth = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getUTCMonth();
    };
    System.DateTimeOffset.prototype.getMonth = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getMonth();
    };

    System.DateTimeOffset.prototype.getUTCDate = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getUTCDate();
    };
    System.DateTimeOffset.prototype.getDate = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getDate();
    };

    System.DateTimeOffset.prototype.getTime = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getTime();
    };
    System.DateTimeOffset.prototype.getTimezoneOffset = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.getTimezoneOffset();
    };
    System.DateTimeOffset.prototype.getTicks = function () {
        var date = this instanceof Date ? this : this.m_dateTime;
        return date.ticks;
    };
    System.DateTime.gt = function (a, b) {
        return a.getTime() > b.getTime();
    };
    Date.prototype.ToString$1 = function (format) {
        return System.String.format(`{0:${format}}`, this);
    };
})();

function zoom() {
    const image = document.querySelector(".dark-overlay img");
    let scale = 1;
    const SCALE_FACTOR = 0.1;

    image.addEventListener("mousewheel", function (e) {
        e.preventDefault();
        scale += e.deltaY > 0 ? -SCALE_FACTOR : SCALE_FACTOR;
        scale = Math.min(Math.max(1, scale), 6);
        this.style.transform = `scale(${scale})`;
    });
    image.addEventListener('mousemove', function (e) {
        e.preventDefault();
        const { left, top, width, height } = this.getBoundingClientRect();
        const x = (e.clientX - left) / width * 100;
        const y = (e.clientY - top) / height * 100;
        this.style.transformOrigin = `${x}% ${y}%`;
    });

    image.addEventListener('mouseout', function (e) {
        e.preventDefault();
        this.style.transformOrigin = 'unset';
        this.style.transform = 'scale(1)'
    });
}

function initCodeEditor(com) {
    if (typeof (require) === 'undefined') return;
    require.config({ paths: { 'vs': 'https://unpkg.com/monaco-editor@latest/min/vs' } });
    window.MonacoEnvironment = { getWorkerUrl: () => proxy };

    let proxy = URL.createObjectURL(new Blob([`
	    self.MonacoEnvironment = {
		    baseUrl: 'https://unpkg.com/monaco-editor@latest/min/'
	    };
	    importScripts('https://unpkg.com/monaco-editor@latest/min/vs/base/worker/workerMain.js');`],
        { type: com.Meta?.Template ?? 'text/javascript' }));

    require(["vs/editor/editor.main"], function () {
        let editor = monaco.editor.create(com.Element, {
            value: com.Entity[com.Meta.FieldName],
            language: com.Meta.Lang ?? 'javascript',
            theme: 'vs-light',
            automaticLayout: true,
            minimap: {
                enabled: false,
            }
        });
        // hook event from UI
        editor.getModel().onDidChangeContent(() => {
            com.Entity[com.Meta.FieldName] = editor.getValue();
            com.Dirty = true;
        });
    });
    com.Element.style.resize = 'both';
    com.Element.style.border = '1px solid #dde';
    // register change event from UI
    com.addEventListener('UpdateView', () => {
        editor.setValue(com.Entity[com.Meta.FieldName]);
    });
}

function initCkEditor(com) {
    if (com == null || typeof (CKEDITOR) === 'undefined') return;
    CKEDITOR.ClassicEditor.create(com.Element, {
        // https://ckeditor.com/docs/ckeditor5/latest/features/toolbar/toolbar.html#extended-toolbar-configuration-format
        toolbar: {
            items: [
                'exportPDF', 'exportWord', '|',
                'findAndReplace', 'selectAll', '|',
                'heading', '|',
                'bold', 'italic', 'strikethrough', 'underline', 'code', 'subscript', 'superscript', 'removeFormat', '|',
                'bulletedList', 'numberedList', 'todoList', '|',
                'outdent', 'indent', '|',
                'undo', 'redo',
                '-',
                'fontSize', 'fontFamily', 'fontColor', 'fontBackgroundColor', 'highlight', '|',
                'alignment', '|',
                'link', 'uploadImage', 'blockQuote', 'insertTable', 'mediaEmbed', 'codeBlock', 'htmlEmbed', '|',
                'specialCharacters', 'horizontalLine', 'pageBreak', '|',
                'textPartLanguage', '|',
                'sourceEditing'
            ],
            shouldNotGroupWhenFull: true
        },
        // Changing the language of the interface requires loading the language file using the <script> tag.
        // language: 'es',
        list: {
            properties: {
                styles: true,
                startIndex: true,
                reversed: true
            }
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/headings.html#configuration
        heading: {
            options: [
                { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
                { model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
                { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
                { model: 'heading4', view: 'h4', title: 'Heading 4', class: 'ck-heading_heading4' },
                { model: 'heading5', view: 'h5', title: 'Heading 5', class: 'ck-heading_heading5' },
                { model: 'heading6', view: 'h6', title: 'Heading 6', class: 'ck-heading_heading6' }
            ]
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/editor-placeholder.html#using-the-editor-configuration
        placeholder: 'Welcome to CKEditor 5!',
        // https://ckeditor.com/docs/ckeditor5/latest/features/font.html#configuring-the-font-family-feature
        fontFamily: {
            options: [
                'default',
                'Arial, Helvetica, sans-serif',
                'Courier New, Courier, monospace',
                'Georgia, serif',
                'Lucida Sans Unicode, Lucida Grande, sans-serif',
                'Tahoma, Geneva, sans-serif',
                'Times New Roman, Times, serif',
                'Trebuchet MS, Helvetica, sans-serif',
                'Verdana, Geneva, sans-serif'
            ],
            supportAllValues: true
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/font.html#configuring-the-font-size-feature
        fontSize: {
            options: [10, 12, 14, 'default', 18, 20, 22],
            supportAllValues: true
        },
        // Be careful with the setting below. It instructs CKEditor to accept ALL HTML markup.
        // https://ckeditor.com/docs/ckeditor5/latest/features/general-html-support.html#enabling-all-html-features
        htmlSupport: {
            allow: [
                {
                    name: /.*/,
                    attributes: true,
                    classes: true,
                    styles: true
                }
            ]
        },
        // Be careful with enabling previews
        // https://ckeditor.com/docs/ckeditor5/latest/features/html-embed.html#content-previews
        htmlEmbed: {
            showPreviews: true
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/link.html#custom-link-attributes-decorators
        link: {
            decorators: {
                addTargetToExternalLinks: true,
                defaultProtocol: 'https://',
                toggleDownloadable: {
                    mode: 'manual',
                    label: 'Downloadable',
                    attributes: {
                        download: 'file'
                    }
                }
            }
        },
        // https://ckeditor.com/docs/ckeditor5/latest/features/mentions.html#configuration
        mention: {
            feeds: [
                {
                    marker: '@',
                    feed: [
                        '@apple', '@bears', '@brownie', '@cake', '@cake', '@candy', '@canes', '@chocolate', '@cookie', '@cotton', '@cream',
                        '@cupcake', '@danish', '@donut', '@dragée', '@fruitcake', '@gingerbread', '@gummi', '@ice', '@jelly-o',
                        '@liquorice', '@macaroon', '@marzipan', '@oat', '@pie', '@plum', '@pudding', '@sesame', '@snaps', '@soufflé',
                        '@sugar', '@sweet', '@topping', '@wafer'
                    ],
                    minimumCharacters: 1
                }
            ]
        },
        // The "superbuild" contains more premium features that require additional configuration, disable them below.
        // Do not turn them on unless you read the documentation and know how to configure them and setup the editor.
        removePlugins: [
            // These two are commercial, but you can try them out without registering to a trial.
            // 'ExportPdf',
            // 'ExportWord',
            'AIAssistant',
            'CKBox',
            'CKFinder',
            'EasyImage',
            // This sample uses the Base64UploadAdapter to handle image uploads as it requires no configuration.
            // https://ckeditor.com/docs/ckeditor5/latest/features/images/image-upload/base64-upload-adapter.html
            // Storing images as Base64 is usually a very bad idea.
            // Replace it on production website with other solutions:
            // https://ckeditor.com/docs/ckeditor5/latest/features/images/image-upload/image-upload.html
            // 'Base64UploadAdapter',
            'RealTimeCollaborativeComments',
            'RealTimeCollaborativeTrackChanges',
            'RealTimeCollaborativeRevisionHistory',
            'PresenceList',
            'Comments',
            'TrackChanges',
            'TrackChangesData',
            'RevisionHistory',
            'Pagination',
            'WProofreader',
            // Careful, with the Mathtype plugin CKEditor will not load when loading this sample
            // from a local file system (file://) - load this site via HTTP server if you enable MathType.
            'MathType',
            // The following features are part of the Productivity Pack and require additional license.
            'SlashCommand',
            'Template',
            'DocumentOutline',
            'FormatPainter',
            'TableOfContents',
            'PasteFromOfficeEnhanced',
            'CaseChange'
        ]
    }).then(editor => {
        editor.setData(com.Entity == null ? '' : (com.Entity[com.FieldName] ?? ''));
        editor.model.document.on('change:data', () => {
            com.Entity[com.FieldName] = editor.getData();
            com.Dirty = true;
        });
        editor.plugins.get('FileRepository').createUploadAdapter = function (loader) {
            return new MyUploadAdapter(loader, editor);
        };
        com.addEventListener('UpdateView', () => {
            editor.setHtml(com.Entity == null ? null : com.Entity[com.FieldName]);
        });
    });
}

class MyUploadAdapter {
    constructor(loader, editor) {
        this.loader = loader;
        this.editor = editor;
    }

    upload() {
        return this.loader.file
            .then(file => new Promise((resolve, reject) => {
                const reader = new FileReader();

                reader.onload = () => {
                    this._sendRequest(reader.result, file, resolve, reject);
                };

                reader.readAsDataURL(file);
            }));
    };

    _sendRequest(base64, file, resolve, reject) {
        var $step = 0,
            $task1,
            $taskResult1,
            $jumpFromFinally,
            uploader,
            path,
            content,
            $asyncBody = Bridge.fn.bind(this, function () {
                for (; ;) {
                    $step = System.Array.min([0, 1], $step);
                    switch ($step) {
                        case 0: {
                            uploader = new Core.Components.ImageUploader(new Core.Models.Component());
                            $task1 = uploader.UploadBase64Image(Bridge.toString(base64), file.name);
                            $step = 1;
                            if ($task1.isCompleted()) {
                                continue;
                            }
                            $task1.continue($asyncBody);
                            return;
                        }
                        case 1: {
                            $taskResult1 = $task1.getAwaitedResult();
                            path = $taskResult1;
                            resolve({
                                default: path
                            });
                            return;
                        }
                        default: {
                            return;
                        }
                    }
                }
            }, arguments);

        $asyncBody();
    }
}