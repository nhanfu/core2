using Bridge.Html5;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using Direction = Core.MVVM.Direction;

namespace Core.Components.Forms
{
    public delegate void ClickHandler();
    public class ContextMenuItem
    {
        public HTMLElement Ele { get; set; }
        public string Icon { get; set; }
        public string Style { get; set; }
        public string Text { get; set; }
        public Action<object> Click { get; set; }
        public bool Disabled { get; set; }
        public object Parameter { get; set; }
        public List<ContextMenuItem> MenuItems { get; set; }
    }

    public class ContextMenu : EditableComponent
    {
        private static HTMLElement _root;
        public HTMLElement PElement;
        public double Top { get; set; }
        public double Left { get; set; }
        public ContextMenu _selectedContextMenuItem;
        public HTMLElement _selectedItem;
        public bool IsRoot => _selectedItem?.ParentElement == Element;
        public int _selectedIndex = -1;
        public List<ContextMenuItem> MenuItems = new List<ContextMenuItem>();
        private const string _active = "active";

        private static ContextMenu _instance;

        private ContextMenu() : base(null)
        {
            IsSingleton = true;
        }

        public static ContextMenu Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new ContextMenu();
                    _instance.MenuItems.Clear();
                }
                _instance.PElement = null;
                return _instance;
            }
        }

        public override void Render()
        {
            if (_root == null)
            {
                Html.Take(PElement ?? Document.Body).Ul.ClassName("context-menu").Event(EventType.FocusOut, Dispose).Event(EventType.KeyDown, HotKeyHandler);
                _root = Html.Context;
            }
            if (PElement is null && _root != null)
            {
                Document.Body.AppendChild(_root);
            }
            if (PElement != null && _root != null)
            {
                PElement.AppendChild(_root);
            }
            Element = _root;
            Html.Take(_root).Clear().TabIndex(-1).Floating(Top, Left);
            ParentElement = Element.ParentElement;
            RenderMenuItems(MenuItems);
            Window.SetTimeout(() =>
            {
                Element.Show();
                Element.Focus();
                AlterPosition();
            });
        }

        private void RenderMenuItems(List<ContextMenuItem> items, int level = 0)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items.ElementAt(i);
                if (item is null)
                {
                    continue;
                }

                Html.Instance.Li.Style(item.Style).Render();
                item.Ele = Html.Context;
                if (i == 0 && level == 0 && (items[i].MenuItems is null || items[i].MenuItems.Nothing()))
                {
                    _selectedIndex = i;
                    SetSelectedItem(Html.Context);
                }
                if (item.Disabled)
                {
                    Html.Instance.Disabled(true);
                }
                else
                {
                    Html.Instance.Event(EventType.Click, MenuItemClickHandler, item);
                }

                Html.Instance.Icon(item.Icon).End.Span.IText(item.Text).End.Render();
                if (item.MenuItems.HasElement())
                {
                    Html.Instance.Ul.Render();
                    RenderMenuItems(item.MenuItems, level + 1);
                    Html.Instance.End.Render();
                }
                Html.Instance.End.Render();
            }
        }

        private void SetSelectedItem(HTMLElement ele)
        {
            _selectedItem = ele;
            _selectedItem?.AddClass(_active);
        }

        private void MenuItemClickHandler(Event e, ContextMenuItem item)
        {
            e.StopPropagation();
            if (item is null || item.Click is null)
            {
                return;
            }

            item.Click(item.Parameter);
            Element?.DispatchEvent(new Event(EventType.FocusOut.ToString()));
        }

        private void HotKeyHandler(Event e)
        {
            e.PreventDefault();
            if (Element is null || Element.Children.Nothing())
            {
                return;
            }

            var children = _selectedItem?.ParentElement?.Children ?? Element.Children;
            var code = e.KeyCode();
            switch (code)
            {
                case 27:
                    Dispose();
                    break;
                case (int)KeyCodeEnum.LeftArrow:
                    if (IsRoot || _selectedItem is null || _selectedItem.ParentElement is null)
                    {
                        return;
                    }

                    _selectedItem.ParentElement.Children.SelectForEach(x => x.RemoveClass(_active));
                    _selectedItem = _selectedItem.ParentElement;
                    break;
                case (int)KeyCodeEnum.UpArrow:
                    e.PreventDefault();
                    e.StopPropagation();
                    children.SelectForEach(x => x.RemoveClass(_active));
                    _selectedIndex = _selectedIndex > 0 ? _selectedIndex - 1 : children.Length - 1;
                    SetSelectedItem(children.ElementAt(_selectedIndex));
                    break;
                case (int)KeyCodeEnum.RightArrow:
                    var ul = _selectedItem?.LastElementChild;
                    if (ul is null || ul.Children.Nothing())
                    {
                        return;
                    }

                    ul.Children.SelectForEach(x => x.RemoveClass(_active));
                    SetSelectedItem(ul.FirstElementChild);
                    break;
                case (int)KeyCodeEnum.DownArrow:
                    e.PreventDefault();
                    e.StopPropagation();
                    children.SelectForEach(x => x.RemoveClass(_active));
                    _selectedIndex = _selectedIndex < children.Length - 1 ? _selectedIndex + 1 : 0;
                    SetSelectedItem(children.ElementAt(_selectedIndex));
                    break;
                case (int)KeyCodeEnum.Enter:
                    if (_selectedItem is null)
                    {
                        SetSelectedItem(Element.FirstElementChild);
                    }
                    MenuItemClickHandler(e, MenuItems.Flattern(x => x.MenuItems).FirstOrDefault(x => x.Ele == _selectedItem));
                    break;
            }
        }

        public void AlterPosition()
        {
            Html.Take(Element).Floating(Top, Left);
            var clientRect = Element.GetBoundingClientRect();
            var outOfViewPort = Element.OutOfViewport();
            if (outOfViewPort.Bottom)
            {
                Html.Take(Element).Position(Direction.top, Top - clientRect.Height);
            }
            if (outOfViewPort.Right)
            {
                Html.Take(Element).Position(Direction.left, Left - clientRect.Width);
                Html.Take(Element).Position(Direction.top, Top);
            }
            outOfViewPort = Element.OutOfViewport();
            if (outOfViewPort.Bottom)
            {
                Html.Take(Element).Position(Direction.top, Top - clientRect.Height);
                Html.Take(Element).Position(Direction.top, Top - clientRect.Height - Element.ClientHeight);
            }
        }

        protected override void RemoveDOM()
        {
            _root.Hide();
        }
    }
}