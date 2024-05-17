import { Datepicker } from "../datepicker.js";
import { ComponentType } from "../models/componentType.js";
import { Utils } from "./utils.js";
import { EditForm } from "../editForm.js";
import { Component } from "../models/component.js";
import { Client } from "../clients/client.js";
import { SqlViewModel } from "../models/sqlViewModel.js";
import { TabEditor } from "tabEditor.js";
import { ListViewItem } from "listViewItem.js";
import EditableComponent from "editableComponent.js";

export class ComponentExt {
    static MapToPatch(com, table = null, fields = null) {
        const patch = {
            Table: table,
            Changes: [],
        };
        Utils.ForEachProp(com, (prop, val) => {
            if (prop.startsWith("$") || (fields && !fields.includes(prop))) return;
            patch.Changes.push({
                Field: prop,
                Value: val?.toString()
            });
        });
        return patch;
    }

    static MapToFilterOperator(component, searchTerm) {
        if (!searchTerm || !component.FieldName) {
            return '';
        }

        searchTerm = searchTerm.trim();
        let fieldName = component.DisplayField ? `JSON_VALUE(ds.[${component.DisplayField}], '$.${component.DisplayDetail}')` : `ds.[${component.FieldName}]`;
        if (!fieldName) return '';

        if (component.ComponentType === ComponentType.Datepicker) {
            const { parsed, datetime } = Datepicker.TryParseDateTime(searchTerm);
            if (parsed) {
                const dateStr = datetime.toISOString().slice(0, 10).replace(/-/g, '/');
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

    static InitFeatureByName(featureName, portal = true) {
        return new Promise((resolve, reject) => {
            this.LoadFeature(featureName).then(feature => {
                if (!feature) {
                    reject(new Error('Feature not found'));
                    return;
                }
                const instance = new EditForm(); // Assuming it creates an instance of the required type
                instance.Feature = feature;
                instance.Name = feature.Name;
                instance.Id = feature.Name + feature.Id;
                instance.Icon = feature.Icon;
                if (portal) {
                    instance.RenderPortal(); // Assuming a method to render in a portal context
                } else {
                    instance.Render();
                }
                resolve(instance);
            }).catch(err => reject(err));
        });
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

    static OpenTabOrPopup(com, tab) {
        let parentTab;
        if (com instanceof EditForm) {
            parentTab = com;
        } else if (com instanceof EditableComponent) {
            parentTab = com.EditForm || this.FindClosest(com , EditForm);
        }
        if (tab instanceof TabEditor) {
            if (tab.Popup) {
                com.AddChild(tab);
            } else {
                tab.Render();
            }
        
            tab.ParentForm = parentTab;
            tab.OpenFrom = parentTab instanceof EditForm && parentTab?.FilterChildren(x => x.Entity === tab.Entity)?.[0];
        }
    }
    
    OpenTabOrPopup(com, tab) {
        let parentTab;
        if (com instanceof EditForm) {
            parentTab = com;
        } else {
            parentTab = com.EditForm || com.FindClosest(EditForm);
        }
    
        if (tab.Popup) {
            com.AddChild(tab);
        } else {
            tab.Render();
        }
    
        tab.ParentForm = parentTab;
        tab.OpenFrom = parentTab?.FilterChildren(x => x.Entity === tab.Entity)?.[0];
    }

    static OpenTab(com, id, featureName, factory, popup = false, anonymous = false) {
        return new Promise((resolve, reject) => {
            if (!popup && TabEditor.FindTab(id)) {
                const Exists = TabEditor.FindTab(id);
                Exists.Focus();
                resolve(Exists);
                return;
            }
            this.LoadFeature(featureName).then(Feature => {
                const Tab = factory();
                Tab.Popup = popup;
                Tab.Name = featureName;
                Tab.Id = id;
                Tab.Feature = Feature;
                this.AssignMethods(Feature, Tab);
                this.OpenTabOrPopup(com, Tab);
                resolve(Tab);
            });
        });
    }

    static OpenPopup (com , featureName, factory, anonymous = false, child = false) {
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
        return this.OpenTab(com, hashCode().toString(), featureName, factory, true, anonymous);
    };

    
}
