import { ComponentType } from "../models/componentType.js";
import { Utils } from "./utils.js";
import { Component } from "../models/component.js";
import { Client } from "../clients/client.js";
import { PatchVM } from "../models/patch.js";
import { EditForm } from "../editForm.js";
import { TabEditor } from "../tabEditor.js";
import { Html } from "./html.js";
import { Feature } from "../models/feature.js";
import { Textbox } from "../textbox.js";
import { searchMethodEnum } from "../models/enum.js";

export class ComponentExt {
    /**
     * @param {any} com
     * @returns {PatchVM}
     */
    static stepPx = 10;
    static mapToPatch(com, table = null, fields = null) {
        /** @type {PatchVM} */
        // @ts-ignore
        const patch = {
            Table: table,
            Changes: [],
        };
        Utils.forEachProp(com, (prop, val) => {
            if (prop.startsWith("$") || (fields && !fields.includes(prop))) return;
            // @ts-ignore
            patch.Changes.push({
                field: prop,
                value: val?.toString()
            });
        });
        return patch;
    }

    /**
     * @param {Component} component
     * @param {string} searchTerm
     * @param {Textbox} textbox
     */
    static mapToFilterOperator(component, searchTerm, textbox) {
        var rs = '';
        if (Utils.isNullOrWhiteSpace(searchTerm) || !component.fieldName) {
            if (!textbox) {
                return '';
            }
            let fieldName = component.searchFieldName ? `${component.searchFieldName}` : `ds.[${component.fieldName}]`;
            switch (textbox.searchMethod) {
                case searchMethodEnum.empty:
                    rs = `(${fieldName} is null or ${fieldName} = '')`
                    break;
                case searchMethodEnum.filled:
                    rs = `${fieldName} is not null`
                    break;
                default:
                    break
            }
            return rs;
        }
        searchTerm = searchTerm.trim();
        let fieldName = component.searchFieldName ? `${component.searchFieldName}` : `ds.[${component.fieldName}]`;
        let searchParam = component.searchFieldName ? `@${component.searchFieldName.replaceAll(".", "").toLocaleLowerCase()}search` : `@${component.fieldName.toLocaleLowerCase()}search`;
        switch (component.hotKey ? searchMethodEnum.startWith : (textbox ? textbox.searchMethod : searchMethodEnum.contain)) {
            case searchMethodEnum.empty:
                rs = `(${fieldName} is null or ${fieldName} = '')`;
                break;
            case searchMethodEnum.filled:
                rs = `${fieldName} is not null`;
                break;
            case searchMethodEnum.equal:
                if (component.componentType === "Dropdown") {
                    if (Utils.isNullOrWhiteSpace(component.refName)) {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `ds2.[${x}] = ${searchParam}`;
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }
                    else {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `ds2.[${x}] = ${searchParam}`;
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }
                }
                else if (component.componentType === "Datepicker") {
                    rs = `(${fieldName} >= ${searchParam} AND ${fieldName} < DATEADD(day, 1, ${searchParam}))`;
                }
                else {
                    rs = `${fieldName} = ${searchParam}`;
                }
                break;
            case searchMethodEnum.notEqual:
                if (component.componentType === "Dropdown") {
                    if (Utils.isNullOrWhiteSpace(component.refName)) {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `ds2.[${x}] != ${searchParam}`;
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }
                    else {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `ds2.[${x}] != ${searchParam}`;
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }
                }
                else if (component.componentType === "Datepicker") {
                    rs = `(${fieldName} < ${searchParam} OR ${fieldName} >= DATEADD(day, 1, ${searchParam}))`;
                }
                else {
                    rs = `${fieldName} != ${searchParam}`;
                }
                break;
            case searchMethodEnum.contain:
                if (component.componentType === "Dropdown") {
                    if (Utils.isNullOrWhiteSpace(component.refName)) {
                        var datas = JSON.parse(component.Query);
                        var fieldSearch = this.extractStrings(component.formatData)[0];
                        var ids = datas.filter(x => x[fieldSearch].toLocaleLowerCase().includes(searchTerm.toLocaleLowerCase())).map(x => x.id);
                        if (ids && ids.length > 0) {
                            rs = `${fieldName} in ('${ids.join("','")}')`;
                        }
                        else {
                            rs = `${fieldName} = '-1'`;
                        }
                    }
                    else {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `charindex(${searchParam}, ds2.[${x}]) >= 1`
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }
                }
                else if (component.componentType === "Checkbox") {
                    rs = `${fieldName} in (${searchTerm})`;
                }
                else {
                    rs = `charindex(${searchParam}, ${fieldName}) >= 1`;
                }
                break;
            case searchMethodEnum.startWith:
                if (component.componentType === "Dropdown") {
                    if (Utils.isNullOrWhiteSpace(component.refName)) {
                        var datas = JSON.parse(component.Query);
                        var fieldSearch = this.extractStrings(component.formatData)[0];
                        var ids = datas.filter(x => x[fieldSearch].toLocaleLowerCase().includes(searchTerm.toLocaleLowerCase())).map(x => x.id);
                        if (ids && ids.length > 0) {
                            rs = `${fieldName} in ('${ids.join("','")}')`;
                        }
                        else {
                            rs = `${fieldName} = '-1'`;
                        }
                    }
                    else {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `ds2.[${x}] LIKE ${searchParam} + '%'`;
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }
                }
                else {
                    rs = `${fieldName} LIKE ${searchParam} + '%'`;
                }
                break;
            case searchMethodEnum.notContain:
                if (component.componentType === "Dropdown") {
                    if (Utils.isNullOrWhiteSpace(component.refName)) {
                        var datas = JSON.parse(component.Query);
                        var fieldSearch = this.extractStrings(component.formatData)[0];
                        var ids = datas.filter(x => x[fieldSearch].toLocaleLowerCase().includes(searchTerm.toLocaleLowerCase())).map(x => x.id);
                        if (ids && ids.length > 0) {
                            rs = `${fieldName} not in ('${ids.join("','")}')`;
                        }
                        else {
                            rs = `${fieldName} = '-1'`;
                        }
                    }
                    else {
                        var sqlmap = this.extractStrings(component.formatData).map(x => {
                            return `charindex(${searchParam}, ds2.[${x}]) = 0`
                        });
                        rs = `exists (select ds2.id from [${component.refName}] ds2 where ds2.id = ${fieldName} and (${sqlmap.join(" or ")}))`;
                    }

                }
                else {
                    rs = `charindex(${searchParam}, ${fieldName}) = 0`;
                }
                break;
            default:
                break;
        }
        return rs;
    }

