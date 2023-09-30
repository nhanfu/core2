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
            GuiInfo.LocalHeader = new List<Component>
            {
                new Component
                {
                    FieldName = nameof(History.InsertedBy),
                    ComponentType = "Label",
                    ShortDesc = "Người thao tác",
                    Description = "Người thao tác",
                    ReferenceId = Utils.GetEntity(nameof(User))?.Id,
                    Reference = new Entity
                    {
                        Name = nameof(User),
                    },
                    RefName = nameof(User),
                    FormatData = "{" + nameof(User.FullName) + "}",
                    Active = true,
                },
                new Component
                {
                    FieldName = nameof(History.InsertedDate),
                    ComponentType = "Label",
                    ShortDesc = "Ngày thao tác",
                    Description = "Ngày thao tác",
                    Active = true,
                    TextAlign = "left",
                    FormatData = "{0:dd/MM/yyyy HH:mm zz}"
                },
                new Component
                {
                    FieldName = nameof(History.ReasonOfChange),
                    ComponentType = "Label",
                    ShortDesc = "Nội dung",
                    Description = "Nội dung",
                    HasFilter = true,
                    Active = true,
                },
                new Component
                {
                    FieldName = nameof(History.TextHistory),
                    ComponentType = "Label",
                    ChildStyle = Style,
                    ShortDesc = "Chi tiết thay đổi",
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
