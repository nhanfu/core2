import { html } from "./utils/html.js";
import { EventType } from "./models/";
import { Client } from "./clients/client.js";
import { Utils } from "./utils/utils.js";
import { Section } from "./section.js";
import { EditableComponent } from "./editableComponent.js";
import { Uuid7 } from "./structs/uuidv7.js";

/**
 * DevTools component for inspecting and modifying components and sections.
 */

export class DevTools extends EditableComponent {
    /**
     * @type {HTMLElement}
     */
    element;

    /**
     * @type {HTMLElement}
     */
    btnGroupConfig;

    /**
     * @typedef {import('./editForm.js').editForm} EditForm
     */
    editForm;

    /**
     * @type {object}
     */
    configEditor = null;

    /**
     * @type {object}
     */
    configSectionEditor = null;

    /**
     * @type {HTMLElement}
     */
    darkOverlay;

    /**
     * @type {number}
     */
    x = 0;

    /**
     * @type {number}
     */
    w = 0;

    /**
     * @type {mouseEvent}
     */
    mouseMoveHandler;

    /**
     * @type {mouseEvent}
     */
    mouseUpHandler;

    /**
     * @type {number}
     */
    _imeout = 0;

    /**
     * @param {EditForm} editForm - the EditForm instance that hosts this DevTools.
     */
    constructor(editForm, group) {
        /** @type {Component} */
        const meta = {
            id: "devtools",
            Label: "DevTools",
            className: "devtools",
            type: "Section",
        };
        super(meta);
        this.editForm = editForm;
    }

    /**
     * opens the DevTools for a specific component group.
     * @param {object} group - the component group to inspect.
     */
    async show(group) {
        html.take(document.body).div.className("popup-config");
        this.element = html.context;
        this.addResizeLines(this.element);
        html.instance.div.className("devtools-header")
            .span.text("DevTools").end
            .span.className("btn").event(EventType.click, () => {
                if (this.configEditor) {
                    this.configEditor.dirty = false;
                    this.configEditor.dispose();
                    this.configEditor = null;
                }
                if (this.configSectionEditor) {
                    this.configSectionEditor.dirty = false;
                    this.configSectionEditor.dispose();
                    this.configSectionEditor = null;
                }
                if (this.element) {
                    this.element.remove();
                }
            }).i.className("fa fa-times").end.end.end;
        html.instance.div.className("devtools-tabs")
            .div.className("devtools-left")
            .div.className("devtools-tab elements2 active").event(EventType.click, () => this.showTabContent("elements")).text("elements").end
            .div.className("devtools-tab console2").event(EventType.click, () => this.showTabContent("console")).text("console").end
            .div.className("devtools-tab sources2").event(EventType.click, () => this.showTabContent("sources")).text("sources").end
            .div.className("devtools-tab network2").event(EventType.click, () => this.showTabContent("network")).text("network").end.end
            .div.className("devtools-right").render();
        this.btnGroupConfig = html.context;
        html.instance.end.end.render();
        html.instance.div.className("devtools-content")
            .div.className("devtools-sidebar components");
        this.sectionComponents = new Section(null, html.context);
        this.sectionComponents.meta = {
            id: group.id,
            column: group.column
        };
        this.addChild(this.sectionComponents);
        html.instance.end.div.className("devtools-main meta-data").end.end;
        html.instance.div.className("console")
            .input.className("console-input").placeHolder("> type javaScript here...").end.render();
        this.calculateSidebarHeight();
        html.take(".components");
        this.renderElements(this.editForm.groupTree, true);
    }

    /**
    * @type {Section}
    */
    sectionComponents;

