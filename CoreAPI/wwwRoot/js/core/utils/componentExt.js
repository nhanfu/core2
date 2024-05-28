import { ComponentType } from "../models/componentType.js";
import { Utils } from "./utils.js";
import { Component } from "../models/component.js";
import { Client } from "../clients/client.js";
import { PatchVM } from "../models/patch.js";

export class ComponentExt {
    /**
     * @param {any} com
     * @returns {PatchVM}
     */
    static MapToPatch(com, table = null, fields = null) {
        /** @type {PatchVM} */
        // @ts-ignore
        const patch = {
            Table: table,
            Changes: [],
        };
        Utils.ForEachProp(com, (prop, val) => {
            if (prop.startsWith("$") || (fields && !fields.includes(prop))) return;
            // @ts-ignore
            patch.Changes.push({
                Field: prop,
                Value: val?.toString()
            });
        });
        return patch;
    }

    /**
     * @param {Component} component
     * @param {string} searchTerm
     */
    static MapToFilterOperator(component, searchTerm) {
        if (!searchTerm || !component.FieldName) {
            return '';
        }

        searchTerm = searchTerm.trim();
        let fieldName = component.DisplayField ? `JSON_VALUE(ds.[${component.DisplayField}], '$.${component.DisplayDetail}')` : `ds.[${component.FieldName}]`;
        if (!fieldName) return '';

        if (component.ComponentType === ComponentType.Datepicker) {
            let datetime = Date.parse(searchTerm);
            if (!Number.isNaN(datetime)) {
                let date = new Date(datetime);
                const dateStr = date.toISOString();
                return `cast(${fieldName} as date) = cast('${dateStr}' as date)`;
            }
            return '';
        } else if (component.ComponentType === ComponentType.Checkbox) {
            const val = Boolean(searchTerm);
            return `${fieldName} = ${val}`;
        } else if (component.ComponentType === ComponentType.Numbox) {
            const searchNumber = parseFloat(searchTerm);
            if (isNaN(searchNumber)) {
                return '';
            }
            return `${fieldName} = ${searchNumber}`;
        }
        return component.FilterTemplate ? `${component.FilterTemplate.replace(/\{0\}/g, searchTerm)}` : `charindex(N'${searchTerm}', ${fieldName}) >= 1`;
    }

    /**
     * @param {string} featureName
     * @param {boolean | undefined} portal
     */
    static async InitFeatureByName(featureName, portal = true) {
        const {EditForm} = await import('../editForm.js');
        const {TabEditor} = await import('../tabEditor.js');
        const feature = await this.LoadFeature(featureName);
        if (!feature) {
            throw new Error('Feature not found');
        }
        const instance = new TabEditor(feature.EntityName);
        if (feature.Script) {
            ComponentExt.AssignMethods(feature, instance);
        }
        EditForm.Portal = portal;
        instance.Meta = feature;
        instance.Name = feature.Name;
        instance.Id = feature.Name + feature.Id;
        instance.Icon = feature.Icon;
        instance.Render();
        return instance;
    }

    /**
 * Loads a feature by name and optionally by ID, returning a promise that resolves to the feature.
 * 
 * @param {string} name - The name of the feature to load.
 * @param {string} [id=null] - The optional ID of the feature.
 * @returns {Promise<Component>} A promise that resolves to the loaded Feature object or null if not found.
 */
    static LoadFeature(name, id = null) {
        return new Promise((resolve, reject) => {
            // @ts-ignore
            const featureTask = Client.Instance.UserSvc({
                ComId: "Feature",
                Action: "GetFeature",
                MetaConn: Client.MetaConn,
                DataConn: Client.MetaConn,
                Params: JSON.stringify({ Name: name, Id: id })
            });

            featureTask.then(ds => {
                if (!ds || !ds[0]) {
                    resolve(null);
                    return;
                }
                const feature = ds[0][0];
                if (!feature) {
                    resolve(null);
                    return;
                }

                feature.FeaturePolicy = ds.length > 1 ? ds[1] : null;
                const groups = ds.length > 2 ? ds[2] : null;
                feature.ComponentGroup = groups;
                const components = ds.length > 3 ? ds[3] : null;
                feature.Component = components;

                if (!groups || !components) {
                    resolve(feature);
                    return;
                }

                const groupMap = groups.reduce((acc, group) => {
                    acc[group.Id] = group;
                    return acc;
                }, {});

                components.filter(x => x.ComponentType !== "Section").forEach(com => {
                    const g = groupMap[com.ParentId] || groupMap[com.ComponentGroupId];
                    if (!g) return;
                    if (!g.Children) g.Children = [];
                    g.Children.push(com);
                });

                resolve(feature);
            }).catch(err => reject(err));
        });
    }