    static mapToFilterOperatorValue(component, searchTerm) {
        if (Utils.isNullOrWhiteSpace(searchTerm) || !component.fieldName) {
            return null;
        }
        searchTerm = searchTerm.trim();
        var searchParam = `@${component.fieldName.toLocaleLowerCase()}search`;
        return {
            fieldName: searchParam,
            value: searchTerm
        };
    }
    /**
     * @param {string} inputy
     * 
     * @return {string[]}
     */
    static extractStrings(input) {
        const regex = /\{([^}]+)\}/g;
        const matches = [];
        let match;

        while ((match = regex.exec(input)) !== null) {
            matches.push(match[1]);
        }

        return matches;
    }

    /**
     * @param {featureName} feature
     * @param {boolean | undefined} portal
     */
    static async initFeatureByName(featureName, portal = true) {
        const instance = new TabEditor(featureName);
        instance.Portal = portal;
        instance.render();
        return instance;
    }

    /**
 * Loads a feature by name and optionally by ID, returning a promise that resolves to the feature.
 * 
 * @param {string} name - The name of the feature to load.
 * @param {string} [id=null] - The optional ID of the feature.
 * @returns {Promise<Component>} A promise that resolves to the loaded Feature object or null if not found.
 */
    static loadFeature(name, id = null) {
        return new Promise((resolve, reject) => {
            // @ts-ignore
            const featureTask = Client.instance.submitAsync({
                url: `/api/feature/loadFeature`,
                method: "POST",
                jsonData: JSON.stringify({
                    Name: name
                })
            })
            featureTask.then(ds => {
                resolve(ds);
            }).catch(err => reject(err));
        });
    }

    static loadPublicFeature(name, id = null) {
        return new Promise((resolve, reject) => {
            // @ts-ignore
            const featureTask = Client.instance.submitAsync({
                url: `/api/feature/getPublicFeature?name=` + name,
                isRawString: true,
                method: "GET",
            })
            featureTask.then(ds => {
                resolve(ds);
            }).catch(err => reject(err));
        });
    }


    // Assign methods to an instance based on a feature's script
    static assignMethods(feature, instance) {
        try {
            const scriptFunction = new Function(feature.Script).call(instance);
            Object.assign(instance, scriptFunction);
            if (typeof instance.Init === 'function') {
                /**
                 * @type {Function}
                 */
                const method = instance["Init"];
                if (!method) {
                    return Promise.resolve(false);
                }
                new Promise((resolve, reject) => {
                    let task = method.apply(instance, instance);
                    if (!task || task.isCompleted == null) {
                        resolve(task);
                    } else {
                        task.then(() => resolve(task)).catch(e => reject(e));
                    }
                });
            }
        } catch (error) {
            console.log(error);
        }

    }

    // Find a component that has a specific event handler registered
    static findComponentEvent(component, eventName) {
        let parent = component.parentForm;
        while (parent !== null && !parent[eventName]) {
            parent = parent.parentForm;
        }
        return parent;
    }

    // Modify the visibility of specific fields in a component
    static setShow(component, show, ...fieldNames) {
        component.Children.filter(child => fieldNames.includes(child.Name))
            .forEach(child => child.Show = show);
    }


    /**
    * Detailed rendering logic for the calendar, handling navigation and selection of dates.
    * @param {HTMLElement} element - The date to render in the calendar.
    * @param {HTMLElement} parentEle - The date to render in the calendar.
    */
    // Alter position of HTMLElement relative to parent
    static alterPosition(element, parentEle) {
        if (!element || !element.parentElement || !parentEle) {
            return;
        }
        const containerRect = parentEle.getBoundingClientRect();
        var containerBottom = containerRect.bottom;
        element.style.top = "auto";
        element.style.right = "auto";
        element.style.bottom = "auto";
        element.style.left = "auto";
        Html.take(element).floating(containerBottom, containerRect.left);
        if (this.isOutOfViewport(element).Right) {
            if (!this.isOutOfViewport(element).Bottom) {
                this.bottomCenter(element, parentEle);
            }
            else if (containerRect.Top > element.clientHeight) {
                this.topCenter(element, parentEle);
            }
        }
        if (this.isOutOfViewport(element).Bottom) {
            this.topCenter(element, parentEle);
        }
    }

    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static bottomCenter(element, parent) {
        const containerRect = parent.getBoundingClientRect();
        element.style.right = 'auto';
        element.style.top = containerRect.bottom + 'px';
        this.moveLeft(element);
    }

    static getComputedPx(element, prop) {
        const computedVal = window.getComputedStyle(element)[prop];
        return computedVal ? parseFloat(computedVal.replace('px', '')) || 0 : 0;
    }
    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static topCenter(element, parent) {
        element.style.right = 'auto';
        this.moveLeft(element);
        this.moveTop(element, parent);
    }
    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static moveLeft(element) {
        while (this.isOutOfViewport(element).Right) {
            const left = this.getComputedPx(element, 'left') - this.stepPx;
            element.style.left = left + 'px';
        }
    }
    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static moveTop(element, parent) {
        const parentTop = parent ? parent.getBoundingClientRect().top : null;
        while (this.isOutOfViewport(element).Bottom || (parent && element.getBoundingClientRect().bottom > parentTop)) {
            const top = this.getComputedPx(element, 'top') - (parent ? 1 : this.stepPx);
            element.style.top = top + 'px';
        }
    }
    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static leftMiddle(element, parent) {
        const containerRect = parent.getBoundingClientRect();
        element.style.left = 'auto';
        element.style.bottom = 'auto';
        element.style.right = containerRect.left + 'px';
        this.moveTop(element);
    }
    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static rightMiddle(element, parent) {
        const containerRect = parent.getBoundingClientRect();
        element.style.right = 'auto';
        element.style.bottom = 'auto';
        element.style.left = containerRect.right + 'px';
        this.moveTop(element);
    }
    /**
    * @param {HTMLElement} element 
    * @param {HTMLElement} parent
    */
    static isOutOfViewport(element) {
        const rect = element.getBoundingClientRect();
        return {
            Top: rect.top < 0,
            Right: rect.right > (window.innerWidth || document.documentElement.clientWidth),
            Bottom: rect.bottom > (window.innerHeight || document.documentElement.clientHeight),
            Left: rect.left < 0
        };
    }

    // Download a file using Blob and URL.createObjectURL
    static downloadFile(filename, blob) {
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = URL.createObjectURL(blob);
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }

    // Toggle full screen mode for an element
    static fullScreen(element) {
        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.webkitRequestFullscreen) { /* Safari */
            element.webkitRequestFullscreen();
        } else if (element.msRequestFullscreen) { /* IE11 */
            element.msRequestFullscreen();
        }
    }

    static findClosest(component, Type) {
        if (component instanceof Type) {
            return component;
        }

        while (component.Parent != null) {
            component = component.Parent;
            if (component instanceof Type) {
                return component;
            }
        }
        return null;
    }

    /**
     * @typedef {import("../editableComponent.js").default} EditableComponent
     * @param {EditableComponent} com
     * @param {TabEditor} tab
     */
    static async openTabOrPopup(com, tab) {
        const editablMd = await import('../editableComponent.js');
        const { EditForm } = await import('../editForm.js');
        const { TabEditor } = await import('../tabEditor.js');
        let parentTab;
        if (com instanceof EditForm) {
            parentTab = com;
        } else if (com instanceof editablMd.default) {
            parentTab = EditForm || this.findClosest(com, EditForm);
        }
        if (tab instanceof TabEditor) {
            if (tab.Popup) {
                com.addChild(tab);
            } else {
                tab.render();
            }

            tab.parentForm = parentTab;
            tab.openFrom = parentTab instanceof EditForm && parentTab?.firstOrDefault(x => x.entity === tab.entity);
        }
    }

    /**
     * 
     * @param {EditableComponent} com 
     * @param {TabEditor} tab 
     */
    async openTabOrPopup(com, tab) {
        const { EditForm } = await import('../editForm.js');
        let parentTab;
        if (com instanceof EditForm) {
            parentTab = com;
        } else {
            parentTab = com.editForm || com.findClosest(x => x instanceof EditForm);
        }

        if (tab.Popup) {
            com.addChild(tab);
        } else {
            tab.render();
        }

        tab.parentForm = parentTab;
        tab.openFrom = parentTab?.filterChildren(x => x.entity === tab.entity)?.[0];
    }

    /**
     * @typedef {import('../tabEditor.js').tabEditor} TabEditor
     * @param {EditableComponent} com
     * @param {string} id
     * @param {string} featureName
     * @param {() => TabEditor} factory
     */
    static async openTab(com, id, featureName, factory, popup = false, anonymous = false) {
        const md = await import('../tabEditor.js');
        if (!popup && md.tabEditor.findTab(id)) {
            const exists = md.tabEditor.findTab(id);
            exists.Focus();
            return exists;
        }
        const feature = await this.loadFeature(featureName);
        const tab = factory();
        tab.Popup = popup;
        tab.Name = featureName;
        tab.id = id;
        tab.meta = feature;
        this.assignMethods(feature, tab);
        await this.openTabOrPopup(com, tab);
        return tab;
    }

    /**
     * @param {EditableComponent} com
     * @param {string} featureName
     * @param {{ (): EditableComponent }} factory
     */
    static openPopup(com, featureName, factory, anonymous = false, child = false) {
        const hashCode = () => {
            let hash = 0;
            let str = JSON.stringify(com);
            for (let i = 0; i < str.length; i++) {
                const char = str.charCodeAt(i);
                hash = ((hash << 5) - hash) + char;
                hash |= 0;
            }
            return hash;
        };
        // @ts-ignore
        return this.openTab(com, hashCode().toString(), featureName, factory, true, anonymous);
    };
}