    components = [
        {
            fieldName: "Button",
            Label: "Button",
            showLabel: true,
            visibility: true,
            className: "btn detail-button primary",
            icon: "fal fa-plus",
            componentType: "Button",
            column: 12
        },
        {
            fieldName: "input",
            Label: "input",
            showLabel: true,
            visibility: true,
            componentType: "input",
            column: 12
        },
        {
            fieldName: "Select",
            Label: "Select",
            query: "[{id:1,name:'test'},{id:2,name:'test2'}]",
            formData: "{name}",
            showLabel: true,
            visibility: true,
            componentType: "Select",
            column: 12
        },
        {
            fieldName: "Textarea",
            Label: "Textarea",
            showLabel: true,
            visibility: true,
            componentType: "Textarea",
            column: 12
        },
        {
            fieldName: "number",
            Label: "number",
            showLabel: true,
            visibility: true,
            componentType: "number",
            column: 12
        },
        {
            fieldName: "Checkbox",
            Label: "Checkbox",
            showLabel: true,
            visibility: true,
            componentType: "Checkbox",
            column: 12
        },
        {
            fieldName: "dropdown",
            Label: "dropdown",
            showLabel: true,
            visibility: true,
            query: "[{id:1,name:'test'},{id:2,name:'test2'}]",
            formData: "{name}",
            template: `[{ "fieldName" : "name", "Label" : "name", "componentType": "input" }]`,
            componentType: "dropdown",
            formData: "{name}",
            column: 12
        },
        {
            fieldName: "Datepicker",
            Label: "Datepicker",
            showLabel: true,
            visibility: true,
            componentType: "Datepicker",
            column: 12
        },
        {
            fieldName: "fileUpload",
            Label: "fileUpload",
            showLabel: true,
            visibility: true,
            componentType: "fileUpload",
            column: 12
        },
        {
            fieldName: "GridView",
            Label: "GridView",
            showLabel: true,
            visibility: true,
            componentType: "GridView",
            column: 12
        },
        {
            fieldName: "word",
            Label: "word",
            showLabel: true,
            visibility: true,
            componentType: "word",
            column: 12
        },
        {
            fieldName: "CodeEditor",
            Label: "CodeEditor",
            showLabel: true,
            visibility: true,
            componentType: "CodeEditor",
            column: 12
        },
        {
            fieldName: "Label",
            Label: "Label",
            showLabel: true,
            visibility: true,
            componentType: "Label",
            column: 12
        },
        {
            fieldName: "Pdf",
            Label: "Pdf",
            showLabel: true,
            visibility: true,
            className: "btn detail-button primary",
            componentType: "Pdf",
            column: 12
        },
        {
            fieldName: "excel",
            Label: "excel",
            showLabel: true,
            visibility: true,
            className: "btn detail-button primary",
            componentType: "excel",
            column: 12
        }
    ];
    updateConfig() {
        for (let index = 0; index < this.components.length; index++) {
            this.components[index].id = Uuid7.newGuid();
            this.components[index].reportTypeId = 1;
            this.components[index].active = true;
        }
        var sectionInfo = {
            components: this.components,
            column: 12,
            isSimple: true,
            isConfig: true,
            className: 'card-body panel group'
        };
        this.componentsEditor = Section.renderSection(this.sectionComponents, sectionInfo);
    }

