using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components.Framework
{
    public partial class MenuComponent : EditableComponent
    {
        public IEnumerable<Feature> _feature;
        private const string ActiveClass = "active";
        public const string ASIDE_WIDTH = "44px";
        private static MenuComponent _instance;
        private bool _hasRender;
        private HTMLElement _elementMain;
        private HTMLElement _elementMenu;
        private HTMLElement _elementBrandLink;
        private HTMLElement _elementMainHeader;
        private HTMLElement _elementMainSidebar;
        private HTMLElement _elementExpand;
        private MenuComponent() : base(null)
        {
            _elementMain = Document.QuerySelector("#tab-content") as HTMLElement;
            _elementMenu = Document.QuerySelector("aside") as HTMLElement;
            _elementBrandLink = Document.QuerySelector(".brand-link") as HTMLElement;
            _elementMainHeader = Document.QuerySelector(".main-header") as HTMLElement;
            _elementMainSidebar = Document.QuerySelector(".main-sidebar") as HTMLElement;
            var widthMenu = LocalStorage.GetItem<string>("menu-width") ?? "202px";
            _elementMain.Style.MarginLeft = widthMenu;
            _elementBrandLink.Style.Width = widthMenu;
            _elementMainHeader.Style.MarginLeft = widthMenu;
            _elementMainSidebar.Style.Width = widthMenu;
        }
        public static MenuComponent Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new MenuComponent();
                }

                return _instance;
            }
        }

        private void BuildFeatureTree()
        {
            var dic = _feature.Where(f => f.IsMenu).ToDictionary(f => f.Id);
            foreach (var menu in dic.Values)
            {
                if (menu.ParentId != null && dic.ContainsKey(menu.ParentId))
                {
                    var parent = dic[menu.ParentId];
                    if (parent.InverseParent is null)
                    {
                        parent.InverseParent = new List<Feature>();
                    }
                    else
                    {
                        parent.InverseParent.Add(menu);
                    }
                }
            }
            _feature = _feature.Where(f => f.ParentId == null && f.IsMenu).ToList();
        }

        public void ReloadMenu(string focusedParentFeatureId)
        {
            BoostrapTask().Done(ds =>
            {
                _feature = ds[0].Select(x => x.CastProp<Feature>()).ToList();
                BuildFeatureTree();
                Html.Take(".sidebar-items").Clear();
                RenderMenuItems(_feature);
                DOMContentLoaded?.Invoke();
                if (focusedParentFeatureId != null)
                {
                    FocusFeature(focusedParentFeatureId);
                }
            });
        }

        public override void Render()
        {
            if (_hasRender)
            {
                return;
            }

            _hasRender = true;
            var startup = BoostrapTask();
            var roles = string.Join("\\", Client.Token.RoleIds);
            startup.Done(res =>
            {
                var features = res.Length > 0 ? res[0].Select(x => x.CastProp<Feature>()).ToArray() : new Feature[] { };
                var settings = res.Length > 1 ? res[1].Select(x => x.CastProp<UserSetting>()).ToArray() : new UserSetting[] { };
                var entities = res.Length > 2 ? res[2].Select(x => x.CastProp<Entity>()).ToArray() : new Entity[] { };
                var tasks = res.Length > 3 ? res[3].Select(x => x.CastProp<TaskNotification>()).ToList() : new List<TaskNotification>();
                SetTask(tasks);
                GetFeatureCb(features, settings, entities);
            });
        }

        private void SetTask(List<TaskNotification> tasks)
        {
            if (tasks.Nothing()) return;
            NotificationBL.Notifications.Data = tasks;
        }

        private static Task<object[][]> BoostrapTask()
        {
            var doc = Document.Instance as dynamic;
            var meta = doc.head.children.startupSvc;
            var startup = Client.Instance.UserSvc(new SqlViewModel
            {
                SvcId = meta.content,
                OrderBy = "ds.[Order] asc",
            });
            return startup;
        }

        private void GetFeatureCb(Feature[] features, UserSetting[] settings, Entity[] entities)
        {
            if (features.Nothing() || settings.Nothing() || entities.Nothing()) return;
            Client.Entities = entities.ToDictionary(x => x.Id);
            var startApps = settings.Where(x => x.Name == "Startup").Combine(x => x.Value)
                .Split(",").Select(x => x).Distinct().ToArray();
            _feature = features;
            BuildFeatureTree();
            Html.Take("#menu");
            RenderKeyMenuItems(_feature);
            var main = Document.QuerySelector("aside") as HTMLElement;
            CreateResizableTable(main);
            var urlFeatureName = App.GetFeatureNameFromUrl() ?? string.Empty;
            var tasks = new List<Task<bool>>() { OpenUrlFeature(urlFeatureName) };
            var shouldOpenClosedTabs = settings.FirstOrDefault(x => x.Name == "OpenClosedTabs")?.Value?.TryParse<bool>() ?? false;
            if (shouldOpenClosedTabs)
            {
                var previousTabs = Window.LocalStorage.GetItem("tabs") as string ?? string.Empty;
                var tabStates = JsonConvert.DeserializeObject<TabState[]>(previousTabs)
                    .Where(x => x.Name != urlFeatureName).ToArray();
                var previousTabNames = tabStates.Select(x => x.Name).ToArray();
                var startFeatures = features
                    .Where(x => startApps.Contains(x.Id) || startApps.Contains(x.Name) || x.StartUp)
                    .Where(x => !previousTabNames.Contains(x.Name) && x.Name != urlFeatureName).ToArray();
                tasks.AddRange(startFeatures.Select(OpenFeature).Concat(tabStates.Select(OpenFeature)));
            }
            else
            {
                tasks.AddRange(features
                    .Where(x => startApps.Contains(x.Id) || startApps.Contains(x.Name) || x.StartUp)
                    .Where(x => x.Name != urlFeatureName).Select(OpenFeature));
            }
            Task.WhenAll(tasks).Done(x =>
            {
                App.FeatureLoaded = true;
                DOMContentLoaded?.Invoke();
                _btnBack = Document.GetElementById("btnBack");
                _btnToggle = Document.GetElementsByClassName("sidebar-toggle").ToArray();
                if (_btnBack is null)
                {
                    return;
                }

                _btnBack.AddEventListener(EventType.Click, RoutingHandler);
                _btnToggle.ForEach(btn =>
                {
                    btn.AddEventListener(EventType.Click, () => Show = !Show);
                });
            });
        }

        private void CreateResizableTable(HTMLElement col)
        {
            var resizer = Document.CreateElement("div");
            resizer.AddClass("resizer");
            col.AppendChild(resizer);
            var button = Document.CreateElement("button");
            button.AddClass("resizer-button");
            button.AddEventListener(EventType.Click, () =>
            {
                if (int.Parse(_elementBrandLink.Style.Width.Replace("px", "")) < 100)
                {
                    _elementMain.Style.MarginLeft = "202px";
                    _elementBrandLink.Style.Width = "202px";
                    _elementMainHeader.Style.MarginLeft = "202px";
                    _elementMainSidebar.Style.Width = "202px";
                    col.Style.Width = "202px";
                    col.Style.MinWidth = "202px";
                    col.Style.MaxWidth = "202px";
                    _elementExpand.ReplaceClass("fa-arrow-circle-right", "fa-arrow-circle-left");
                }
                else
                {
                    _elementMain.Style.MarginLeft = "45px";
                    _elementBrandLink.Style.Width = "45px";
                    _elementMainHeader.Style.MarginLeft = "45px";
                    _elementMainSidebar.Style.Width = "45px";
                    col.Style.Width = "45px";
                    col.Style.MinWidth = "45px";
                    col.Style.MaxWidth = "45px";
                    _elementExpand.ReplaceClass("fa-arrow-circle-left", "fa-arrow-circle-right");
                }
            });
            _elementExpand = Document.CreateElement("i");
            if (int.Parse(_elementBrandLink.Style.Width.Replace("px", "")) < 100)
            {
                _elementExpand.AddClass("fal fa-arrow-circle-right");
            }
            else
            {
                _elementExpand.AddClass("fal fa-arrow-circle-left");
            }
            button.AppendChild(_elementExpand);
            col.AppendChild(button);
            CreateResizableColumn(col, resizer);
        }

        private double x = 0;
        private double w = 0;

        private void CreateResizableColumn(HTMLElement col, HTMLElement resizer)
        {
            x = 0;
            w = 0;
            resizer.AddEventListener("mousedown", (e) => MouseDownHandler(e, col, resizer));
        }

        private Action<MouseEvent> mouseMoveHandler;
        private Action<MouseEvent> mouseUpHandler;

        private void MouseDownHandler(object e, HTMLElement col, HTMLElement resizer)
        {
            var mouse = e as MouseEvent;
            mouse.PreventDefault();
            x = mouse.ClientX;
            var styles = Window.GetComputedStyle(col);
            w = double.Parse((styles.Width.Replace("px", "") == "") ? "0" : styles.Width.Replace("px", ""));
            mouseMoveHandler = (a) => MouseMoveHandler(a, col, resizer);
            mouseUpHandler = (a) => MouseUpHandler(a, col, resizer);
            Document.AddEventListener("mousemove", mouseMoveHandler);
            Document.AddEventListener("mouseup", mouseUpHandler);
            resizer.AddClass("resizing");
        }

        private void MouseMoveHandler(object e, HTMLElement col, HTMLElement resizer)
        {
            var mouse = e as MouseEvent;
            mouse.PreventDefault();
            var dx = mouse.ClientX - x;
            col.Style.Width = $"{w + dx}px";
            col.Style.MinWidth = $"{w + dx}px";
            col.Style.MaxWidth = $"{w + dx}px";
            _elementMain.Style.MarginLeft = $"{w + dx}px";
            _elementBrandLink.Style.Width = $"{w + dx}px";
            _elementMainHeader.Style.MarginLeft = $"{w + dx}px";
            if (w + dx < 100)
            {
                _elementExpand.ReplaceClass("fa-arrow-circle-left", "fa-arrow-circle-right");
            }
            else
            {
                _elementExpand.ReplaceClass("fa-arrow-circle-right", "fa-arrow-circle-left");
            }
            Window.LocalStorage.SetItem("menu-width", $"{w + dx}px");
        }

        private void MouseUpHandler(object e, HTMLElement col, HTMLElement resizer)
        {
            var mouse = e as MouseEvent;
            mouse.PreventDefault();
            resizer.RemoveClass("resizing");
            Document.RemoveEventListener("mousemove", mouseMoveHandler);
            Document.RemoveEventListener("mouseup", mouseUpHandler);
        }

        private static Task<bool> OpenUrlFeature(string featureName)
        {
            if (featureName.IsNullOrWhiteSpace())
            {
                return Task.FromResult(true);
            }

            return OpenFeature(new TabState { Name = featureName });
        }

        private static DateTime _lastTimeBackPress;
        private const int _timeperiodToExit = 2000;
        private const string TranslateY50 = "translateY(-50%)";

        private void RoutingHandler()
        {
            PressToExit();
            if (TabEditor.ActiveTab is TabEditor currentTab)
            {
                currentTab.DirtyCheckAndCancel();
            }
        }

        private static void PressToExit()
        {
            /*@
            if (typeof(navigator.app) === 'undefined') return;
            */
            var time = DateTime.Now - _lastTimeBackPress;
            if (time.TotalMilliseconds < 2000)
            {
                /*@
                navigator.app.exitApp();
                */
                return;
            }
            _lastTimeBackPress = DateTime.Now;
            Toast.Small("Bấm quay lại 2 lần để thoát", 1000);
        }

        private void HideAll(HTMLElement current = null)
        {
            if (current is null)
            {
                current = Document.Body;
            }
            var activea = current.QuerySelectorAll("a.active");
            var activeLi = current.QuerySelectorAll("li.menu-open");
            foreach (var item in activea)
            {
                item.RemoveClass(ActiveClass);
            }
            foreach (var item in activeLi)
            {
                item.RemoveClass("menu-open");
            }
        }

        private void AlterMainSectionWidth()
        {
            Element.TabIndex = -1;
            Element.Focus();
            Element.AddEventListener(EventType.FocusOut, () =>
            {
                Show = IsSmallUp;
            });
            Show = IsSmallUp;
        }

        private void HideAside()
        {
            Element.Style.Left = $"-{ASIDE_WIDTH}";
        }

        private void RenderKeyMenuItems(IEnumerable<Feature> menuItems, bool nested = false)
        {
            Html.Instance.Ul.ClassName("nav nav-pills nav-sidebar flex-column").DataAttr("widget", "treeview").Attr("role", "menu").DataAttr("accordion", "false").ForEach(menuItems, (item, index) =>
            {
                if (item.IsGroup)
                {
                    Html.Instance.Li.ClassName("nav-header").Title(item.Label).End.Render();
                }
                else
                {
                    var check = item.InverseParent != null && item.InverseParent.Count > 0;
                    Html.Instance.Li.ClassName("nav-item")
                    .A.ClassName("nav-link")
                    .Event(EventType.Click, MenuItemClick, item)
                    .Event(EventType.ContextMenu, FeatureContextMenu, item)
                    .Icon(item.Icon).ClassName("nav-icon").End.P.IText(item.Label);
                    if (check)
                    {
                        Html.Instance.I.ClassName("right fas fa-angle-left").End.Render();
                    }
                    Html.Instance.EndOf(ElementType.a).Render();
                    if (check)
                    {
                        RenderMenuItems(item.InverseParent.ToList(), nested: true);
                    }
                }
            });
        }

        private void RenderMenuItems(IEnumerable<Feature> menuItems, bool nested = false)
        {
            Html.Instance.Ul.ClassName("nav nav-treeview").ForEach(menuItems, (item, index) =>
            {
                var check = item.InverseParent != null && item.InverseParent.Count > 0;
                Html.Instance.Li.ClassName("nav-item")
                .A.ClassName("nav-link")
                .Event(EventType.Click, MenuItemClick, item)
                .Event(EventType.ContextMenu, FeatureContextMenu, item)
                .I.ClassName("fal fa-circle nav-icon").End.P.IText(item.Label);
                if (check)
                {
                    Html.Instance.I.ClassName("right fas fa-angle-left").End.Render();
                }
                Html.Instance.EndOf(ElementType.a).Render();
                if (check)
                {
                    RenderMenuItems(item.InverseParent.ToList(), nested: true);
                }
            });
        }

        private HTMLElement FindMenuItemByID(string id)
        {
            var activeLi = Document.QuerySelectorAll(".sidebar-items li");
            foreach (HTMLElement active in activeLi)
            {
                if (active.GetAttribute("data-feature").Equals(id))
                {
                    return active;
                }
            }
            return null;
        }

        private HTMLElement _btnBack;
        private HTMLElement[] _btnToggle;

        private void FeatureContextMenu(Event e, Feature feature)
        {
            if (!Client.SystemRole)
            {
                return;
            }

            e.PreventDefault();
            var ctxMenu = ContextMenu.Instance;
            {
                ctxMenu.Top = e.Top();
                ctxMenu.Left = e.Left();
                ctxMenu.MenuItems = new List<ContextMenuItem>
                {
                    new ContextMenuItem { Icon = "fa fa-plus", Text = "New feature", Click = EditFeature, Parameter = new Feature() },
                    new ContextMenuItem { Icon = "mif-unlink", Text = "Deactivate this feature", Click = Deactivate, Parameter = feature },
                    new ContextMenuItem { Icon = "fa fa-clone", Text = "Clone this feature", Click = CloneFeature, Parameter = feature },
                    new ContextMenuItem { Icon = "fa fa-list", Text = "Manage features", Click = FeatureManagement },
                    new ContextMenuItem { Icon = "fa fa-wrench", Text = "Properties", Click = EditFeature, Parameter = feature },
                };
            };
            ctxMenu.Render();
        }

        private void EditFeature(object ev)
        {
            var feature = ev as Feature;
            var editor = new FeatureDetailBL()
            {
                Entity = feature,
                ParentElement = Document.Body,
                OpenFrom = this.FindClosest<EditForm>(),
            };
            AddChild(editor);
        }

        private void CloneFeature(object ev)
        {
            var feature = ev as Feature;
            var confirmDialog = new ConfirmDialog
            {
                Content = "Bạn có muốn clone feature này?"
            };
            confirmDialog.YesConfirmed += () =>
            {
                var sql = new SqlViewModel
                {
                    ComId = "Feature",
                    Action = "Clone",
                    Ids = new string[] { feature.Id }
                };
                Client.Instance.UserSvc<bool>(sql).Done(x =>
                {
                    ReloadMenu(feature.ParentId);
                });
            };
            AddChild(confirmDialog);
        }

        private void FeatureManagement(object ev)
        {
            var editor = new FeatureBL()
            {
                ParentElement = Document.Body,
                OpenFrom = this.FindClosest<EditForm>(),
            };
            AddChild(editor);
        }

        private void Deactivate(object ev)
        {
            var feature = ev as Feature;
            var confirmDialog = new ConfirmDialog();
            confirmDialog.Content = "DO you want to deactivate this feature?";
            confirmDialog.YesConfirmed += () =>
            {
                Client.Instance
                    .DeactivateAsync(new string[] { feature.Id }, nameof(Feature), Client.ConnKey)
                    .Done();
            };
            AddChild(confirmDialog);
        }

        private void MenuItemClick(Feature feature, Event e)
        {
            var a = e.Target as HTMLElement;
            if (!(a is HTMLAnchorElement))
            {
                a = a.Closest("a") as HTMLAnchorElement;
            }
            var li = a.Closest(ElementType.li.ToString());
            if (li.HasClass("menu-open"))
            {
                li.RemoveClass("menu-open");
                return;
            }
            HideAll(a.Closest("ul"));
            a.Focus();
            if (a.HasClass(ActiveClass))
            {
                a.RemoveClass(ActiveClass);
            }
            else
            {
                a.AddClass(ActiveClass);
            }
            if (li.HasClass("menu-open"))
            {
                li.RemoveClass("menu-open");
            }
            else
            {
                li.AddClass("menu-open");
            }
            OpenFeature(feature);
        }

        private static void AlterPositionSubMenu(float top, HTMLElement li)
        {
            if (li is null)
            {
                return;
            }
            var ul = li.QuerySelector(ElementType.ul.ToString()) as HTMLElement;
            if (ul is null)
            {
                return;
            }
            ul.Style.Top = top - 20 + Utils.Pixel;
            ul.Style.Bottom = null;
            ul.Style.Transform = null;
            var outOfVp = ul.OutOfViewport();
            if (outOfVp.Bottom)
            {
                ul.Style.Top = null;
                ul.Style.Bottom = Document.Body.ClientHeight - top + Utils.Pixel;
                outOfVp = ul.OutOfViewport();
                if (outOfVp.Top)
                {
                    ul.Style.Top = "50%";
                    ul.Style.Bottom = null;
                    ul.Style.Transform = TranslateY50;
                }
            }
        }

        private void FocusFeature(string parentFeatureID)
        {
            var li = FindMenuItemByID(parentFeatureID);
            if (li != null)
            {
                var activeLi = Document.QuerySelectorAll(".sidebar-items li.active");
                foreach (HTMLElement active in activeLi)
                {
                    if (active.Contains(li))
                    {
                        continue;
                    }
                    active.RemoveClass("active");
                }
                li.AddClass(ActiveClass);
                li.ParentElement.AddClass(ActiveClass);
            }
        }

        public static Task<bool> OpenFeature(TabState state)
        {
            if (state is null)
            {
                return Task.FromResult(true);
            }
            var tcs = new TaskCompletionSource<bool>();
            ComponentExt.LoadFeature(Client.ConnKey, state.Name).Done(f =>
            {
                if (f is null || f.Component.Nothing()) return;
                EditForm instance = null;
                instance = new TabEditor(f.EntityName)
                {
                    Entity = state.Entity
                };
                if (!f.Script.IsNullOrWhiteSpace())
                {
                    ComponentExt.AssignMethods(f, instance);
                }
                instance.Name = f.Name;
                instance.Id = f.Name + f.Id;
                instance.Icon = f.Icon;
                instance.Feature = f;
                instance.Render();
                tcs.TrySetResult(true);
            });
            return tcs.Task;
        }

        public static Task<bool> OpenFeature(Feature feature)
        {
            if (feature is null)
            {
                return Task.FromResult(true);
            }
            var id = feature.Name + feature.Id;
            var exists = TabEditor.Tabs.FirstOrDefault(x => x.Id == id);
            if (exists != null)
            {
                exists.Focus();
                return Task.FromResult(true);
            }
            var tcs = new TaskCompletionSource<bool>();
            ComponentExt.LoadFeature(Client.ConnKey, feature.Name).Done(f =>
            {
                if (f is null || f.Component.Nothing()) return;
                EditForm instance = null;
                instance = new TabEditor(f.EntityName);
                if (!f.Script.IsNullOrWhiteSpace())
                {
                    ComponentExt.AssignMethods(f, instance);
                }

                instance.Name = f.Name;
                instance.Id = id;
                instance.Icon = f.Icon;
                instance.Feature = f;
                instance.Render();
                tcs.TrySetResult(true);
            });
            return tcs.Task;
        }

        protected override void RemoveDOM()
        {
            Html.Take(".sidebar-wrapper").Clear();
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            // Not to do anything here
        }
    }
}
