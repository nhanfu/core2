import { Html } from "./utils/html.js";
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
    Element;

    /**
     * @type {HTMLElement}
     */
    BtnGroupConfig;

    /**
     * @typedef {import('./editForm.js').EditForm} EditForm
     */
    EditForm;

    /**
     * @type {Object}
     */
    ConfigEditor = null;

    /**
     * @type {Object}
     */
    ConfigSectionEditor = null;

    /**
     * @type {HTMLElement}
     */
    DarkOverlay;

    /**
     * @type {number}
     */
    x = 0;

    /**
     * @type {number}
     */
    w = 0;

    /**
     * @type {MouseEvent}
     */
    mouseMoveHandler;

    /**
     * @type {MouseEvent}
     */
    mouseUpHandler;

    /**
     * @type {number}
     */
    _imeout = 0;

    /**
     * @param {EditForm} editForm - The EditForm instance that hosts this DevTools.
     */
    constructor(editForm, group) {
        /** @type {Component} */
        const meta = {
            Id: "devtools",
            Label: "DevTools",
            ClassName: "devtools",
            Type: "Section",
        };
        super(meta);
        this.EditForm = editForm;
    }

    /**
     * Opens the DevTools for a specific component group.
     * @param {object} group - The component group to inspect.
     */
    async show(group) {
        Html.take(document.body).div.className("popup-config");
        this.Element = Html.Context;
        this.AddResizeLines(this.Element);
        Html.Instance.div.className("devtools-header")
            .span.text("DevTools").end
            .span.className("btn").event(EventType.Click, () => {
                if (this.ConfigEditor) {
                    this.ConfigEditor.Dirty = false;
                    this.ConfigEditor.Dispose();
                    this.ConfigEditor = null;
                }
                if (this.ConfigSectionEditor) {
                    this.ConfigSectionEditor.Dirty = false;
                    this.ConfigSectionEditor.Dispose();
                    this.ConfigSectionEditor = null;
                }
                if (this.Element) {
                    this.Element.remove();
                }
            }).i.className("fa fa-times").end.end.end;
        Html.Instance.div.className("devtools-tabs")
            .div.className("devtools-left")
            .div.className("devtools-tab elements2 active").event(EventType.Click, () => this.showTabContent("Elements")).text("Elements").end
            .div.className("devtools-tab console2").event(EventType.Click, () => this.showTabContent("Console")).text("Console").end
            .div.className("devtools-tab sources2").event(EventType.Click, () => this.showTabContent("Sources")).text("Sources").end
            .div.className("devtools-tab network2").event(EventType.Click, () => this.showTabContent("Network")).text("Network").end.end
            .div.className("devtools-right").render();
        this.BtnGroupConfig = Html.Context;
        Html.Instance.end.end.render();
        Html.Instance.div.className("devtools-content")
            .div.className("devtools-sidebar components");
        this.SectionComponents = new Section(null, Html.Context);
        this.SectionComponents.Meta = {
            Id: group.Id,
            Column: group.Column
        };
        this.AddChild(this.SectionComponents);
        Html.Instance.end.div.className("devtools-main meta-data").end.end;
        Html.Instance.div.className("console")
            .input.className("console-input").placeHolder("> Type JavaScript here...").end.render();
        this.calculateSidebarHeight();
        Html.take(".components");
        this.RenderElements(this.EditForm.GroupTree, true);
    }

    /**
    * @type {Section}
    */
    SectionComponents;

    components = [
        {
            FieldName: "Button",
            Label: "Button",
            ShowLabel: true,
            Visibility: true,
            ClassName: "btn detail-button primary",
            Icon: "fal fa-plus",
            ComponentType: "Button",
            Column: 12
        },
        {
            FieldName: "Input",
            Label: "Input",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Input",
            Column: 12
        },
        {
            FieldName: "Select",
            Label: "Select",
            Query: "[{Id:1,Name:'Test'},{Id:2,Name:'Test2'}]",
            FormData: "{Name}",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Select",
            Column: 12
        },
        {
            FieldName: "Textarea",
            Label: "Textarea",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Textarea",
            Column: 12
        },
        {
            FieldName: "Number",
            Label: "Number",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Number",
            Column: 12
        },
        {
            FieldName: "Checkbox",
            Label: "Checkbox",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Checkbox",
            Column: 12
        },
        {
            FieldName: "Dropdown",
            Label: "Dropdown",
            ShowLabel: true,
            Visibility: true,
            Query: "[{Id:1,Name:'Test'},{Id:2,Name:'Test2'}]",
            FormData: "{Name}",
            Template: `[{ "FieldName" : "Name", "Label" : "Name", "ComponentType" : "Input" }]`,
            ComponentType: "Dropdown",
            FormData: "{Name}",
            Column: 12
        },
        {
            FieldName: "Datepicker",
            Label: "Datepicker",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Datepicker",
            Column: 12
        },
        {
            FieldName: "FileUpload",
            Label: "FileUpload",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "FileUpload",
            Column: 12
        },
        {
            FieldName: "GridView",
            Label: "GridView",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "GridView",
            Column: 12
        },
        {
            FieldName: "Word",
            Label: "Word",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Word",
            Column: 12
        },
        {
            FieldName: "CodeEditor",
            Label: "CodeEditor",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "CodeEditor",
            Column: 12
        },
        {
            FieldName: "Label",
            Label: "Label",
            ShowLabel: true,
            Visibility: true,
            ComponentType: "Label",
            Column: 12
        },
        {
            FieldName: "Pdf",
            Label: "Pdf",
            ShowLabel: true,
            Visibility: true,
            ClassName: "btn detail-button primary",
            ComponentType: "Pdf",
            Column: 12
        },
        {
            FieldName: "Excel",
            Label: "Excel",
            ShowLabel: true,
            Visibility: true,
            ClassName: "btn detail-button primary",
            ComponentType: "Excel",
            Column: 12
        }
    ];
    UpdateConfig() {
        for (let index = 0; index < this.components.length; index++) {
            this.components[index].Id = Uuid7.NewGuid();
            this.components[index].ReportTypeId = 1;
            this.components[index].Active = true;
        }
        var sectionInfo = {
            Components: this.components,
            Column: 12,
            IsSimple: true,
            IsConfig: true,
            ClassName: 'card-body panel group'
        };
        this.ComponentsEditor = Section.RenderSection(this.SectionComponents, sectionInfo);
    }

    /**
     * 
     * @param {Component[]} groupTree 
     */
    RenderElements(groupTree) {
        groupTree = groupTree.sort((a, b) => a.Order - b.Order);
        Html.Instance.ul.render();
        Html.Instance.className("devtools-nested active");
        groupTree.forEach(group => {
            Html.Instance.li.className("devtools-care-li").dataAttr("id", group.Id).render();
            if (this.ConfigEditor && this.ConfigEditor.Entity.Id == group.Id) {
                Html.Instance.className("active");
            }
            if ((group.Children && group.Children.length > 0) ||
                (group.Components && group.Components.length > 0)) {
                Html.Instance.span.className("devtools-caret devtools-caret-down");
                if (this.ConfigSectionEditor && this.ConfigSectionEditor.Entity.Id == group.Id) {
                    Html.Instance.className("active");
                }
                Html.Instance.i.event(EventType.Click, async (e) => {
                    var ulElement = e.target.closest("span").nextElementSibling;
                    var spanElement = e.target.closest("span");
                    ulElement.classList.toggle('active');
                    spanElement.classList.toggle('devtools-caret-down');
                }).className("fas fa-chevron-right").end.span.className("w-100").event(EventType.Click, async (e) => {
                    var spanElement = e.target.closest("span").parentElement;
                    spanElement.classList.toggle('active');
                    await this.UpdateSectionData(group, e);
                }).text(group.Label || group.FieldName).end.end.render();
            }
            else {
                const iconMap = {
                    Button: "fal fa-plus",
                    Input: "fal fa-keyboard",
                    Select: "fal fa-caret-down",
                    Textarea: "fal fa-comment-alt-lines",
                    Number: "fal fa-sort-numeric-up-alt",
                    Checkbox: "fal fa-check-square",
                    Dropdown: "fal fa-caret-down",
                    Datepicker: "fal fa-calendar-alt",
                    FileUpload: "fal fa-file-upload",
                    Pdf: "fal fa-file-pdf",
                    Image: "fal fa-image",
                    GridView: "fal fa-th-large"
                };
                const icon = iconMap[group.ComponentType] || "fal fa-text";
                Html.Instance.event(EventType.Click, async (e) => await this.UpdateMetaData(group, e));
                Html.Instance.span.i.className(icon).className("mr-1").end.text(group.Label || group.FieldName).end.render();
            }
            if (group.Children && group.Children.length > 0) {
                this.RenderElements(group.Children);
            }
            if (group.Components && group.Components.length > 0) {
                this.RenderElements(group.Components);
            }
            Html.Instance.end.render();
        });
        Html.Instance.end.render();
    }

    RerenderUI() {
        Html.take(".components").clear();
        Html.Instance.ul.className("devtools-tree")
            .li.className("devtools-care-li");
        Html.Instance.span.className("devtools-caret devtools-caret-down");
        Html.Instance.i.event(EventType.Click, async (e) => {
            var ulElement = e.target.closest("span").nextElementSibling;
            var spanElement = e.target.closest("span");
            ulElement.classList.toggle('active');
            spanElement.classList.toggle('devtools-caret-down');
        }).className("fas fa-chevron-right").end.span.className("w-100").event(EventType.Click, async (e) => {
            await this.UpdateFeatureData(this.Meta, e);
        }).text(this.Meta.Label).end.end.render();
        this.RenderElements(this.GroupTree);
        Html.take(this.Element).clear();
        this.EditForm.RenderTabOrSection(this.GroupTree.filter(x => x.Active), this);
    }

    ConfigFeatureEditor = null;
    ComponentsEditor = null;
    /**
     * Switch between different DevTools tabs.
     * @param {String} name - The name of the tab to show.
     */
    showTabContent(name) {
        this.Element.querySelectorAll(".devtools-tab").forEach(tab => tab.classList.remove("active"));
        switch (name) {
            case "Elements":
                this.Element.querySelector(".elements2").classList.add("active");
                Html.take(".meta-data").clear();
                Html.take(".devtools-right").clear();
                Html.take(".components").clear();
                this.RenderElements(this.EditForm.GroupTree, true);
                if (this.SectionComponents) {
                    this.SectionComponents.DisposeChildren();
                }
                if (this.ConfigFeatureEditor) {
                    this.ConfigFeatureEditor.Dispose();
                    this.ConfigFeatureEditor = null;
                }
                if (this.ConfigSectionEditor) {
                    this.ConfigSectionEditor.Dispose();
                    this.ConfigSectionEditor = null;
                }
                if (this.ConfigEditor) {
                    this.ConfigEditor.Dispose();
                    this.ConfigEditor = null;
                }
                break;
            case "Console":
                if (this.SectionComponents) {
                    this.SectionComponents.DisposeChildren();
                }
                if (this.ConfigFeatureEditor) {
                    this.ConfigFeatureEditor.Dispose();
                    this.ConfigFeatureEditor = null;
                }
                if (this.ConfigSectionEditor) {
                    this.ConfigSectionEditor.Dispose();
                    this.ConfigSectionEditor = null;
                }
                if (this.ConfigEditor) {
                    this.ConfigEditor.Dispose();
                    this.ConfigEditor = null;
                }
                Html.take(".meta-data").clear();
                Html.take(".components").clear();
                this.UpdateConfig();
                break;
            case "Sources":
                this.DevToolsElement.querySelector(".sources2").classList.add("active");
                Html.take(".meta-data").clear();
                Html.take(".components").clear();
                break;
            case "Network":
                this.DevToolsElement.querySelector(".network2").classList.add("active");
                Html.take(".meta-data").clear();
                Html.take(".components").clear();
                break;
        }
    }

    /**
     * Updates metadata for a component in the main panel.
     * @param {Component} group - The component to update.
     * @param {Event} e - The event that triggered the update.
     */
    async UpdateMetaData(group, e) {
        if (this.ConfigSectionEditor) {
            this.ConfigSectionEditor.Dispose();
            this.ConfigSectionEditor = null;
        }
        this.Element.querySelectorAll(".devtools-care-li").forEach(li => li.classList.remove("active"));
        const selectedLi = e.target.closest(".devtools-care-li");
        if (selectedLi) {
            selectedLi.closest(".devtools-care-li").classList.add("active");
        }
        if (this.ConfigEditor) {
            this.ConfigEditor.Entity = group;
            this.ConfigEditor.UpdateView(true, true);
            return;
        }
        Html.take(".devtools-right").clear();
        Html.take(".meta-data").div.render();
        this.ConfigEditor = await this.EditForm.OpenPopup("component-editor2", group, true, { BtnGroupConfig: this.BtnGroupConfig }, Html.Context);
    }

    /**
     * Updates section data in the main panel.
     * @param {Component} group - The section component to update.
     * @param {Event} e - The event that triggered the update.
     */
    async UpdateSectionData(group, e) {
        if (this.ConfigEditor) {
            this.ConfigEditor.Dispose();
            this.ConfigEditor = null;
        }
        this.Element.querySelectorAll(".devtools-care-li").forEach(li => li.classList.remove("active"));
        if (this.ConfigSectionEditor) {
            this.ConfigSectionEditor.Entity = group;
            this.ConfigSectionEditor.UpdateView(true, true);
            return;
        }
        Html.take(".devtools-right").clear();
        Html.take(".meta-data").div.render();
        this.ConfigSectionEditor = await this.EditForm.OpenPopup("section-editor2", group, true, { BtnGroupConfig: this.BtnGroupConfig }, Html.Context);
    }

    /**
     * Adds resize lines to the popup element.
     * @param {HTMLElement} popup - The popup element to make resizable.
     */
    AddResizeLines(popup) {
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
     * Calculates and sets the height for sidebar and content panels.
     */
    calculateSidebarHeight() {
        const headerHeight = this.Element.querySelector(".devtools-header")?.offsetHeight || 0;
        const tabsHeight = this.Element.querySelector(".devtools-tabs")?.offsetHeight || 0;
        const consoleHeight = this.Element.querySelector(".console")?.offsetHeight || 0;

        const popupHeight = this.Element.offsetHeight;
        const contentHeight = popupHeight - headerHeight - tabsHeight - consoleHeight;

        // Set height for .devtools-sidebar
        const sidebar = this.Element.querySelector(".devtools-sidebar");
        if (sidebar) {
            sidebar.style.height = `${contentHeight}px`;
            sidebar.style.overflowY = "auto"; // Allow scrolling
        }

        // Set height for .devtools-main
        const mainContent = this.Element.querySelector(".devtools-main");
        if (mainContent) {
            mainContent.style.height = `${contentHeight}px`;
            mainContent.style.overflowY = "auto"; // Allow scrolling
        }
    }

    /**
     * Handles mouse movement during resize operations.
     * @param {MouseEvent} mouse - The mouse event.
     * @param {HTMLElement} col - The column element being resized.
     * @param {HTMLElement} resizer - The resizer element.
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
     * Handles mouse up event after resizing.
     * @param {MouseEvent} mouse - The mouse event.
     * @param {HTMLElement} col - The column element being resized.
     * @param {HTMLElement} resizer - The resizer element.
     */
    mouseUpHandler(mouse, col, resizer) {
        mouse.preventDefault();
        this.updateHeaders();
        resizer.classList.remove("resizing");
        document.removeEventListener("mousemove", this.mouseMoveHandler);
        document.removeEventListener("mouseup", this.mouseUpHandler);
    }

    /**
     * Updates column headers after resizing.
     * @param {boolean} sticky - Whether to update sticky columns.
     */
    updateHeaders(sticky) {
        window.clearTimeout(this._imeout);
        this._imeout = window.setTimeout(() => {
            const headerElements = this.HeaderSection.Children.filter(x => x.Meta && x.Meta.Id);
            let index = 0;
            let anyGroup = this.Header.some(x => x.GroupName && !Utils.isNullOrWhiteSpace(x.GroupName));
            if (!anyGroup) {
                headerElements.forEach(header => {
                    header.Order = index;
                    header.Meta.Order = index;
                    index++;
                });
            }
            if (Client.systemRole) {
                const columns = headerElements.map(header => {
                    const match = header.Element;
                    if (match && !header.Meta.StatusBar && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        const dirtyPatch = [
                            { Field: "Id", Value: header.Meta.Id },
                            { Field: "FeatureId", Value: header.Meta.FeatureId },
                            { Field: "Frozen", Value: header.Meta.Frozen },
                            { Field: "FrozenRight", Value: header.Meta.Frozen },
                            Utils.isNullOrWhiteSpace(header.GroupName) ? { Field: "Width", Value: width } : { Field: "Width", Value: header.Meta.Width },
                            Utils.isNullOrWhiteSpace(header.GroupName) ? { Field: "MaxWidth", Value: width } : { Field: "MaxWidth", Value: header.Meta.MaxWidth },
                            Utils.isNullOrWhiteSpace(header.GroupName) ? { Field: "MinWidth", Value: width } : { Field: "MinWidth", Value: header.Meta.MinWidth },
                        ];
                        if (!anyGroup) {
                            dirtyPatch.push({ Field: "Order", Value: header.Order })
                        }
                        return {
                            Changes: dirtyPatch,
                            NotMessage: true,
                            Table: "Component",
                        };
                    }
                    return null;
                }).filter(x => x != null);
                Client.instance.patchAsync2(columns).then();
            }
            else {
                const columns = headerElements.map(header => {
                    const match = header.Element;
                    if (match && !header.Meta.StatusBar && !Utils.isNullOrWhiteSpace(header.Meta.FieldName) && Utils.isNullOrWhiteSpace(match.style.display)) {
                        const width = `${match.offsetWidth}px`;
                        return {
                            Id: header.Meta.Id,
                            FieldName: header.Meta.FieldName,
                            Frozen: header.Meta.Frozen,
                            Order: header.Order,
                            Width: width,
                        };
                    }
                    return null;
                }).filter(x => x != null);
                var userSetting = new UserSetting();
                userSetting.FeatureId = this.EditForm.Meta.Label;
                userSetting.ComponentId = this.Meta.Id;
                userSetting.Active = true;
                userSetting.Value = JSON.stringify(columns);
                Client.instance.postAsync(userSetting, "/api/UserSetting").then();
            }
            if (sticky) {
                this.updateStickyColumns();
            }
        }, 500);
    }

    /**
     * Placeholder for updating sticky columns method.
     * This would be implemented based on the specific requirements.
     */
    updateStickyColumns() {
        // Implementation depends on the specific grid/table structure
    }
}