    /**
     * 
     * @param {Component[]} groupTree 
     */
    renderElements(groupTree) {
        groupTree = groupTree.sort((a, b) => a.order - b.order);
        html.instance.ul.render();
        html.instance.className("devtools-nested active");
        groupTree.forEach(group => {
            html.instance.li.className("devtools-care-li").dataAttr("id", group.id).render();
            if (this.configEditor && this.configEditor.entity.id == group.id) {
                html.instance.className("active");
            }
            if ((group.children && group.children.length > 0) ||
                (group.components && group.components.length > 0)) {
                html.instance.span.className("devtools-caret devtools-caret-down");
                if (this.configSectionEditor && this.configSectionEditor.entity.id == group.id) {
                    html.instance.className("active");
                }
                html.instance.i.event(EventType.click, async (e) => {
                    var ulElement = e.target.closest("span").nextElementSibling;
                    var spanElement = e.target.closest("span");
                    ulElement.classList.toggle('active');
                    spanElement.classList.toggle('devtools-caret-down');
                }).className("fas fa-chevron-right").end.span.className("w-100").event(EventType.click, async (e) => {
                    var spanElement = e.target.closest("span").parentElement;
                    spanElement.classList.toggle('active');
                    await this.updateSectionData(group, e);
                }).text(group.Label || group.fieldName).end.end.render();
            }
            else {
                const iconMap = {
                    Button: "fal fa-plus",
                    input: "fal fa-keyboard",
                    Select: "fal fa-caret-down",
                    Textarea: "fal fa-comment-alt-lines",
                    number: "fal fa-sort-numeric-up-alt",
                    Checkbox: "fal fa-check-square",
                    dropdown: "fal fa-caret-down",
                    Datepicker: "fal fa-calendar-alt",
                    fileUpload: "fal fa-file-upload",
                    Pdf: "fal fa-file-pdf",
                    Image: "fal fa-image",
                    gridView: "fal fa-th-large"
                };
                const icon = iconMap[group.componentType] || "fal fa-text";
                html.instance.event(EventType.click, async (e) => await this.updateMetaData(group, e));
                html.instance.span.i.className(icon).className("mr-1").end.text(group.Label || group.fieldName).end.render();
            }
            if (group.children && group.children.length > 0) {
                this.renderElements(group.children);
            }
            if (group.components && group.components.length > 0) {
                this.renderElements(group.components);
            }
            html.instance.end.render();
        });
        html.instance.end.render();
    }

    reRenderUI() {
        html.take(".components").clear();
        html.instance.ul.className("devtools-tree")
            .li.className("devtools-care-li");
        html.instance.span.className("devtools-caret devtools-caret-down");
        html.instance.i.event(EventType.click, async (e) => {
            var ulElement = e.target.closest("span").nextElementSibling;
            var spanElement = e.target.closest("span");
            ulElement.classList.toggle('active');
            spanElement.classList.toggle('devtools-caret-down');
        }).className("fas fa-chevron-right").end.span.className("w-100").event(EventType.click, async (e) => {
            await this.updateFeatureData(this.meta, e);
        }).text(this.meta.label).end.end.render();
        this.renderElements(this.groupTree);
        html.take(this.element).clear();
        this.editForm.renderTabOrSection(this.groupTree.filter(x => x.active), this);
    }

    configFeatureEditor = null;
    componentsEditor = null;
    /**
     * switch between different DevTools tabs.
     * @param {string} name - the name of the tab to show.
     */
    showTabContent(name) {
        this.element.querySelectorAll(".devtools-tab").forEach(tab => tab.classList.remove("active"));
        switch (name) {
            case "elements":
                this.element.querySelector(".elements2").classList.add("active");
                html.take(".meta-data").clear();
                html.take(".devtools-right").clear();
                html.take(".components").clear();
                this.renderElements(this.editForm.groupTree, true);
                if (this.sectionComponents) {
                    this.sectionComponents.disposeChildren();
                }
                if (this.configFeatureEditor) {
                    this.configFeatureEditor.dispose();
                    this.configFeatureEditor = null;
                }
                if (this.configSectionEditor) {
                    this.configSectionEditor.dispose();
                    this.configSectionEditor = null;
                }
                if (this.configEditor) {
                    this.configEditor.dispose();
                    this.configEditor = null;
                }
                break;
            case "console":
                if (this.sectionComponents) {
                    this.sectionComponents.disposeChildren();
                }
                if (this.configFeatureEditor) {
                    this.configFeatureEditor.dispose();
                    this.configFeatureEditor = null;
                }
                if (this.configSectionEditor) {
                    this.configSectionEditor.dispose();
                    this.configSectionEditor = null;
                }
                if (this.configEditor) {
                    this.configEditor.dispose();
                    this.configEditor = null;
                }
                html.take(".meta-data").clear();
                html.take(".components").clear();
                this.updateConfig();
                break;
            case "sources":
                this.devToolsElement.querySelector(".sources2").classList.add("active");
                html.take(".meta-data").clear();
                html.take(".components").clear();
                break;
            case "network":
                this.devToolsElement.querySelector(".network2").classList.add("active");
                html.take(".meta-data").clear();
                html.take(".components").clear();
                break;
        }
    }

