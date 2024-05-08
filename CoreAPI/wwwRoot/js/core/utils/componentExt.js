import { Datepicker } from "../datepicker.js";
import { ComponentType } from "../models/componentType.js";
import { Utils } from "./utils.js";
import { EditForm } from "../editForm.js";

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

    // Load feature data from a mock service or similar data source
    static LoadFeature(name, id = null) {
        return new Promise((resolve, reject) => {
            // Simulate an API call to fetch feature data
            setTimeout(() => {
                // Mock data representing what might be returned from an API
                const feature = {
                    Name: name,
                    Id: id || 'default-id',
                    Script: 'function modify(){ console.log("Feature modified"); }',
                    EntityName: 'FeatureEntity'
                };
                resolve(feature);
            }, 100);
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
}
