using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace TMS.UI.Business
{
    public partial class MenuComponent : EditableComponent
    {
        public IEnumerable<Feature> _feature;
        private const string ActiveClass = "active";
        public const string ASIDE_WIDTH = "44px";
        private static HTMLElement _main;
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
                if (menu.ParentId != null && dic.ContainsKey(menu.ParentId.Value))
                {
                    var parent = dic[menu.ParentId.Value];
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

        public void ReloadMenu(int? focusedParentFeatureId)
        {
            Task.Run(async () =>
            {
                var featureTask = new Client(nameof(Feature)).GetRawList<Feature>(
                    "?$expand=Entity($select=Name)&$filter=Active eq true and IsMenu eq true&$orderby=Order");
                var feature = await featureTask;
                _feature = feature;
                BuildFeatureTree();
                Html.Take(".nav-sidebar").Clear();
                RenderMenuItems(_feature);
                DOMContentLoaded?.Invoke();
            });
        }
        public override void Render()
        {
            if (_hasRender)
            {
                return;
            }

            _hasRender = true;
            Task.Run(async () =>
            {
                var featureTask = new Client(nameof(Feature)).GetRawList<Feature>(
                    "?$expand=Entity($select=Name)&$filter=Active eq true and IsMenu eq true&$orderby=Order");
                var roles = string.Join("\\", Client.Token.RoleIds);
                var startAppTask = new Client(nameof(UserSetting)).GetRawList<UserSetting>("?$filter=Name eq 'StartApp'");
                await Task.WhenAll(featureTask, startAppTask);
                var feature = featureTask.Result;
                var startApps = startAppTask.Result.Select(x => x.Value)
                    .Where(x => !x.IsNullOrWhiteSpace()).Select(x => x.Split(",")).SelectMany(x => x)
                    .Select(x => x.TryParseInt()).Where(x => x.HasValue);
                _feature = feature;
                BuildFeatureTree();
                Html.Take("#menu");
                RenderMenuItems(_feature);
                RenderMenuMobileItems(_feature, true);
                await feature.Where(x => startApps.Contains(x.Id) || x.StartUp).ForEachAsync(OpenFeature);
                var featureParam = Window.Location.PathName.Replace("/", "").Replace("-", " ");
                if (!featureParam.IsNullOrWhiteSpace())
                {
                    var currentFeature = feature.FirstOrDefault(x => x.Name == featureParam);
                    var id = Utils.GetUrlParam("Id");
                    if (currentFeature is null)
                    {
                        currentFeature = await new Client(nameof(Feature)).FirstOrDefaultAsync<Feature>($"?$expand=Entity&$filter=Name eq '{featureParam}'");
                        if (currentFeature is null)
                        {
                            return;
                        }
                        var entity = await new Client(currentFeature.Entity.Name).GetAsync<object>(int.Parse(id), refname: currentFeature.Entity.Name);
                        if (entity is null)
                        {
                            entity = new object();
                        }
                        await this.OpenTab(
                            id: currentFeature.Name + id,
                            featureName: currentFeature.Name,
                            factory: () =>
                            {
                                var type = Type.GetType(currentFeature.ViewClass);
                                var instance = Activator.CreateInstance(type) as TabEditor;
                                instance.Title = currentFeature.Label;
                                instance.Entity = entity;
                                return instance;
                            });
                    }
                    else
                    {
                        await OpenFeature(currentFeature);
                    }
                }
                DOMContentLoaded?.Invoke();
            });
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

        private void RenderMenuItems(IEnumerable<Feature> menuItems)
        {
            var html = Html.Instance.Ul.Id("menuHorizontal").ClassName("navbar-nav navbar-nav-highlight flex-wrap d-none d-xl-flex");
            html.ForEach(menuItems, (item, index) =>
            {
                var check = item.InverseParent != null && item.InverseParent.Count > 0;
                Html.Instance.Li.ClassName("nav-item dropdown").DataAttr("feature", item.Id.ToString())
                .A.Href(check ? "javascript:void(0);" : ("?f=" + item.Name.Replace(" ", "-") + "&Id=" + item.Id)).ClassName("navbar-nav-link" + (check ? " dropdown-toggle" : ""))
                .DataAttr("toggle", "dropdown").Attr("aria-expanded", "false")
                .AsyncEvent(EventType.Click, MenuItemClick, item)
                .Event(EventType.ContextMenu, FeatureContextMenu, item)
                .Title(item.Label).Render();
                Html.Instance.Span.IText(item.Label).Style("margin-left: 19px;margin-top: 4px;").EndOf(ElementType.a).Render();
                if (check)
                {
                    RenderSubMenu(item.InverseParent.ToList());
                }
                Html.Instance.EndOf(ElementType.li);
            });
        }

        private void RenderMenuMobileItems(IEnumerable<Feature> menuItems, bool nest = false)
        {
            Html.Take(".card-sidebar-mobile");
            var html = Html.Instance.Ul.ClassName("nav nav-sidebar").DataAttr("nav-type", nest ? "accordion" : "");
            html.ForEach(menuItems, (item, index) =>
            {
                var check = item.InverseParent != null && item.InverseParent.Count > 0;
                Html.Instance.Li.ClassName("nav-item" + (check ? " nav-item-submenu" : "")).DataAttr("feature", item.Id.ToString())
                .A.Href("javascript:void(0);").ClassName("nav-link")
                .AsyncEvent(EventType.Click, MenuItemClick, item)
                .Event(EventType.ContextMenu, FeatureContextMenu, item)
                .Title(item.Label).Render();
                Html.Instance.Span.IText(item.Label).Style("margin-left: 19px;margin-top: 4px;").EndOf(ElementType.a).Render();
                if (check)
                {
                    RenderSubMobileMenu(item.InverseParent.ToList());
                }
                Html.Instance.EndOf(ElementType.li);
            });
        }

        private void RenderSubMobileMenu(IEnumerable<Feature> menuItems)
        {
            Html.Instance.Ul.ClassName("nav nav-group-sub");
            Html.Instance.ForEach(menuItems, (item, index) =>
            {
                var check = item.InverseParent != null && item.InverseParent.Count > 0;
                Html.Instance.Li.ClassName("nav-item").A.Href("javascript:void(0);").ClassName("nav-link")
                .AsyncEvent(EventType.Click, MenuItemClick, item)
                .Event(EventType.ContextMenu, FeatureContextMenu, item)
                .I.ClassName(item.Icon ?? "").End
                .Title(item.Label).Span.IText(item.Label).End.End.Render();
                if (check)
                {
                    RenderSubMenu(item.InverseParent.ToList());
                }
            });
        }

        private void RenderSubMenu(IEnumerable<Feature> menuItems)
        {
            Html.Instance.Div.ClassName("dropdown-menu");
            Html.Instance.ForEach(menuItems, (item, index) =>
            {
                var check = item.InverseParent != null && item.InverseParent.Count > 0;
                if (check)
                {
                    Html.Instance.Div.ClassName("dropdown-submenu")
                    .A.Href("javascript:void(0);").ClassName("dropdown-item" + (check ? " dropdown-toggle" : ""));
                }
                else
                {
                    Html.Instance.A.Href("javascript:void(0);").ClassName("dropdown-item" + (check ? " dropdown-toggle" : ""));
                }
                Html.Instance.AsyncEvent(EventType.Click, MenuItemClick, item)
                .Event(EventType.ContextMenu, FeatureContextMenu, item)
                .I.ClassName(item.Icon ?? "").End
                .Title(item.Label).Span.IText(item.Label).End.Render();
                if (check)
                {
                    Html.Instance.End.Render();
                    RenderSubMenu(item.InverseParent.ToList());
                }
            });
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
                    new ContextMenuItem { Icon = "fa fa-ban", Text = "Deactivate this feature", Click = Deactivate, Parameter = feature },
                    new ContextMenuItem { Icon = "fa fa-clone", Text = "Clone this feature", Click = CloneFeature, Parameter = feature },
                    new ContextMenuItem { Icon = "fa fa-list", Text = "Manage features", Click = FeatureManagement },
                    new ContextMenuItem { Icon = "fa fa-wrench", Text = "Properties", Click = EditFeature, Parameter = feature },
                };
            };
            AddChild(ctxMenu);
        }

        private void EditFeature(object ev)
        {
            var feature = ev as Feature;
            var id = feature.Name + "Prop" + feature.Id;
            this.OpenTab(id, () => new FeatureDetailBL
            {
                Id = id,
                Entity = feature,
                Title = $"Feature {feature.Name ?? feature.Label ?? feature.Description}"
            });
        }

        public void CloneFeature(object ev)
        {
            var feature = ev as Feature;
            var confirmDialog = new ConfirmDialog
            {
                Content = "Bạn có muốn clone feature này?"
            };
            confirmDialog.YesConfirmed += async () =>
            {
                var client = new Client(nameof(Feature));
                await client.CloneFeatureAsync(feature.Id);
                ReloadMenu(feature.ParentId);
            };
            AddChild(confirmDialog);
        }

        private void FeatureManagement(object ev)
        {
            var id = GetHashCode().ToString();
            this.OpenTab(id, () => new FeatureBL
            {
                Id = GetHashCode().ToString(),
            });
        }

        private void Deactivate(object ev)
        {
            var feature = ev as Feature;
            var confirmDialog = new ConfirmDialog();
            confirmDialog.YesConfirmed += async () =>
            {
                var client = new Client(nameof(Feature));
                await client.DeactivateAsync(new List<int> { feature.Id });
            };
            AddChild(confirmDialog);
        }

        private async Task MenuItemClick(Feature feature, Event e)
        {
            e.PreventDefault();
            if (feature.InverseParent.Nothing())
            {
                /*@
                 $('body').removeClass('sidebar-mobile-main');
                 */
            }
            FocusFeature(feature);
            await OpenFeature(feature);
        }

        private HTMLElement FindMenuItemByID(int id)
        {
            var activeLi = Document.QuerySelectorAll(".card-sidebar-mobile .nav-link");
            foreach (HTMLElement active in activeLi)
            {
                if (active.ParentElement.GetAttribute("data-feature").Equals(id.ToString()))
                {
                    return active;
                }
            }
            return null;
        }

        private void FocusFeature(Feature feature)
        {
            var li = FindMenuItemByID(feature.Id);
            if (li != null)
            {
                var activeLi = Document.QuerySelectorAll(".card-sidebar-mobile .active").Union(Document.QuerySelectorAll(".card-sidebar-mobile .nav-item-open"));
                foreach (HTMLElement active in activeLi)
                {
                    if (active.Contains(li))
                    {
                        continue;
                    }
                    active.RemoveClass(ActiveClass);
                    active.RemoveClass("nav-item-open");
                    if (li.HasClass("child-link"))
                    {
                        active.ParentElement.RemoveClass("nav-item-open");
                    }
                    else
                    {
                        active.ParentElement.ParentElement.ParentElement.RemoveClass("nav-item-open");
                    }
                }
                if (li.HasClass("child-link"))
                {
                    li.ParentElement.ParentElement.ParentElement.AddClass("nav-item-open");
                }
                li.AddClass(ActiveClass);
            }
        }

        public static async Task OpenFeature(Feature feature)
        {
            if (feature is null || feature.ViewClass is null && feature.Entity is null)
            {
                return;
            }

            feature = await ComponentExt.LoadFeatureComponent(feature);
            Type type;
            if (feature.ViewClass != null)
            {
                type = Type.GetType(feature.ViewClass);
            }
            else
            {
                type = typeof(TabEditor);
            }
            var id = feature.Name + feature.Id;
            var exists = TabEditor.Tabs.FirstOrDefault(x => x.Id == id);
            if (exists != null)
            {
                exists.UpdateView();
                exists.Focus();
            }
            else
            {
                var instance = Activator.CreateInstance(type) as EditForm;
                instance.Name = feature.Name;
                instance.Id = id;
                instance.Icon = feature.Icon;
                instance.Feature = feature;
                instance.Render();
            }
            if (!IsSmallUp)
            {
                Instance.Show = false;
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            // not to do anything here
        }
    }
}
