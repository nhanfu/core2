import { EditableComponent } from "./editableComponent.js";
import { Component } from "./models/component.js";
import { ComponentExt } from "./utils/componentExt.js";
import { Str } from "./utils/ext.js";
import { html } from "./utils/html.js";
import { Utils } from "./utils/utils.js";
import { Client } from "./clients/client.js";
import EventType from "./models/eventType.js";

/**
 * represents a code editor component.
 */
export class CodeEditor extends EditableComponent {
    /**
     * creates an instance of a CodeEditor.
     * @param {Component} ui - the uI component.
     * @param {HTMLElement} [ele=null] - the HTML element associated with the editor.
     */
    constructor(ui, ele = null) {
        super(ui);
        this.element = ele || null;
        this.defaultValue = '';
        this.editor = null;
    }

    setOldTextAndVal() {
        this.originalText = this.entity[this.name] || "";
        this.oldValue = this.originalText;
    }

    /**
     * renders the code editor.
     */
    render() {
        this.setOldTextAndVal();
        if (!this.element) {
            this.parentElement.style.textAlign = 'unset';
            html.take(this.parentElement).div.className("code-editor").style(this.meta.style || "height:150px;max-height:150px;position: relative;");
            this.element = html.context;
        }
        this.config().then(() => {
            if (typeof (require) === 'undefined') return;
            // @ts-ignore
            require(["vs/editor/editor.main"], this.editorLoaded.bind(this));
        });
    }

    static _hasConfig;
    async config() {
        if (typeof (require) === 'undefined') return;
        if (CodeEditor._hasConfig) return;
        CodeEditor._hasConfig = true;
        // @ts-ignore
        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs' } });
        // @ts-ignore
        window.monacoEnvironment = { getWorkerurl: () => proxy };

        let proxy = uRL.createObjectURL(new blob([`
            self.monacoEnvironment = {
                baseurl: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/'
            };
            importScripts('https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.52.2/min/vs/base/worker/workerMain.js');
            `], { type: 'text/javascript' }));
    }

    getValueText() {
        return this.editor.getValue();
    }

