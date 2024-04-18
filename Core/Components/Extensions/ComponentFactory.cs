using Core.Extensions;
using Core.Models;
using System;
using System.Linq;
using Bridge.Html5;
using Core.Components.Forms;

namespace Core.Components.Extensions
{
    public static class ComponentFactory
    {
        public static object GetComponent(Component ui, EditForm form, HTMLElement ele = null)
        {
            if (ui is null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            if (ui.ComponentType.IsNullOrEmpty())
            {
                return null;
            }

            ui.ComponentType = ui.ComponentType.Trim();
            object childComponent;
            switch (ui.ComponentType)
            {
                case nameof(GridView):
                    if (ui.GroupBy.IsNullOrWhiteSpace())
                    {
                        childComponent = new GridView(ui);
                    }
                    else
                    {
                        childComponent = new GroupGridView(ui);
                    }

                    break;
                case "ListView":
                    if (ui.GroupBy.IsNullOrWhiteSpace())
                    {
                        childComponent = new ListView(ui);
                    }
                    else
                    {
                        childComponent = new GroupListView(ui);
                    }

                    break;
                default:
                    childComponent = null;
                    var current = typeof(EditableComponent);
                    var fullName = ui.ComponentType.IndexOf(".") >=0 ? ui.ComponentType 
                        : current.Namespace + "." + ui.ComponentType;
                    var args = "a, b";
                    var body = $"return new {fullName}(a, b)";
                    var typeConstructor = new Function(args, body);
                    childComponent = typeConstructor.Call(null, ui, ele);
                    break;
            }
            childComponent[Utils.IdField] = ui.FieldName + ui.Id.ToString();
            childComponent["Name"] = ui.FieldName;
            childComponent["ComponentType"] = ui.ComponentType;
            childComponent["EditForm"] = form;
            return childComponent;
        }

        private static EditableComponent CompositedComponents(Component gui, EditForm form)
        {
            if (gui.FieldName.IsNullOrWhiteSpace() || gui.ComponentType.IsNullOrWhiteSpace())
            {
                return null;
            }

            var fields = gui.FieldName.Split(',');
            if (fields.Nothing())
            {
                return null;
            }

            gui.ComponentType = gui.ComponentType.Trim();
            var nonChar = gui.ComponentType.Where(x => !(x >= 'a' && x <= 'z' || x >= 'A' && x <= 'Z')).ToArray();
            if (fields.Count() != nonChar.Length + 1)
            {
                return null;
            }

            var section = new Section(MVVM.ElementType.div);
            section.DOMContentLoaded += () =>
            {
                fields.SelectForEach((field, index) =>
                {
                    var startIndex = index == 0 ? 0 : gui.ComponentType.IndexOf(nonChar[index - 1]);
                    var endIndex = index == fields.Length - 1 ? gui.ComponentType.Length - 1 : gui.ComponentType.IndexOf(nonChar[index]) - 1;
                    var componentType = gui.ComponentType.Substring(startIndex, endIndex - startIndex);
                    var childGui = new Component();
                    childGui.CopyPropFrom(gui);
                    childGui.ComponentType = componentType;
                    childGui.FieldName = field;
                    section.AddChild(GetComponent(childGui, form) as EditableComponent);
                    if (nonChar.Length > index)
                    {
                        section.Element.AppendChild(new Text(nonChar[index].ToString()));
                    }
                });
            };
            return section;
        }
    }
}
