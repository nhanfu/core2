using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
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
        private MenuComponent() : base(null)
        {

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
            Task.Run(async () =>
            {
                var featureTask = new Client(nameof(Feature), typeof(Feature).Namespace).GetRawList<Feature>(
                    "?$expand=Entity($select=Name)&$filter=Active eq true and IsMenu eq true&$orderby=Order");
                var feature = await featureTask;
                _feature = feature;
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
            var doc = Document.Instance as dynamic;
            var meta = doc.head.children.startupSvc;
            var submitEntity = new SqlViewModel
            {
                SvcId = meta.content,
                OrderBy = "ds.[Order] asc",
            };
            var startup = Client.Instance.SubmitAsync<object[][]>(new XHRWrapper
            {
                Value = JSON.Stringify(submitEntity),
                Url = Utils.UserSvc,
                IsRawString = true,
                Method = HttpMethod.POST
            });
            var roles = string.Join("\\", Client.Token.RoleIds);
            Client.ExecTask(startup, (res) =>
            {
                var features = res[0].Select(x => x.CastProp<Feature>()).ToArray();
                var startApps = res[1].Select(x => x.CastProp<UserSetting>()).ToArray();
                var entities = res[2].Select(x => x.CastProp<Entity>()).ToArray();
                GetFeatureCb(features, startApps, entities);
            });
        }

        private void GetFeatureCb(Feature[] feature, UserSetting[] startApp, Entity[] entities)
        {
            Client.Entities = entities.ToDictionary(x => x.Id);
            var startApps = startApp.Combine(x => x.Value).Split(",").Select(x => x).Distinct();
            _feature = feature;
            BuildFeatureTree();
            Html.Take("#menu");
            RenderKeyMenuItems(_feature);
            var featureParam = Window.Location.PathName.SubStrIndex(Window.Location.PathName.LastIndexOf("/") + 1);
            if (!featureParam.IsNullOrWhiteSpace())
            {
                var currentFeature = feature.FirstOrDefault(x => x.Name == featureParam);
                OpenFeature(currentFeature);
            }
            else
            {
                feature.Where(x => startApps.Contains(x.Id) || x.StartUp).ForEach(OpenFeature);
            }
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
            var editor = new FeatureBL()
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
                Client.Instance.CloneFeatureAsync(feature.Id).Done(() =>
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

        public static void OpenFeature(Feature feature)
        {
            if (feature is null)
            {
                return;
            }
            var id = feature.Name + feature.Id;
            var exists = TabEditor.Tabs.FirstOrDefault(x => x.Id == id);
            if (exists != null)
            {
                exists.Focus();
                return;
            }
            ComponentExt.LoadFeature(feature.Name).Done(f =>
            {
                if (f is null) return;
                EditForm instance = null;
                instance = new TabEditor(f.EntityName);
                if (!f.Script.IsNullOrWhiteSpace())
                {
                    var obj = Window.Eval<object>(f.Script);
                    /*@
                    for (let prop in obj) instance[prop] = obj[prop];
                    if (instance.Init != null) instance.Init();
                    */
                }

                instance.Name = f.Name;
                instance.Id = id;
                instance.Icon = f.Icon;
                instance.Feature = f;
                instance.Render();
            });
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
