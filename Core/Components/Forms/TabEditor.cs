using Bridge.Html5;
using Core.Components.Extensions;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components.Forms
{
    public class TabEditor : EditForm
    {
        private const string ActiveClass = "active";
        protected HTMLElement _backdrop;
        protected HTMLElement _backdropGridView;
        protected HTMLElement _tab;
        public static HTMLElement TabContainer = Document.GetElementById("tab-content");
        public static List<TabEditor> Tabs = new List<TabEditor>();
        public static TabEditor ActiveTab => Tabs.FirstOrDefault(x => x.Show);
        public static EditableComponent FindTab(string id) => Tabs.FirstOrDefault(x => x.Id == id);
        public Dictionary<string, List<object>> DataSearchEntry = new Dictionary<string, List<object>>();
        private HTMLElement _li;
        public bool Popup { get; set; }
        public bool ChildForm { get; set; }
        public static bool ShowTabText { get; set; }
        private List<Button> _hotKeyComponents;

        public TabEditor(string entity = null) : base(entity)
        {
            PopulateDirty = false;
            ShouldLoadEntity = true;
        }

        public override void Render()
        {
            if (ParentElement is null && !ChildForm)
            {
                ParentElement = TabEditor?.Element ?? TabContainer;
            }
            if (Popup)
            {
                RenderPopup();
            }
            else
            {
                RenderTab();
            }
            Focus();
        }

        private string TabTitle => Feature?.Label ?? Title;
        private void RenderTab()
        {
            if (ChildForm)
            {
                Html.Take(ParentElement).Div.Render();
                Element = Html.Context;
                base.Render();
                return;
            }
            var html = Html.Take("#tabs");
            html.Li.ClassName("nav-item").Title(TabTitle)
            .A.ClassName("nav-link pl-lg-2 pr-lg-2 pl-xl-3 pr-xl-3")
            .Event(EventType.Click, Focus).Event(EventType.MouseUp, Close);
            if (!Icon.IsNullOrWhiteSpace())
            {
                html.Icon(Icon).End.Render();
            }
            html.Icon("fa fal fa-compress-wide").Event(EventType.Click, (e) =>
            {
                FullScreen();
            }).End.Render();
            html.Icon("fa fa-times").Event(EventType.Click, (e) =>
            {
                e.StopPropagation();
                DirtyCheckAndCancel();
            }).End.Render();
            if (ShowTabText)
            {
                html.Span.ClassName("title").IText(TabTitle).End.Render();
            }

            _li = Html.Context.ParentElement;
            IconElement = _li.FirstElementChild;
            Html.Take(TabContainer).TabIndex(-1).Trigger(EventType.Focus).Div.Event(EventType.KeyDown, HotKeyHandler).Render();
            Element = Html.Context;
            ParentElement = TabContainer;
            Tabs.Add(this);
            var tabIds = Tabs.Select(x => x.Name).ToList();
            Window.LocalStorage.SetItem("tabs", tabIds.Combine());
            base.Render();
        }

        private void FullScreen()
        {
            var elem = Element as dynamic;
            if (elem.requestFullscreen) {
                elem.requestFullscreen();
            } else if (elem.webkitRequestFullscreen) { 
                elem.webkitRequestFullscreen();
            } else if (elem.msRequestFullscreen) {
                elem.msRequestFullscreen();
            }
        }

        public void RenderPopup()
        {
            if (Parent is GridView)
            {
                Html.Take(ParentElement ?? Parent?.Element ?? TabContainer)
                .Div.ClassName("backdrop-gridview").TabIndex(-1).Trigger(EventType.Focus).Event(EventType.KeyDown, HotKeyHandler);
                _backdropGridView = Html.Context;
                Html.Instance
                    .Div.ClassName("popup-content").Div.ClassName("popup-title").Span.IconForSpan(Icon);
                IconElement = Html.Context;
                Html.Instance.End.Span.IText(Title);
                TitleElement = Html.Context;
                Html.Instance.End.Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, Dispose)
                    .EndOf(".popup-title")
                    .Div.ClassName("popup-body");
                Element = Html.Context;
                base.Render();
                if (_backdropGridView.OutOfViewport().Top)
                {
                    _backdropGridView.ScrollIntoView(true);
                }
            }
            else
            {
                Html.Take(ParentElement ?? Parent?.Element ?? TabContainer)
                .Div.ClassName("backdrop").TabIndex(-1).Trigger(EventType.Focus).Event(EventType.KeyDown, HotKeyHandler);
                _backdrop = Html.Context;
                Html.Instance
                    .Div.ClassName("popup-content").Div.ClassName("popup-title").Span.IconForSpan(Icon);
                IconElement = Html.Context;
                Html.Instance.End.Span.IText(Title);
                TitleElement = Html.Context;
                Html.Instance.End.Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, Dispose)
                    .EndOf(".popup-title")
                    .Div.ClassName("popup-body");
                Element = Html.Context;
                base.Render();
                if (_backdrop.OutOfViewport().Top)
                {
                    _backdrop.ScrollIntoView(true);
                }
            }
        }

        protected void HotKeyHandler(Event e)
        {
            if (e["keyCode"] == null)
            {
                return;
            }

            var keyCode = e.KeyCodeEnum();
            if (keyCode == KeyCodeEnum.F6)
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                if (gridView != null && !gridView.AllListViewItem.Any(x => x.Selected))
                {
                    if (gridView.AllListViewItem.Any())
                    {
                        gridView.AllListViewItem.FirstOrDefault().Focus();
                    }
                    else
                    {
                        gridView.ListViewSearch.Focus();
                    }
                }
                return;
            }
            if (e.AltKey() && keyCode == KeyCodeEnum.GraveAccent)
            {
                if (Tabs.Count <= 1)
                {
                    return;
                }
                int index;
                index = Tabs.IndexOf(this);
                if (index >= Tabs.Count - 1)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }
                if (index < 0 || index > Tabs.Count)
                {
                    return;
                }

                Tabs.ElementAt(index)?.Focus();
            }
            var shiftKey = e.ShiftKey();
            var ctrlKey = e.CtrlOrMetaKey();
            var altKey = e.AltKey();
            var defaultKeys = DefaultHotKeys(keyCode, shiftKey, ctrlKey, altKey);
            if (defaultKeys)
            {
                e.PreventDefault();
                e.StopPropagation();
                return;
            }
            if (keyCode >= KeyCodeEnum.Shift && keyCode <= KeyCodeEnum.Alt)
            {
                return;
            }

            TriggerMatchHotKey(e, keyCode, shiftKey, ctrlKey, altKey);
        }

        private void DisposeTabView()
        {
            _tab.Hide();
        }

        private void TriggerMatchHotKey(Event e, KeyCodeEnum? keyCode, bool shiftKey, bool ctrlKey, bool altKey)
        {
            if (keyCode == null)
            {
                return;
            }
            var patternList = new List<KeyCodeEnum>();
            if (shiftKey)
            {
                patternList.Add(KeyCodeEnum.Shift);
            }

            if (ctrlKey)
            {
                patternList.Add(KeyCodeEnum.Ctrl);
            }

            if (altKey)
            {
                patternList.Add(KeyCodeEnum.Alt);
            }

            if (keyCode < KeyCodeEnum.Shift)
            {
                patternList.Insert(0, keyCode.Value);
            }
            else if (keyCode > KeyCodeEnum.Alt)
            {
                patternList.Add(keyCode.Value);
            }

            _hotKeyComponents = _hotKeyComponents
               ?? this.FilterChildren(x => x is Button && !x.GuiInfo.HotKey.IsNullOrWhiteSpace()).Cast<Button>().ToList();
            foreach (var com in _hotKeyComponents)
            {
                var parts = com.GuiInfo.HotKey.Split(",");
                if (parts.Nothing())
                {
                    continue;
                }

                var lastPart = parts.LastOrDefault();
                var configKeys = lastPart.Split("-").Select(x =>
                {
                    var parsed = Enum.TryParse<KeyCodeEnum>(x, out var key);
                    return parsed ? (KeyCodeEnum?)key : null;
                }).Where(x => x != null).Cast<KeyCodeEnum>().OrderBy(x => x);
                var isMatch = Enumerable.SequenceEqual(patternList, configKeys);
                if (!isMatch)
                {
                    continue;
                }

                e.PreventDefault();
                e.StopPropagation();
                com.Element?.Click();
                return;
            }
        }

        private bool DefaultHotKeys(KeyCodeEnum? keyCode, bool shiftKey, bool ctrlKey, bool altKey)
        {
            if (keyCode == null)
            {
                return false;
            }
            if (keyCode == KeyCodeEnum.Escape && !shiftKey && !ctrlKey && !altKey)
            {
                DirtyCheckAndCancel();
                return true;
            }
            else if (ctrlKey && altKey && (keyCode == KeyCodeEnum.LeftArrow || keyCode == KeyCodeEnum.RightArrow))
            {
                int index;
                index = Tabs.IndexOf(this);
                if (keyCode == KeyCodeEnum.LeftArrow)
                {
                    if (index == 0)
                    {
                        index = Tabs.Count - 1;
                    }
                    else
                    {
                        index--;
                    }
                }
                else
                {
                    if (index >= Tabs.Count - 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        index++;
                    }
                }
                if (index < 0 || index > Tabs.Count)
                {
                    return false;
                }

                Tabs.ElementAt(index)?.Focus();
                return true;
            }
            else if (ctrlKey && shiftKey && keyCode == KeyCodeEnum.F)
            {
                // trigger search gridview
                var listView = this.FindActiveComponent<ListView>().FirstOrDefault();
                if (listView is null || !listView.GuiInfo.CanSearch)
                {
                    return true;
                }

                listView.ListViewSearch.AdvancedSearch(null);
                return true;
            }
            return false;
        }

        public override void Focus()
        {
            if (!Popup && !ChildForm)
            {
                Tabs.ForEach(x => x.Show = false);
            }

            if (!ChildForm)
            {
                Show = true;
            }
            if (Feature != null && Feature.Name != null && !Popup && !ChildForm)
            {
                var ns = (Document.Instance as dynamic).head.children.baseUri;
                var newPath = System.IO.Path.Combine(ns?.content ?? Window.Location.Origin,
                    Feature.Name.Replace(" ", "-") + $"{(Entity?[IdField] == null ? "" : $"?Id={Entity[IdField]}")}");
                Window.History.PushState(null, LangSelect.Get(TabTitle), newPath);
            }
            Document.Title = LangSelect.Get(TabTitle);
            this.FindActiveComponent<EditableComponent>(x => x?.GuiInfo?.Focus == true).FirstOrDefault()?.Focus();
        }

        public override bool Show
        {
            get => base.Show;
            set
            {
                base.Show = value;
                if (_li is null)
                {
                    return;
                }

                if (value)
                {
                    _li.AddClass(ActiveClass);
                    _li.QuerySelector(ElementType.a.ToString()).AddClass(ActiveClass);
                }
                else
                {
                    _li.RemoveClass(ActiveClass);
                    _li.QuerySelector(ElementType.a.ToString()).RemoveClass(ActiveClass);
                }
            }
        }

        public void Close(Event e)
        {
            int.TryParse(e["which"]?.ToString(), out int intWhich);
            int.TryParse(e["button"]?.ToString(), out int intButton);
            if (intWhich == 2 || intButton == 1)
            {
                e.PreventDefault();
                DirtyCheckAndCancel();
            }
        }

        public override void Dispose()
        {
            if (ParentForm != null)
            {
                ParentForm.Focus();
            }
            else if (Parent != null)
            {
                Parent.Focus();
            }
            else if (ParentElement != null)
            {
                ParentElement.Focus();
            }

            if (!Popup && _li != null)
            {
                _li.Remove();
                DisposeTab();
            }
            else
            {
            }
            var firstGridView = Parent.FindActiveComponent<GridView>().FirstOrDefault();
            if (firstGridView != null && firstGridView.LastListViewItem != null && firstGridView.LastElementFocus != null)
            {
                firstGridView.LastListViewItem.Focused = true;
                firstGridView.LastElementFocus.Focus();
            }
            base.Dispose();
        }

        protected virtual void DisposeTab()
        {
            if (ParentForm is null)
            {
                Tabs.LastOrDefault(x => x != this)?.Focus();
            }
            else
            {
                ParentForm.Focus();
                ParentForm = null;
            }
            Tabs.Remove(this);
            var tabIds = Tabs.Select(x => x.Name).ToList();
            Window.LocalStorage.SetItem("tabs", tabIds.Combine());
        }

        protected override void RemoveDOM()
        {
            Element?.Remove();
            _li?.Remove();
            _backdrop?.Remove();
            _backdropGridView?.Remove();
        }
    }
}