    /**
     * updates metadata for a component in the main panel.
     * @param {Component} group - the component to update.
     * @param {event} e - the event that triggered the update.
     */
    async updateMetaData(group, e) {
        if (this.configSectionEditor) {
            this.configSectionEditor.dispose();
            this.configSectionEditor = null;
        }
        this.element.querySelectorAll(".devtools-care-li").forEach(li => li.classList.remove("active"));
        const selectedLi = e.target.closest(".devtools-care-li");
        if (selectedLi) {
            selectedLi.closest(".devtools-care-li").classList.add("active");
        }
        if (this.configEditor) {
            this.configEditor.entity = group;
            this.configEditor.updateView(true, true);
            return;
        }
        html.take(".devtools-right").clear();
        html.take(".meta-data").div.render();
        this.configEditor = await this.editForm.openPopup("component-editor2", group, true, { btnGroupConfig: this.btnGroupConfig }, html.context);
    }

    /**
     * updates section data in the main panel.
     * @param {Component} group - the section component to update.
     * @param {event} e - the event that triggered the update.
     */
    async updateSectionData(group, e) {
        if (this.configEditor) {
            this.configEditor.dispose();
            this.configEditor = null;
        }
        this.element.querySelectorAll(".devtools-care-li").forEach(li => li.classList.remove("active"));
        if (this.configSectionEditor) {
            this.configSectionEditor.entity = group;
            this.configSectionEditor.updateView(true, true);
            return;
        }
        html.take(".devtools-right").clear();
        html.take(".meta-data").div.render();
        this.configSectionEditor = await this.editForm.openPopup("section-editor2", group, true, { btnGroupConfig: this.btnGroupConfig }, html.context);
    }

    /**
     * adds resize lines to the popup element.
     * @param {HTMLElement} popup - the popup element to make resizable.
     */
    addResizeLines(popup) {
        let isResizing = false;
        let lastMouseY;
        const horizontalLine = document.createElement("div");
        horizontalLine.style.height = "3px";
        horizontalLine.className = "line-resize";
        horizontalLine.style.width = "100%";
        horizontalLine.style.position = "absolute";
        horizontalLine.style.top = "0";
        horizontalLine.style.left = "0";
        horizontalLine.style.cursor = "ns-resize";
        popup.appendChild(horizontalLine);
        horizontalLine.addEventListener("mousedown", (e) => {
            isResizing = true;
            lastMouseY = e.clientY;
            e.preventDefault();
        });
        document.addEventListener("mousemove", (e) => {
            if (!isResizing) return;
            const dy = lastMouseY - e.clientY;
            const newHeight = Math.max(popup.offsetHeight + dy, 200);
            popup.style.height = `${newHeight}px`;
            lastMouseY = e.clientY;
            this.calculateSidebarHeight();
        });
        document.addEventListener("mouseup", () => {
            isResizing = false;
        });
    }

    /**
     * calculates and sets the height for sidebar and content panels.
     */
    calculateSidebarHeight() {
        const headerHeight = this.element.querySelector(".devtools-header")?.offsetHeight || 0;
        const tabsHeight = this.element.querySelector(".devtools-tabs")?.offsetHeight || 0;
        const consoleHeight = this.element.querySelector(".console")?.offsetHeight || 0;

        const popupHeight = this.element.offsetHeight;
        const contentHeight = popupHeight - headerHeight - tabsHeight - consoleHeight;

        // set height for .devtools-sidebar
        const sidebar = this.element.querySelector(".devtools-sidebar");
        if (sidebar) {
            sidebar.style.height = `${contentHeight}px`;
            sidebar.style.overflowY = "auto"; // allow scrolling
        }

        // set height for .devtools-main
        const mainContent = this.element.querySelector(".devtools-main");
        if (mainContent) {
            mainContent.style.height = `${contentHeight}px`;
            mainContent.style.overflowY = "auto"; // allow scrolling
        }
    }

