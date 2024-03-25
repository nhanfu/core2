using Core.Extensions;
using Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Core.Components
{
    public class CompareGridView : GridView
    {
        public const string ContentFieldName = nameof(History.TextHistory);
        public const string ReasonOfChange = nameof(History.ReasonOfChange);
        private const string Style = "white-space: pre-wrap;";

        public CompareGridView(Component ui) : base(ui)
        {
            Meta.LocalHeader = new List<Component>
            {
                new Component
                {
                    FieldName = nameof(History.InsertedBy),
                    ComponentType = "Label",
                    Label = "Người thao tác",
                    Description = "Người thao tác",
                    ReferenceId = Utils.GetEntity(nameof(User))?.Id,
                    RefName = nameof(User),
                    FormatData = "{" + nameof(User.FullName) + "}",
                    Active = true,
                },
                new Component
                {
                    FieldName = nameof(History.InsertedDate),
                    ComponentType = "Label",
                    Label = "Ngày thao tác",
                    Description = "Ngày thao tác",
                    Active = true,
                    TextAlign = "left",
                    FormatData = "{0:dd/MM/yyyy HH:mm zz}"
                },
                new Component
                {
                    FieldName = nameof(History.ReasonOfChange),
                    ComponentType = "Label",
                    Label = "Nội dung",
                    Description = "Nội dung",
                    HasFilter = true,
                    Active = true,
                },
                new Component
                {
                    FieldName = nameof(History.TextHistory),
                    ComponentType = "Label",
                    ChildStyle = Style,
                    Label = "Chi tiết thay đổi",
                    Description = "Chi tiết thay đổi",
                    HasFilter = true,
                    Active = true,
                },
            };
        }

        protected override List<Component> FilterColumns(List<Component> Component)
        {
            base.FilterColumns(Component);
            Component.ForEach(x => x.Frozen = false);
            Header.Remove(Header.FirstOrDefault(x => x == ToolbarColumn));
            return Component;
        }
    }
}
