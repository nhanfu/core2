using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class Section : EditableComponent
    {
        private ElementType? elementType;
        private HTMLElement InnerEle;
        private HTMLElement _chevron;

        public ComponentGroup ComponentGroup { get; set; }

        public Section(ElementType elementType) : base(null)
        {
            this.elementType = elementType;
        }

        public Section(Element interactiveEle) : base(null)
        {
            Element = interactiveEle as HTMLElement;
        }

        public override void Render()
        {
            if (elementType is null)
            {
                var tag = Element.TagName.ToLowerCase();
                var parsed = Enum.TryParse(tag, out ElementType type);
                if (parsed)
                {
                    elementType = type;
                }
            }
            else
            {
                Html.Take(ParentElement).Add(elementType.Value);
                Element = Html.Context;
            }
            Element.Id = Id;
            if (ComponentGroup is null)
            {
                return;
            }
            if (ComponentGroup.IsDropDown)
            {
                Html.Take(Element).ClassName("dd-wrap").Style("position: relative;").TabIndex(-1)
                    .Event(EventType.FocusOut, HideDetailIfButtonOnly)
                    .Button.ClassName("btn ribbon").IText(ComponentGroup.Label)
                        .Event(EventType.Click, DropdownBtnClick).Span.Text("▼").EndOf(ElementType.button)
                    .Div.ClassName("dropdown").TabIndex(-1).Render();
                if (ComponentGroup.IsCollapsible == true)
                {
                    Html.Instance.Style("display: none;");
                }
                InnerEle = Html.Context;
                _chevron = InnerEle.PreviousElementSibling.FirstElementChild;

                Element = Html.Context;
            }
            if (ComponentGroup.Responsive && !ComponentGroup.IsTab || ComponentGroup.IsDropDown)
            {
                RenderComponentResponsive(ComponentGroup);
            }
            else
            {
                RenderComponent(ComponentGroup);
            }
            RenderChildrenSection(ComponentGroup);
        }

        private int _waitFocusOut;
        private bool? _isAllBtn;

        private void HideDetailIfButtonOnly()
        {
            if (_isAllBtn is null)
            {
                var allChildren = FilterChildren<EditableComponent>(x => x.Children.Nothing()).ToArray();
                _isAllBtn = allChildren.All(x => typeof(Button).IsAssignableFrom(x.GetType()));
            }
            Window.ClearTimeout(_waitFocusOut);
            _waitFocusOut = Window.SetTimeout(() =>
            {
                if (_isAllBtn == true)
                {
                    InnerEle.Style.Display = Display.None;
                }
            }, 150);
        }

        private void DropdownBtnClick(Event e)
        {
            e.StopPropagation();
            if (InnerEle.Style.Display.ToString() == Display.None.ToString())
            {
                InnerEle.Style.Display = string.Empty;
                _chevron.InnerHTML = "▼";
            }
            else
            {
                InnerEle.Style.Display = Display.None;
                _chevron.InnerHTML = "▼";
            }
        }

        private void RenderChildrenSection(ComponentGroup group)
        {
            if (group.InverseParent.Nothing())
            {
                return;
            }
            foreach (var child in group.InverseParent.OrderBy(x => x.Order))
            {
                child.Disabled = group.Disabled;
                if (child.IsTab)
                {
                    RenderTabGroup(this, child);
                }
                else
                {
                    RenderSection(this, child);
                }
            }
        }

        public static void RenderTabGroup(EditableComponent parent, ComponentGroup group)
        {
            var disabled = parent.Disabled || group.Disabled;
            if (parent.EditForm.TabGroup is null)
            {
                parent.EditForm.TabGroup = new List<TabGroup>();
            }

            var tabG = parent.EditForm.TabGroup.FirstOrDefault(x => x.Name == group.TabGroup);
            if (tabG is null)
            {
#pragma warning disable IDE0017 // Simplify object initialization
                tabG = new TabGroup
                {
                    Name = group.TabGroup,
                    Parent = parent,
                    ParentElement = parent.Element,
                    Entity = parent.Entity,
                    ComponentGroup = group,
                    EditForm = parent.EditForm,
                };
#pragma warning restore IDE0017 // Simplify object initialization
                tabG.Disabled = disabled;
#pragma warning disable IDE0017 // Simplify object initialization
                var subTab = new TabComponent(group)
                {
                    Parent = tabG,
                    Entity = parent.Entity,
                    ComponentGroup = group,
                    Name = group.Name,
                    EditForm = parent.EditForm,
                };
#pragma warning restore IDE0017 // Simplify object initialization
                subTab.Disabled = disabled;
                tabG.Children.Add(subTab);
                parent.EditForm.TabGroup.Add(tabG);
                parent.Children.Add(tabG);
                tabG.Render();
                subTab.Render();
                subTab.RenderTabContent();
                subTab.Focus();
                subTab.ToggleShow(group.ShowExp);
                subTab.ToggleDisabled(group.DisabledExp);
            }
            else
            {
#pragma warning disable IDE0017 // Simplify object initialization
                var subTab = new TabComponent(group)
                {
                    Parent = tabG,
                    ParentElement = tabG.Element,
                    Entity = parent.Entity,
                    ComponentGroup = group,
                    Name = group.Name
                };
#pragma warning restore IDE0017 // Simplify object initialization
                subTab.Disabled = disabled;
                tabG.Children.Add(subTab);
                subTab.Render();
                subTab.RenderTabContent();
                subTab.ToggleDisabled(group.DisabledExp);
                tabG.FirstChild.Focus();
            }
        }

        public static Section RenderSection(EditableComponent parent, ComponentGroup groupInfo)
        {
            var uiPolicy = parent.EditForm.GetElementPolicies(new string[] { groupInfo.Id }, Utils.ComponentGroupId);
            var readPermission = !groupInfo.IsPrivate || uiPolicy.HasElementAndAll(x => x.CanRead);
            var writePermission = !groupInfo.IsPrivate || uiPolicy.HasElementAndAll(x => x.CanWrite);
            if (!readPermission)
            {
                return null;
            }

            var width = groupInfo.Width;
            var outerColumn = parent.EditForm.GetOuterColumn(groupInfo);
            var parentColumn = parent.EditForm.GetInnerColumn(groupInfo.Parent);
            var hasOuterColumn = outerColumn > 0 && parentColumn > 0;
            if (hasOuterColumn)
            {
                var per = (decimal)outerColumn / parentColumn * 100;
                per = decimal.Round(per, 2, MidpointRounding.AwayFromZero);
                var padding = decimal.Round((groupInfo.ItemInRow - 1m) / groupInfo.ItemInRow, 2, MidpointRounding.AwayFromZero);
                width = outerColumn == parentColumn ? "100%" : $"calc({per}% - {padding}rem)";
            }

            Html.Take(parent.Element).Div.Render();
            if (!string.IsNullOrEmpty(groupInfo.Label))
            {
                Html.Instance.Label.ClassName("header").IText(groupInfo.Label);
                if (Client.CheckHasRole(RoleEnum.System))
                {
                    Html.Instance.Attr("contenteditable", "true");
                    Html.Instance.Event(EventType.Input, (e) => ChangeComponentGroupLabel(e, groupInfo));
                    Html.Instance.Event(EventType.DblClick, (e) => parent.EditForm.SectionProperties(groupInfo));
                }
                Html.Instance.End.Render();
            }
            Html.Instance.ClassName(groupInfo.ClassName).Event(EventType.ContextMenu, (e) => parent.EditForm.SysConfigMenu(e, null, groupInfo));
            if (!groupInfo.ClassName.Contains("ribbon"))
            {
                Html.Instance.ClassName("panel").ClassName("group");
            }
            Html.Instance.Display(!groupInfo.Hidden).Style(groupInfo.Style ?? string.Empty).Width(width);
            var section = new Section(Html.Context)
            {
                Id = groupInfo.Name + groupInfo.Id?.ToString(),
                Name = groupInfo.Name,
                ComponentGroup = groupInfo,
            };
            section.Disabled = parent.Disabled || groupInfo.Disabled || !writePermission || parent.EditForm.IsLock || section.Disabled;
            parent.AddChild(section, null, groupInfo.ShowExp);
            Html.Take(parent.Element);
            section.DOMContentLoaded?.Invoke();
            return section;
        }

        public override void PrepareUpdateView(bool force, bool? dirty)
        {
            base.PrepareUpdateView(force, dirty);
            ToggleShow(ComponentGroup?.ShowExp);
            ToggleDisabled(ComponentGroup?.DisabledExp);
        }

        public void ComponentProperties(object component)
        {
            var editor = new ComponentBL()
            {
                Entity = component,
                ParentElement = Element,
                OpenFrom = this.FindClosest<EditForm>(),
            };
            AddChild(editor);
        }

        private void RenderComponent(ComponentGroup group)
        {
            if (group.Component.Nothing())
            {
                return;
            }
            var html = Html.Instance;
            html.Table.ClassName("ui-layout").TBody.TRow.Render();
            var column = 0;
            var allComPolicies = EditForm.GetElementPolicies(group.Component.Select(x => x.Id).ToArray(), Utils.ComponentId);
            foreach (var ui in group.Component.OrderBy(x => x.Order))
            {
                if (ui.Hidden)
                {
                    continue;
                }

                var comPolicies = allComPolicies.Where(x => x.RecordId == ui.Id).ToArray();
                var readPermission = !ui.IsPrivate || comPolicies.HasElementAndAll(x => x.CanRead);
                var writePermission = !ui.IsPrivate || comPolicies.HasElementAndAll(x => x.CanWrite);
                if (!readPermission)
                {
                    continue;
                }

                var colSpan = ui.Column ?? 2;
                ui.Label = ui.Label ?? string.Empty;
                if (ui.ShowLabel)
                {
                    html.TData.Visibility(ui.Visibility).Label.IText(ui.Label)
                        .TextAlign(column == 0 ? Enums.TextAlign.left : Enums.TextAlign.right);
                    if (Client.CheckHasRole(RoleEnum.System))
                    {
                        Html.Instance.Attr("contenteditable", "true");
                        Html.Instance.Event(EventType.Input, (e) => ChangeLabel(e, ui));
                        Html.Instance.Event(EventType.DblClick, (e) => ComponentProperties(ui));
                    }
                    html.EndOf(ElementType.td).TData.Visibility(ui.Visibility).ColSpan(colSpan - 1).Render();
                }
                else
                {
                    html.TData.Visibility(ui.Visibility).ColSpan(colSpan).ClassName("text-left")
                        .Style("padding-left: 0;").Render();
                }

                if (ui.Style.HasAnyChar())
                {
                    html.Style(ui.Style);
                }

                if (ui.Width.HasAnyChar())
                {
                    html.Width(ui.Width);
                }

                var childComponent = ComponentFactory.GetComponent(ui, EditForm);
                if (typeof(ListView).IsAssignableFrom(childComponent.GetType()))
                {
                    EditForm.ListViews.Add(childComponent as ListView);
                }
                AddChild(childComponent);
                if (childComponent is EditableComponent editable)
                {
                    editable.Disabled = ui.Disabled || Disabled || !writePermission || editable.Disabled;
                }
                if (childComponent.Element != null)
                {
                    if (ui.ChildStyle.HasAnyChar())
                    {
                        var current = Html.Context;
                        Html.Take(childComponent.Element).Style(ui.ChildStyle);
                        Html.Take(current);
                    }
                    if (ui.ClassName.HasAnyChar())
                    {
                        childComponent.Element?.AddClass(ui.ClassName);
                    }

                    if (ui.Row == 1)
                    {
                        childComponent.ParentElement.ParentElement.AddClass("inline-label");
                    }

                    if (Client.SystemRole)
                    {
                        childComponent.Element.AddEventListener(EventType.ContextMenu.ToString(), (e) => EditForm.SysConfigMenu(e, ui, group));
                    }
                }
                if (ui.Focus)
                {
                    childComponent.Focus();
                }

                html.EndOf(ElementType.td);
                if (ui.Offset != null && ui.Offset > 0)
                {
                    html.TData.ColSpan(ui.Offset.Value).End.Render();
                    column += ui.Offset.Value;
                }
                column += colSpan;
                if (column == EditForm.GetInnerColumn(group))
                {
                    column = 0;
                    html.EndOf(ElementType.tr).TRow.Render();
                }
            }
        }

        private int _imeout;
        private void ChangeLabel(Event e, Component com)
        {
            Window.ClearTimeout(_imeout);
            _imeout = Window.SetTimeout(async () =>
            {
                var comDB = await Client.Instance.GetByIdAsync(com.Id, nameof(Component));
                var html = e.Target as HTMLElement;
                comDB.Label = html.TextContent.Trim();
                await new Client(nameof(Component)).UpdateAsync<Component>(comDB);
            }, 1000);
        }

        private static int _imeout1;
        private static void ChangeComponentGroupLabel(Event e, ComponentGroup com)
        {
            Window.ClearTimeout(_imeout1);
            _imeout1 = Window.SetTimeout(async () =>
            {
                var comDB = await Client.Instance.GetByIdAsync(com.Id, nameof(ComponentGroup));
                var html = e.Target as HTMLElement;
                comDB.Label = html.TextContent.Trim();
                await new Client(nameof(ComponentGroup)).UpdateAsync<ComponentGroup>(comDB);
            }, 1000);
        }

        private void RenderComponentResponsive(ComponentGroup group)
        {
            if (group.Component.Nothing())
            {
                return;
            }
            var html = Html.Instance;
            var allComPolicies = EditForm.GetElementPolicies(group.Component.Select(x => x.Id).ToArray(), Utils.ComponentId);
            var innerCol = EditForm.GetInnerColumn(group);
            if (innerCol > 0)
            {
                Html.Take(Element).ClassName("grid").Style($"grid-template-columns: repeat({innerCol}, 1fr)");
            }
            var column = 0;
            foreach (var ui in group.Component.OrderBy(x => x.Order))
            {
                if (ui.Hidden)
                {
                    continue;
                }

                var comPolicies = allComPolicies.Where(x => x.RecordId == ui.Id).ToArray();
                var readPermission = !ui.IsPrivate || comPolicies.HasElementAndAll(x => x.CanRead);
                var writePermission = !ui.IsPrivate || comPolicies.HasElementAndAll(x => x.CanWrite);
                if (!readPermission)
                {
                    continue;
                }

                Html.Take(Element);
                var colSpan = ui.Column ?? 2;
                ui.Label = ui.Label ?? string.Empty;
                HTMLElement label = null;
                if (ui.ShowLabel)
                {
                    html.Label.IText(ui.Label).TextAlign(column == 0 ? Enums.TextAlign.left : Enums.TextAlign.right).Render();
                    label = Html.Context;
                    html.End.Render();
                }

                var childComponent = ComponentFactory.GetComponent(ui, EditForm);
                if (typeof(ListView).IsAssignableFrom(childComponent.GetType()))
                {
                    EditForm.ListViews.Add(childComponent as ListView);
                }
                AddChild(childComponent);
                if (childComponent is EditableComponent editable)
                {
                    editable.Disabled = ui.Disabled || Disabled || !writePermission || EditForm.IsLock || editable.Disabled;
                }

                if (childComponent.Element != null)
                {
                    if (ui.ChildStyle.HasAnyChar())
                    {
                        var current = Html.Context;
                        Html.Take(childComponent.Element).Style(ui.ChildStyle);
                        Html.Take(current);
                    }
                    if (ui.ClassName.HasAnyChar())
                    {
                        childComponent.Element?.AddClass(ui.ClassName);
                    }

                    if (ui.Row == 1)
                    {
                        childComponent.ParentElement.ParentElement.AddClass("inline-label");
                    }

                    if (Client.SystemRole)
                    {
                        childComponent.Element.AddEventListener(EventType.ContextMenu.ToString(), (e) => EditForm.SysConfigMenu(e, ui, group));
                    }
                }
                if (ui.Focus)
                {
                    childComponent.Focus();
                }

                if (colSpan <= innerCol)
                {
                    if (label != null && label.NextElementSibling != null && colSpan != 2)
                    {
                        label.NextElementSibling.Style.GridColumn = $"{column + 2}/{column + colSpan + 1}";
                    }
                    else if (childComponent.Element != null)
                    {
                        childComponent.Element.Style.GridColumn = $"{column + 2}/{column + colSpan + 1}";
                    }
                    column += colSpan;
                }
                else
                {
                    column = 0;
                }
                if (column == innerCol)
                {
                    column = 0;
                }
            }
        }
    }

    public class ListViewSection : Section
    {
        public ListView ListView { get; internal set; }
        public ListViewSection(ElementType elementType) : base(elementType)
        {
        }

        public ListViewSection(Element interactiveEle) : base(interactiveEle)
        {
        }

        public override void Render()
        {
            ListView = Parent as ListView;
            base.Render();
        }
    }
}
