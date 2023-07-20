using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Components.Framework;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Fw
{
    public partial class Menu : EditableComponent
    {
        public IEnumerable<Feature> _feature;
        private const string ActiveClass = "active";
        public const string ASIDE_WIDTH = "44px";
        private static HTMLElement _main;
        private static Menu _instance;
        private bool _hasRender;
        private Menu() : base (null)
        {

        }
        public static Menu Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new Menu();
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
                    FocusFeature(focusedParentFeatureId.Value);
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
            RenderLayout();
            Task.Run(async () =>
            {
                var featureTask = new Client(nameof(Feature)).GetRawList<Feature>(
                    "?$expand=Entity($select=Name)&$filter=Active eq true and IsMenu eq true&$orderby=Order");
                var roles = string.Join("\\", Client.Token.RoleIds);
                var startAppTask = new Client(nameof(UserSetting)).GetRawList<UserSetting>("?$filter=Name eq 'StartApp'");
                await Task.WhenAll(featureTask, startAppTask);
                var feature = featureTask.Result;
                var startApps = startAppTask.Result.Combine(x => x.Value).Split(",").Select(x => x.TryParseInt() ?? 0).Distinct();
                _feature = feature;
                BuildFeatureTree();
                Html.Take(".sidebar-items");
                RenderMenuItems(_feature);
                await feature.Where(x => startApps.Contains(x.Id) || x.StartUp).ForEachAsync(OpenFeature);
                var featureParam = Utils.GetUrlParam(Utils.FeatureField).Replace("-", " ");
                if (!featureParam.IsNullOrWhiteSpace())
                {
                    var currentFeature = feature.FirstOrDefault(x => x.Name == featureParam);
                    await OpenFeature(currentFeature);
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

        private void RenderLayout()
        {
            Html.Take(Document.Body).Aside.ClassName("sidebar")
                .Div.ClassName("sidebar-header").End.Div.ClassName("sidebar-items").TabIndex(-1).Event(EventType.FocusOut, () => HideAll(Element)).End.Render();
            Element = Html.Context;
            AlterMainSectionWidth();
            Document.Body.InsertBefore(Element, Document.Body.FirstChild);
        }

        private void HideAll(HTMLElement current = null)
        {
            if (current is null)
            {
                current = Document.Body;
            }
            var activeLi = current.QuerySelectorAll("li.active");
            activeLi.ForEach(x => x.RemoveClass(ActiveClass));
        }

        private void AlterMainSectionWidth()
        {
            _main = Document.QuerySelector("#main").As<HTMLElement>();
            Element.TabIndex = -1;
            Element.Focus();
            Element.AddEventListener(EventType.FocusOut, () =>
            {
                Show = IsSmallUp;
            });
            Show = IsSmallUp;
        }

        public override bool Show
        {
            get => base.Show;
            set
            {
                base.Show = value;
                if (value)
                {
                    ShowAside();
                }
                else
                {
                    HideAside();
                }
            }
        }

        private void ShowAside()
        {
            Element.Style.Left = "0";
            _main.Style.Left = ASIDE_WIDTH;
            if (IsSmallUp)
            {
                _main.Style.Width = $"calc(100% - {ASIDE_WIDTH})";
            }
            else
            {
                _main.Style.Width = "100%";
                _main.Style.Left = "0";
            }
        }

        private void HideAside()
        {
            Element.Style.Left = $"-{ASIDE_WIDTH}";
            _main.Style.Width = "100%";
            _main.Style.Left = "0";
        }

        private void RenderMenuItems(IEnumerable<Feature> menuItems, bool nested = false)
        {
            Html.Instance.Ul.ForEach(menuItems, (item, index) =>
            {
                if (item.IsGroup)
                {
                    Html.Instance.Li.ClassName("group-title").Title(item.Label).End.Render();
                }
                else if (item.IsDevider)
                {
                    Html.Instance.Li.ClassName("divider").End.Render();
                }
                else
                {
                    Html.Instance.Li.DataAttr("feature", item.Id.ToString())
                    .A.Attr("data-role", "ripple")
                    .AsyncEvent(EventType.Click, MenuItemClick, item)
                    .Event(EventType.ContextMenu, FeatureContextMenu, item)
                    .Icon(item.Icon).End
                    .Title(item.Label).IText(nested ? item.Label : null).EndOf(ElementType.a).Render();
                    if (item.InverseParent != null && item.InverseParent.Count > 0)
                    {
                        RenderMenuItems(item.InverseParent.ToList(), nested: true);
                    }
                }
            });
        }

        private HTMLElement FindMenuItemByID(int id)
        {
            var activeLi = Document.QuerySelectorAll(".sidebar-items li");
            foreach (HTMLElement active in activeLi)
            {
                if (active.GetAttribute("data-feature").Equals(id.ToString()))
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
                ParentElement = TabEditor.Element,
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
            confirmDialog.YesConfirmed += async () =>
            {
                var client = new Client(nameof(Feature), typeof(Feature).Namespace);
                await client.CloneFeatureAsync(feature.Id);
                ReloadMenu(feature.ParentId);
            };
            AddChild(confirmDialog);
        }

        private void FeatureManagement(object ev)
        {
            var editor = new FeatureBL()
            {
                ParentElement = TabEditor.Element,
                OpenFrom = this.FindClosest<EditForm>(),
            };
            AddChild(editor);
        }

        private void Deactivate(object ev)
        {
            var feature = ev as Feature;
            var confirmDialog = new ConfirmDialog();
            confirmDialog.Content = "Bạn có muốn deactivate feature này?";
            confirmDialog.YesConfirmed += async () =>
            {
                var client = new Client(nameof(Feature));
                await client.DeactivateAsync(new List<int> { feature.Id });
            };
            AddChild(confirmDialog);
        }

        private async Task MenuItemClick(Feature feature, Event e)
        {
            var li = e.Target as HTMLElement;
            if (!(li is HTMLLIElement))
            {
                li = li.Closest("li") as HTMLLIElement;
            }
            HideAll(li.ParentElement.ParentElement);
            li.Focus();
            li.AddClass(ActiveClass);
            li.Closest(ElementType.li.ToString())?.AddClass(ActiveClass);
            AlterPositionSubMenu(e.Top(), li);
            await OpenFeature(feature);
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
            ul.Style.Top = (top - 20) + Utils.Pixel;
            ul.Style.Bottom = null;
            ul.Style.Transform = null;
            var outOfVp = ul.OutOfViewport();
            if (outOfVp.Bottom)
            {
                ul.Style.Top = null;
                ul.Style.Bottom = (Document.Body.ClientHeight - top) + Utils.Pixel;
                outOfVp = ul.OutOfViewport();
                if (outOfVp.Top)
                {
                    ul.Style.Top = "50%";
                    ul.Style.Bottom = null;
                    ul.Style.Transform = TranslateY50;
                }
            }
        }

        private void FocusFeature(int parentFeatureID)
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

        public static async Task OpenFeature(Feature feature)
        {
            if (feature is null || feature.ViewClass is null && feature.Entity is null)
            {
                return;
            }

            feature = await ComponentExt.LoadFeatureByName(feature.Name);
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