    /**
     * handles mouse movement during resize operations.
     * @param {mouseEvent} mouse - the mouse event.
     * @param {HTMLElement} col - the column element being resized.
     * @param {HTMLElement} resizer - the resizer element.
     */
    mouseMoveHandler(mouse, col, resizer) {
        mouse.preventDefault();
        var dx = mouse.clientX - this.x;
        col.style.width = `${this.w + dx}px`;
        col.style.minWidth = `${this.w + dx}px`;
        col.style.maxWidth = `${this.w + dx}px`;
        this.updateStickyColumns();
    }

    /**
     * handles mouse up event after resizing.
     * @param {mouseEvent} mouse - the mouse event.
     * @param {HTMLElement} col - the column element being resized.
     * @param {HTMLElement} resizer - the resizer element.
     */
    mouseUpHandler(mouse, col, resizer) {
        mouse.preventDefault();
        this.updateHeaders();
        resizer.classList.remove("resizing");
        document.removeEventListener("mousemove", this.mouseMoveHandler);
        document.removeEventListener("mouseup", this.mouseUpHandler);
    }

    /**
     * updates column headers after resizing.
     * @param {boolean} sticky - whether to update sticky columns.
     */
    updateHeaders(sticky) {
        window.clearTimeout(this._imeout);
        this._imeout = window.setTimeout(() => {
            const headerElements = this.headerSection.children.filter(x => x.meta && x.meta.id);
            let index = 0;
            let anyGroup = this.header.some(x => x.groupName && !Utils.isNullOrWhiteSpace(x.groupName));
            if (!anyGroup) {
                headerElements.forEach(header => {
                    header.order = index;
                    header.meta.order = index;
                    index++;
                });
            }
            if (Client.systemRole) {
                const columns = headerElements.map(header => {
                    const match = header.element;
                    if (match && !header.meta.statusBar && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        const dirtyPatch = [
                            { field: "id", value: header.meta.id },
                            { field: "featureId", value: header.meta.featureId },
                            { field: "frozen", value: header.meta.frozen },
                            { field: "frozenRight", value: header.meta.frozen },
                            Utils.isNullOrWhiteSpace(header.groupName) ? { field: "width", value: width } : { field: "width", value: header.meta.width },
                            Utils.isNullOrWhiteSpace(header.groupName) ? { field: "maxWidth", value: width } : { field: "maxWidth", value: header.meta.maxWidth },
                            Utils.isNullOrWhiteSpace(header.groupName) ? { field: "minWidth", value: width } : { field: "minWidth", value: header.meta.minWidth },
                        ];
                        if (!anyGroup) {
                            dirtyPatch.push({ field: "order", value: header.order })
                        }
                        return {
                            changes: dirtyPatch,
                            notMessage: true,
                            table: "Component",
                        };
                    }
                    return null;
                }).filter(x => x != null);
                Client.instance.patchAsync2(columns).then();
            }
            else {
                const columns = headerElements.map(header => {
                    const match = header.element;
                    if (match && !header.meta.statusBar && !Utils.isNullOrWhiteSpace(header.meta.fieldName) && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        return {
                            id: header.meta.id,
                            fieldName: header.meta.fieldName,
                            frozen: header.meta.frozen,
                            order: header.order,
                            width: width,
                        };
                    }
                    return null;
                }).filter(x => x != null);
                var userSetting = new UserSetting();
                userSetting.featureId = this.editForm.meta.label;
                userSetting.componentId = this.meta.id;
                userSetting.active = true;
                userSetting.value = JSON.stringify(columns);
                Client.instance.postAsync(userSetting, "/api/UserSetting").then();
            }
            if (sticky) {
                this.updateStickyColumns();
            }
        }, 500);
    }

    /**
     * placeholder for updating sticky columns method.
     * this would be implemented based on the specific requirements.
     */
    updateStickyColumns() {
        // implementation depends on the specific grid/table structure
    }
}