    editorLoaded() {
        monaco.languages.register({ id: 'javascript' });
        const jsKeywords = [
            'break', 'case', 'catch', 'class', 'const', 'continue', 'debugger', 'default', 'delete',
            'do', 'else', 'export', 'extends', 'finally', 'for', 'null', 'function', 'if', 'import', 'in',
            'instanceof', 'new', 'return', 'super', 'switch', 'this', 'throw', 'try', 'typeof', 'var',
            'void', 'while', 'with', 'yield', 'let', 'enum', 'await', 'async', 'static', 'true', 'false'
        ];
        monaco.languages.setMonarchTokensProvider('javascript', {
            keywords: jsKeywords,
            tokenizer: {
                root: [
                    [/[a-zA-z_$][\w$]*/, {
                        cases: {
                            '@keywords': 'keyword',
                            '@default': 'identifier'
                        }
                    }],
                    [/(\.editForm\.)/, 'custom-editform'],
                    [/(\.filter\()/, 'custom-filter'],
                    [/(\.find\()/, 'custom-find'],
                    [/(\.firstCom\()/, 'custom-firstcom'],
                    [/(\.loadMasterData\()/, 'custom-loadmasterdata'],
                    [/(\.clearSelected\()/, 'custom-loadmasterdata'],
                    [/(\.forEach\()/, 'custom-reduce'],
                    [/(\.reduce\()/, 'custom-reduce'],
                    [/(\.openConfig\()/, 'custom-openconfig'],
                    [/(\.item)/, 'custom-item'],
                    [/(\.element)/, 'custom-item'],
                    [/(\.length)/, 'custom-length'],
                    [/(\.updateView2\()/, 'custom-updateview2'],
                    [/(\.updateView\()/, 'custom-updateview2'],
                    [/(\.decimal\()/, 'custom-decimal'],
                    [/(\.times\()/, 'custom-times'],
                    [/(\.isNegative\()/, 'custom-isnegative'],
                    [/(\.abs\()/, 'custom-abs'],
                    [/(\.plus\()/, 'custom-plus'],
                    [/(\.div\()/, 'custom-div'],
                    [/(\.parent)/, 'custom-parent'],
                    // default javaScript tokens
                    [/[{}[\]()]/, '@brackets'],
                    [/\/\/.*$/, 'comment'],
                    [/"([^"\\]|\\.)*$/, 'string.invalid'], // non-terminated string
                    [/"([^"\\]|\\.)*"/, 'string'],
                    [/'([^'\\]|\\.)*$/, 'string.invalid'], // non-terminated string
                    [/'([^'\\]|\\.)*'/, 'string'],
                    [/`([^`\\]|\\.)*$/, 'string.invalid'], // non-terminated template
                    [/`([^`\\]|\\.)*`/, 'string'],
                    [/[+-/*=<>!~&|%]+/, 'operator']
                ]
            }
        });
        monaco.editor.defineTheme('light-soft', {
            base: 'vs', // Nền sáng
            inherit: true, // Kế thừa các thiết lập mặc định
            rules: [
                // quy tắc màu cho các token tùy chỉnh
                { token: 'keyword', foreground: '0000FF' }, // xanh dương đậm
                { token: 'identifier', foreground: '1E1E1E' }, // Đen đậm
                { token: 'custom-editform', foreground: 'a31515' }, // Đỏ sẫm
                { token: 'custom-filter', foreground: '795E26' }, // Nâu vàng
                { token: 'custom-find', foreground: '5A5A5A' }, // Xám đậm
                { token: 'custom-firstcom', foreground: '4B8BBE' }, // xanh lam nhẹ
                { token: 'custom-loadmasterdata', foreground: 'b4009E' }, // Tím đậm
                { token: 'custom-reduce', foreground: '00008B' }, // xanh dương sẫm
                { token: 'custom-openconfig', foreground: '2B91AF' }, // xanh lam sáng
                { token: 'custom-item', foreground: '9932CC' }, // Tím nhạt
                { token: 'custom-length', foreground: '6A8759' }, // xanh lá
                { token: 'custom-updateview2', foreground: '007ACC' }, // xanh visual studio
                { token: 'custom-decimal', foreground: 'c75C6A' }, // Đỏ hồng
                { token: 'custom-times', foreground: 'fF4500' }, // cam đỏ
                { token: 'custom-isnegative', foreground: '9B59B6' }, // Tím sáng
                { token: 'custom-abs', foreground: '2ECC71' }, // xanh lá sáng
                { token: 'custom-plus', foreground: 'fF6347' }, // Đỏ cam nhạt
                { token: 'custom-div', foreground: '4682B4' }, // xanh thép
                { token: 'custom-parent', foreground: '1ABC9C' }, // xanh ngọc sáng

                // quy tắc màu mặc định
                { token: 'comment', foreground: '008000', fontStyle: 'italic' }, // xanh lá đậm
                { token: 'string', foreground: 'a31515' }, // Đỏ sẫm
                { token: 'operator', foreground: '000000' }, // Đen
                { token: 'number', foreground: '098658' }, // xanh lục đậm
                { token: 'delimiter', foreground: '1E1E1E' }, // Đen xám
                { token: 'brackets', foreground: '1E1E1E' }, // Đen xám
            ],
            colors: {
                'editor.foreground': '#333333', // Màu chữ chung - Đen xám
                'editor.background': '#fFFFFF', // Nền trắng
                'editorLineNumber.foreground': '#5A5A5A', // Số dòng - Xám đậm
                'editorCursor.foreground': '#007ACC', // con trỏ - xanh lam visual studio
                'editor.selectionBackground': '#aDD6FF', // Nền vùng chọn - xanh nhạt
                'editor.inactiveSelectionBackground': '#e5EBF1', // Nền vùng chọn không hoạt động - Xám xanh
            }
        });
        this.editor = monaco.editor.create(this.element, {
            value: this.fieldVal ?? Str.empty,
            language: this.meta.lang ?? 'javascript',
            theme: this.meta.theme ?? 'light-soft',
            automaticLayout: true,
            foldingStrategy: "indentation",
            wordWrap: "on",
            folding: true,
            minimap: {
                enabled: false,
            }
        });
        if (this.meta.lang ?? 'javascript' == "javascript") {
            if (this.editor.getModel()) {
                this.editor.getAction("editor.foldLevel2").run();
            } else {
                monaco.editor.onDidCreateModel(() => {
                    this.editor.getAction("editor.foldLevel2").run();
                });
            }
        }
        window.addEventListener('resize', this.resizeHandler.bind(this));
        this.editor.getModel().onDidChangeContent(() => {
            const currentValue = this.editor.getValue();
            if (currentValue !== this.originalText) {
                this.fieldVal = currentValue;
                this.dirty = true;
            }
        });
        this.editor.onContextMenu(function (e) {
            e.event.preventDefault();
            e.event.stopPropagation();
        });
        this.element.classList.add('code-editor');
        this.element.style.resize = 'both';
        this.element.style.border = '1px solid #dde';
        html.take(this.element).icon('fal fal fa-compress-wide').style("position: absolute; z-index: 1; top: 0; right: 0;")
            .event('click', () => {
                ComponentExt.fullScreen(this.element);
            }).end
            .select.event("change", /**@param {event} e */(e) => {
                var newLanguage = e.target.value;
                monaco.editor.setModelLanguage(this.editor.getModel(), newLanguage);
            }).style("position: absolute; z-index: 1; left: 0; bottom: 0;")
            .option.attr("value", "javascript").attr(this.meta.lang == "javascript" ? "selected" : "no", "").iText("javascript").end
            .option.attr("value", "json").attr(this.meta.lang == "json" ? "selected" : "no", "").iText("json").end
            .option.attr("value", "sql").attr(this.meta.lang == "sql" ? "selected" : "no", "").iText("sql").end
            .option.attr("value", "html").attr(this.meta.lang == "html" ? "selected" : "no", "").iText("html").end
            .option.attr("value", "css").attr(this.meta.lang == "css" ? "selected" : "no", "").iText("css").end
            .option.attr("value", "text").attr(this.meta.lang == "text" ? "selected" : "no", "").iText("text").end.end
            .icon('fal fa-history').style("position: absolute; z-index: 1; bottom: 0; right: 0;")
            .event('click', () => {
                this.renderPopup();
            }).event('contextmenu', (e) => this.editForm.sysConfigMenu(e, this.meta, null, null));
    }
    time;

    resizeHandler() {
        window.clearTimeout(this.time);
        this.time = window.setTimeout(() => {
            const minWidth = this.parent.element.clientWidth - 30; // adjust as needed
            this.element.style.width = minWidth + 'px';
        }, 200);
    }
    /**@type {HTMLElement} */
    _backdrop;
    /**@type {HTMLElement} */
    bodyElement;
    renderPopup() {
        html.take(this.editForm.element).div.className("backdrop");
        this._backdrop = html.context;
        html.instance.div.className("popup-content").div.className("popup-title").span.iText("history change");
        this.titleElement = html.context;
        html.instance.end.div.className("icon-box").span.className("fa fa-times")
            .event(EventType.click, () => {
                this._backdrop.remove();
            }).end.end.end.div.className("popup-body").div.className("wrapper scroll-content");
        this.bodyElement = html.context;
        html.instance.end.div.className("popup-footer");
        if (this._backdrop.outOfViewport().top) {
            this._backdrop.scrollIntoView(true);
        }
        const res = {
            comId: this.meta.id,
            params: JSON.stringify(Utils.isFunction(this.meta.preQuery, true, this)),
            orderBy: (!this.meta.orderBy ? "ds.insertedDate desc" : this.meta.orderBy),
            count: false,
            skip: 0,
            top: 110,
        };
        Client.instance.submitAsync({
            noQueue: true,
            url: `/api/feature/com`,
            method: "pOST",
            jsonData: JSON.stringify(res),
        }).then(data => {
            /**@type {[]} */
            var dataa = data.value;
            if (!dataa) {
                return;
            }
            dataa.forEach(item => {
                html.take(this.bodyElement);
                html.instance.div.label.className("header").text(this.dayjs(item.insertedDate).format("DD/MM/YYYY hH:mm")).end.div.className("diff-container").style("height:250px");
                const modifiedModel = monaco.editor.createModel(
                    item.value ?? ``,
                    this.meta.lang ?? 'javascript'
                );
                const originalModel = monaco.editor.createModel(
                    item.oldValue ?? ``,
                    this.meta.lang ?? 'javascript'
                );
                const diffEditor = monaco.editor.createDiffEditor(
                    html.context,
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
     * updates the view of the Checkbox based on the current state.
     * @param {boolean} [force=false] - force the update regardless of changes.
     * @param {?boolean} [dirty=null] - the new dirty state.
     * @param {...string} componentNames - additional component names to update.
     */
    updateView(force = false, dirty = null, ...componentNames) {
        this.value = this.entity[this.meta.fieldName];
        if (!this.dirty) {
            this.originalText = this.value || "";
            this.oldValue = this.value;
        }
        this.editor.setValue(this.value || "");
        if (this.meta.lang ?? 'javascript' == "javascript") {
            if (this.editor.getModel()) {
                this.editor.getAction("editor.foldLevel2").run();
            } else {
                monaco.editor.onDidCreateModel(() => {
                    this.editor.getAction("editor.foldLevel2").run();
                });
            }
        }
    }
}
