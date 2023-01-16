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
        public static EditableComponent GetComponent(Component ui, EditForm form)
        {
            if (ui is null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            if (ui.ComponentType.IsNullOrEmpty())
            {
                throw new InvalidOperationException($"Component type of {ui.Id} is null.");
            }

            ui.ComponentType = ui.ComponentType.Trim();
            EditableComponent childComponent;
            switch (ui.ComponentType)
            {
                case "Link":
                    childComponent = new Link(ui);
                    break;
                case "Input":
                    childComponent = new Textbox(ui);
                    break;
                case "Timepicker":
                    childComponent = new Timepicker(ui);
                    break;
                case "Password":
                    childComponent = new Textbox(ui) { Password = true };
                    break;
                case "Label":
                    childComponent = new CellText(ui);
                    break;
                case "Textarea":
                    childComponent = new Textbox(ui) { MultipleLine = true };
                    break;
                case "Dropdown":
                    childComponent = ui.Precision != null && ui.Precision >= 2 ? new MultipleSearchEntry(ui) : new SearchEntry(ui);
                    break;
                case "Image":
                    childComponent = new ImageUploader(ui);
                    break;
                case "GridView":
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
                    var current = typeof(EditableComponent);
                    var type = Type.GetType(current.Namespace + "." + ui.ComponentType);
                    if (type is null)
                    {
                        return CompositedComponents(ui, form);
                    }
                    childComponent = Activator.CreateInstance(type, ui) as EditableComponent;
                    break;
            }
            childComponent.Id = ui.FieldName + ui.Id.ToString();
            childComponent.Name = ui.FieldName;
            childComponent.ComponentType = ui.ComponentType;
            childComponent.EditForm = form;
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
                fields.ForEach((field, index) =>
                {
                    var startIndex = index == 0 ? 0 : gui.ComponentType.IndexOf(nonChar[index - 1]);
                    var endIndex = index == fields.Length - 1 ? gui.ComponentType.Length - 1 : gui.ComponentType.IndexOf(nonChar[index]) - 1;
                    var componentType = gui.ComponentType.Substring(startIndex, endIndex - startIndex);
                    var childGui = new Component();
                    childGui.CopyPropFrom(gui);
                    childGui.ComponentType = componentType;
                    childGui.FieldName = field;
                    section.AddChild(GetComponent(childGui, form));
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