    // Assign methods to an instance based on a feature's script
    static AssignMethods(feature, instance) {
        const scriptFunction = new Function('return ' + feature.Script)();
        Object.assign(instance, scriptFunction);
        if (typeof instance.Init === 'function') {
            instance.Init();
        }
    }

    // Find a component that has a specific event handler registered
    static FindComponentEvent(component, eventName) {
        let parent = component.ParentForm;
        while (parent !== null && !parent[eventName]) {
            parent = parent.ParentForm;
        }
        return parent;
    }

    // Modify the visibility of specific fields in a component
    static SetShow(component, show, ...fieldNames) {
        component.Children.filter(child => fieldNames.includes(child.Name))
            .forEach(child => child.Show = show);
    }

    // Alter position of HTMLElement relative to parent
    static AlterPosition(element, parentElement) {
        if (!element || !parentElement) {
            return;
        }
        const parentRect = parentElement.getBoundingClientRect();
        const elemRect = element.getBoundingClientRect();
        const outOfViewport = {
            Right: elemRect.right > window.innerWidth,
            Bottom: elemRect.bottom > window.innerHeight
        };

        if (outOfViewport.Right) {
            element.style.right = (window.innerWidth - elemRect.right) + 'px';
        } else {
            element.style.left = parentRect.left + 'px';
        }

        if (outOfViewport.Bottom) {
            element.style.bottom = (window.innerHeight - elemRect.bottom) + 'px';
        } else {
            element.style.top = parentRect.top + 'px';
        }
    }

    // Download a file using Blob and URL.createObjectURL
    static DownloadFile(filename, blob) {
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = URL.createObjectURL(blob);
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }

    // Toggle full screen mode for an element
    static FullScreen(element) {
        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.webkitRequestFullscreen) { /* Safari */
            element.webkitRequestFullscreen();
        } else if (element.msRequestFullscreen) { /* IE11 */
            element.msRequestFullscreen();
        }
    }

    static FindClosest(component, Type) {
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
    static async OpenTabOrPopup(com, tab) {
        const editablMd = await import('../editableComponent.js');
        const {EditForm} = await import('../editForm.js');
        const {TabEditor} = await import('../tabEditor.js');
        let parentTab;
        if (com instanceof EditForm) {
            parentTab = com;
        } else if (com instanceof editablMd.default) {
            parentTab = EditForm || this.FindClosest(com, EditForm);
        }
        if (tab instanceof TabEditor) {
            if (tab.Popup) {
                com.AddChild(tab);
            } else {
                tab.Render();
            }

            tab.ParentForm = parentTab;
            tab.OpenFrom = parentTab instanceof EditForm && parentTab?.FirstOrDefault(x => x.Entity === tab.Entity);
        }
    }

    /**
     * 
     * @param {EditableComponent} com 
     * @param {TabEditor} tab 
     */
    async OpenTabOrPopup(com, tab) {
        const { EditForm } = await import('../editForm.js');
        let parentTab;
        if (com instanceof EditForm) {
            parentTab = com;
        } else {
            parentTab = com.EditForm || com.FindClosest(x => x instanceof EditForm);
        }

        if (tab.Popup) {
            com.AddChild(tab);
        } else {
            tab.Render();
        }

        tab.ParentForm = parentTab;
        tab.OpenFrom = parentTab?.FilterChildren(x => x.Entity === tab.Entity)?.[0];
    }

    /**
     * @typedef {import('../tabEditor.js').TabEditor} TabEditor
     * @param {EditableComponent} com
     * @param {string} id
     * @param {string} featureName
     * @param {() => TabEditor} factory
     */
    static async OpenTab(com, id, featureName, factory, popup = false, anonymous = false) {
        const md = await import('../tabEditor.js');
        if (!popup && md.TabEditor.FindTab(id)) {
            const exists = md.TabEditor.FindTab(id);
            exists.Focus();
            return exists;
        }
        const feature = await this.LoadFeature(featureName);
        const tab = factory();
        tab.Popup = popup;
        tab.Name = featureName;
        tab.Id = id;
        tab.Meta = feature;
        this.AssignMethods(feature, tab);
        await this.OpenTabOrPopup(com, tab);
        return tab;
    }

    /**
     * @param {EditableComponent} com
     * @param {string} featureName
     * @param {{ (): EditableComponent }} factory
     */
    static OpenPopup(com, featureName, factory, anonymous = false, child = false) {
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
        return this.OpenTab(com, hashCode().toString(), featureName, factory, true, anonymous);
    };
}
