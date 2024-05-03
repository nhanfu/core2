import { Utils } from './utils/utils.js';

export class CompareGridView extends GridView {
    constructor(ui) {
        super(ui);
        this.ContentFieldName = "TextHistory";
        this.ReasonOfChange = "ReasonOfChange";
        this.Style = "white-space: pre-wrap;";
        this.meta.localHeader = [
            {
                FieldName: "InsertedBy",
                ComponentType: "Label",
                Label: "Người thao tác",
                Description: "Người thao tác",
                ReferenceId: Utils.GetEntity("User")?.Id,
                RefName: "User",
                FormatData: "{" + "FullName" + "}",
                Active: true,
            },
            {
                FieldName: "InsertedDate",
                ComponentType: "Label",
                Label: "Ngày thao tác",
                Description: "Ngày thao tác",
                Active: true,
                TextAlign: "left",
                FormatData: "{0:dd/MM/yyyy HH:mm zz}"
            },
            {
                FieldName: "ReasonOfChange",
                ComponentType: "Label",
                Label: "Nội dung",
                Description: "Nội dung",
                HasFilter: true,
                Active: true,
            },
            {
                FieldName: "TextHistory",
                ComponentType: "Label",
                ChildStyle: this.Style,
                Label: "Chi tiết thay đổi",
                Description: "Chi tiết thay đổi",
                HasFilter: true,
                Active: true,
            },
        ];
    }

    FilterColumns(Component) {
        super.FilterColumns(Component);
        Component.forEach(x => x.Frozen = false);
        this.header.remove(this.header.find(x => x === this.toolbarColumn));
        return Component;
    }
}
