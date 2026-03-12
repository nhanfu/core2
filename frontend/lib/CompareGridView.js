import { GridView } from './gridView.js';
import { Utils } from './utils/utils.js';

export class CompareGridView extends GridView {
    /**
     * @param {import("./models/component.js").Component} ui
     */
    constructor(ui) {
        super(ui);
        this.contentFieldName = "textHistory";
        this.reasonOfChange = "reasonOfChange";
        this.style = "white-space: pre-wrap;word-break: break-word;";
        this.meta.localHeader = [
            {
                fieldName: "insertedBy",
                componentType: "Label",
                Label: "Người thao tác",
                description: "Người thao tác",
                referenceId: Utils.getEntity("user")?.id.toString(),
                refName: "user",
                formatData: "{" + "fullName" + "}",
                active: true,
            },
            {
                fieldName: "insertedDate",
                componentType: "Label",
                Label: "Ngày thao tác",
                description: "Ngày thao tác",
                active: true,
                textAlign: "left",
                formatData: "{0:dd/mM/yyyy hH:mm zz}"
            },
            {
                fieldName: "reasonOfChange",
                componentType: "Label",
                Label: "Nội dung",
                description: "Nội dung",
                hasFilter: true,
                active: true,
            },
            {
                fieldName: "textHistory",
                componentType: "Label",
                childStyle: this.style,
                Label: "chi tiết thay đổi",
                description: "chi tiết thay đổi",
                hasFilter: true,
                active: true,
            },
        ];
    }

    filterColumns(component) {
        super.filterColumns(component);
        component.forEach(x => x.frozen = false);
        this.header.remove(this.header.find(x => x === GridView.toolbarColumn));
        return component;
    }
}
