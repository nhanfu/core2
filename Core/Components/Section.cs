using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class Section : EditableComponent
    {
        private ElementType? elementType;
        private HTMLElement InnerEle;
        private HTMLElement _chevron;

        public Section() : base(null)
        {
        }

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
            if (Meta is null)
            {
                return;
            }
            if (!Meta.Html.IsNullOrWhiteSpace())
            {
                var cssContent = Meta.Css;
                var hard = Meta.Id;
                var section = Meta.FieldName.ToLower() + hard;
                if (!cssContent.IsNullOrWhiteSpace())
                {
                    /*@
                        const regex = /(?:^|[\s\r\n])\.([a-zA-Z0-9-_]+)/g;
                        cssContent = cssContent.replace(regex, (match) => {
                            if (/\d/.test(match) || match.includes("minmax")) {
                                return match;
                            } else {
                                return match.replace(/([.])/, `[${section}]$1`);
                            }
                        });
                     */
                    if (Document.Head.QuerySelector("#" + section) is null)
                    {
                        var style = Document.CreateElement(ElementType.style.ToString()) as HTMLStyleElement;
                        style.Id = section;
                        style.AppendChild(new Text(cssContent));
                        Document.Head.AppendChild(style);
                    }
                }
                var cellText = Utils.GetHtmlCode(Meta.Html, new object[] { Entity });
                Element.InnerHTML = cellText;
                var allComPolicies = Meta.Id.IsNullOrWhiteSpace() ? EditForm.GetElementPolicies(Meta.Children.Select(x => x.Id).ToArray(), Utils.ComponentId) : new List<FeaturePolicy>().ToArray();
                SplitChild(Element.Children, allComPolicies, section);
                if (!Meta.Javascript.IsNullOrWhiteSpace())
                {
                    try
                    {
                        var fn = new Function(Meta.Javascript);
                        var obj = fn.Call(null, EditForm);
                        /*@
                        for (let prop in obj) this[prop] = obj[prop].bind(this);
                        if (this.useEffect != null) this.useEffect();
                        */
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                RenderChildrenSection(Meta);
                return;
            }
            if (Meta.IsDropDown)
            {
                Html.Take(Element).ClassName("dd-wrap").Style("position: relative;").TabIndex(-1)
                    .Event(EventType.FocusOut, HideDetailIfButtonOnly)
                    .Button.ClassName("btn ribbon").IText(Meta.Label)
                        .Event(EventType.Click, DropdownBtnClick).Span.Text("▼").EndOf(ElementType.button)
                    .Div.ClassName("dropdown").TabIndex(-1).Render();
                if (Meta.IsCollapsible == true)
                {
                    Html.Instance.Style("display: none;");
                }
                InnerEle = Html.Context;
                _chevron = InnerEle.PreviousElementSibling.FirstElementChild;

                Element = Html.Context;
            }
            if (Meta.Responsive && !Meta.IsTab || Meta.IsDropDown)
            {
                RenderComponentResponsive(Meta);
            }
            else
            {
                RenderComponent(Meta);
            }
            RenderChildrenSection(Meta);
        }

        private void SplitChild(HTMLCollection hTMLElements, FeaturePolicy[] allComPolicies, string section)
        {
            foreach (var eleChild in hTMLElements)
            {
                eleChild.SetAttribute(section, "");
                if (eleChild.Dataset["name"] != null)
                {
                    var com = new Section(ElementType.div)
                    {
                        Element = eleChild
                    };
                    var ui = Meta.Children.FirstOrDefault(x => x.FieldName == eleChild.Dataset["name"]);
                    eleChild.RemoveAttribute("data-name");
                    if (ui is null)
                    {
                        continue;
                    }
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
                    var component = ComponentFactory.GetComponent(ui, EditForm);
                    if (component == null) return;
                    var childComponent = component as EditableComponent;
                    /*@
                     if (childComponent == null) childComponent = component;
                     */
                    if (typeof(ListView).IsAssignableFrom(childComponent.GetType()))
                    {
                        EditForm.ListViews.Add(childComponent as ListView);
                    }
                    childComponent.ParentElement = eleChild;
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
                            childComponent.Element.AddEventListener(EventType.ContextMenu.ToString(), (e) => EditForm.SysConfigMenu(e, ui, Meta, null));
                        }
                    }
                    if (ui.Focus)
                    {
                        childComponent.Focus();
                    }
                }
                if (eleChild.Dataset["click"] != null)
                {
                    var eventName = eleChild.Dataset["click"];
                    eleChild.RemoveAttribute("data-click");
                    eleChild.AddEventListener(EventType.Click, async () =>
                    {
                        object method = null;
                        /*@
                        method = this[eventName];
                        */
                        using (Task task = null)
                        {
                            /*@
                            var task = method.apply(this.EditForm, [this]);
                            if (task == null || task.isCompleted == null) {
                                return;
                            }
                            */
                            try
                            {
                                await task;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                                throw ex;
                            }
                        }
                    });
                }
                if (eleChild.Dataset["change"] != null)
                {
                    var eventName = eleChild.Dataset["change"];
                    eleChild.RemoveAttribute("change");
                    eleChild.AddEventListener(EventType.Change, async () =>
                    {
                        object method = null;
                        /*@
                        method = this[eventName];
                        */
                        using (Task task = null)
                        {
                            /*@
                            var task = method.apply(this.EditForm, [this]);
                            if (task == null || task.isCompleted == null) {
                                return;
                            }
                            */
                            try
                            {
                                await task;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                Console.WriteLine(ex.StackTrace);
                                throw ex;
                            }
                        }
                    });
                }
                if (!eleChild.Children.Nothing())
                {
                    SplitChild(eleChild.Children, allComPolicies, section);
                }
            }
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

        private void RenderChildrenSection(Component group)
        {
            if (group.Children.Nothing())
            {
                return;
            }
            foreach (var child in group.Children.OrderBy(x => x.Order))
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "<Pending>")]
        public static void RenderTabGroup(EditableComponent parent, Component group)
        {
            var disabled = parent.Disabled || group.Disabled;
            if (parent.EditForm.TabGroup is null)
            {
                parent.EditForm.TabGroup = new List<TabGroup>();
            }

            var tabG = parent.EditForm.TabGroup.FirstOrDefault(x => x.Name == group.TabGroup);
            if (tabG is null)
            {
                tabG = new TabGroup
                {
                    Name = group.TabGroup,
                    Parent = parent,
                    ParentElement = parent.Element,
                    Entity = parent.Entity,
                    Meta = group,
                    EditForm = parent.EditForm,
                };
                tabG.Disabled = disabled;
                var subTab = new TabComponent(group)
                {
                    Parent = tabG,
                    Entity = parent.Entity,
                    Meta = group,
                    Name = group.FieldName,
                    EditForm = parent.EditForm,
                };
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
                var subTab = new TabComponent(group)
                {
                    Parent = tabG,
                    ParentElement = tabG.Element,
                    Entity = parent.Entity,
                    Meta = group,
                    Name = group.FieldName
                };
                subTab.Disabled = disabled;
                tabG.Children.Add(subTab);
                subTab.Render();
                //subTab.RenderTabContent();
                subTab.ToggleDisabled(group.DisabledExp);
                tabG.FirstChild.Focus();
            }
        }

        public static Section RenderSection(EditableComponent parent, Component groupInfo, object entity = null, EditForm editForm = null)
        {
            var editform = editForm ?? parent.EditForm;
            var uiPolicy = editform.GetElementPolicies(new string[] { groupInfo.Id }, Utils.ComponentGroupId);
            var readPermission = !groupInfo.IsPrivate || uiPolicy.HasElementAndAll(x => x.CanRead);
            var writePermission = !groupInfo.IsPrivate || uiPolicy.HasElementAndAll(x => x.CanWrite);
            if (!readPermission)
            {
                return null;
            }

            var width = groupInfo.Width;
            var outerColumn = editform.GetOuterColumn(groupInfo);
            var parentColumn = editform.GetInnerColumn(groupInfo.Parent);
            var hasOuterColumn = outerColumn > 0 && parentColumn > 0;
            if (hasOuterColumn)
            {
                var per = (decimal)outerColumn / parentColumn * 100;
                per = decimal.Round(per, 2, MidpointRounding.AwayFromZero);
                var padding = decimal.Round((groupInfo.ItemInRow.Value - 1m) / groupInfo.ItemInRow.Value, 2, MidpointRounding.AwayFromZero);
                width = outerColumn == parentColumn ? "100%" : $"calc({per}% - {padding}rem)";
            }

            Html.Take(parent.Element).Div.Render();
            if (!string.IsNullOrEmpty(groupInfo.Label))
            {
                Html.Instance.Label.ClassName("header").IText(groupInfo.Label);
                if (Client.SystemRole)
                {
                    Html.Instance.Attr("contenteditable", "true");
                    Html.Instance.Event(EventType.Input, (e) => ChangeComponentGroupLabel(e, groupInfo));
                    Html.Instance.Event(EventType.DblClick, (e) => editform.SectionProperties(groupInfo));
                }
                Html.Instance.End.Render();
            }
            Html.Instance.ClassName(groupInfo.ClassName).Event(EventType.ContextMenu, (e) => editform.SysConfigMenu(e, null, groupInfo, null));
            if (!groupInfo.ClassName.Contains("ribbon"))
            {
                Html.Instance.ClassName("panel").ClassName("group");
            }
            Html.Instance.Display(!groupInfo.Hidden).Style(groupInfo.Style ?? string.Empty).Width(width);
            var section = new Section(Html.Context)
            {
                Id = groupInfo.FieldName + groupInfo.Id?.ToString(),
                Name = groupInfo.FieldName,
                Meta = groupInfo,
            };
            section.Disabled = parent.Disabled || groupInfo.Disabled || !writePermission || editform.IsLock || section.Disabled;
            parent.AddChild(section, null, groupInfo.ShowExp);
            Html.Take(parent.Element);
            section.DOMContentLoaded?.Invoke();
            return section;
        }

        public override void PrepareUpdateView(bool force, bool? dirty)
        {
            base.PrepareUpdateView(force, dirty);
            ToggleShow(Meta?.ShowExp);
            ToggleDisabled(Meta?.DisabledExp);
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

        private void RenderComponent(Component group)
        {
            if (group.Children.Nothing())
            {
                return;
            }
            var html = Html.Instance;
            html.Table.ClassName("ui-layout").TBody.TRow.Render();
            var column = 0;
            var allComPolicies = EditForm.GetElementPolicies(group.Children.Select(x => x.Id).ToArray(), Utils.ComponentId);
            foreach (var ui in group.Children.OrderBy(x => x.Order))
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
                    html.TData.Visibility(ui.Visibility).Div.IText(ui.Label)
                        .TextAlign(column == 0 ? Enums.TextAlign.left : Enums.TextAlign.right);
                    if (Client.SystemRole)
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
                var childCom = ComponentFactory.GetComponent(ui, EditForm);
                if (childCom == null) return;
                var childComponent = childCom as EditableComponent;
                /*@
                if (childComponent.v == null) childComponent = { v: childCom };
                 */
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
                        childComponent.Element.AddEventListener(EventType.ContextMenu.ToString(), (e) => EditForm.SysConfigMenu(e, ui, group, childComponent));
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
            _imeout = Window.SetTimeout(() =>
            {
                SubmitLabelChanged(nameof(Component), com.Id, (e.Target as HTMLElement).TextContent.DecodeSpecialChar());
            }, 1000);
        }

        private static void SubmitLabelChanged(string table, string id, string label)
        {
            var patch = new PatchVM
            {
                Table = table,
                Changes = new List<PatchDetail>
                {
                    new PatchDetail { Field = IdField, Value = id },
                    new PatchDetail { Field = nameof(Component.Label), Value = label },
                }
            };
            Client.Instance.PatchAsync(patch).Done();
        }

        private static int _imeout1;
        private static void ChangeComponentGroupLabel(Event e, Component com)
        {
            Window.ClearTimeout(_imeout1);
            _imeout1 = Window.SetTimeout(() =>
            {
                SubmitLabelChanged(nameof(Meta), com.Id, (e.Target as HTMLElement).TextContent.DecodeSpecialChar());
            }, 1000);
        }

        private void RenderComponentResponsive(Component group)
        {
            if (group.Children.Nothing())
            {
                return;
            }
            var html = Html.Instance;
            var allComPolicies = EditForm.GetElementPolicies(group.Children.Select(x => x.Id).ToArray(), Utils.ComponentId);
            var innerCol = EditForm.GetInnerColumn(group);
            if (innerCol > 0)
            {
                Html.Take(Element).ClassName("grid").Style($"grid-template-columns: repeat({innerCol}, 1fr)");
            }
            var column = 0;
            foreach (var ui in group.Children.OrderBy(x => x.Order))
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
                    html.Div.IText(ui.Label).TextAlign(column == 0 ? Enums.TextAlign.left : Enums.TextAlign.right).Render();
                    label = Html.Context;
                    html.End.Render();
                }

                var childCom = ComponentFactory.GetComponent(ui, EditForm);
                if (childCom == null) return;
                var childComponent = childCom as EditableComponent;
                /*@
                if (childComponent.v == null) childComponent = { v: childCom };
                 */
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
                        childComponent.Element.AddEventListener(EventType.ContextMenu.ToString(), (e) => EditForm.SysConfigMenu(e, ui, group, childComponent));
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

        public ListViewSection(HTMLElement ele) : base(ele)
        {
        }

        public override void Render()
        {
            ListView = Parent as ListView;
            base.Render();
        }
    }
}
