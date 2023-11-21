using Bridge.Html5;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components.Forms
{
    public class TabGroup : EditableComponent
    {
        private readonly List<string> ListViewType = new List<string> { nameof(ListView), nameof(GroupListView), nameof(GridView), nameof(GroupGridView) };
        public ComponentGroup ComponentGroup { get; set; }
        public HTMLUListElement Ul { get; set; }
        public HTMLDivElement TabContent { get; set; }
        public bool ShouldCountBage { get; private set; }

        public TabGroup() : base(null)
        {
        }

        public override void Render()
        {
            Html.Take(ParentElement).Div.ClassName("tab-group")
                .ClassName(ComponentGroup.IsVertialTab ? "tab-vertical" : "tab-horizontal")
                .Div.ClassName("headers-wrapper").Ul.ClassName("nav-config  nav nav-tabs nav-tabs-bottom mb-0");
            Ul = Html.Context as HTMLUListElement;
            Element = Ul.ParentElement;
            Html.Instance.End.End.Div.ClassName("tabs-content");
            TabContent = Html.Context as HTMLDivElement;
            ShouldCountBage = ComponentGroup.InverseParent.All(x => x.Component.HasElement() && ListViewType.Contains(x.Component.First().ComponentType));
        }
    }

    public class TabComponent : Section
    {
        private const string ActiveClass = "active";
        private HTMLElement _li;
        public bool HasRendered { get; set; }
        private string _badge;
        protected HTMLElement BadgeElement;
        private bool _displayBadge;
        public TabComponent NextTab
        {
            get
            {
                var count = Parent.Children.Count();
                var thisIndex = Parent.Children.IndexOf(this);
                if (thisIndex == count)
                {
                    return (TabComponent)Parent.Children.FirstOrDefault();
                }
                else
                {
                    return (TabComponent)Parent.Children[thisIndex + 1];
                }
            }
        }

        public TabComponent(ComponentGroup group) : base(null)
        {
            ComponentGroup = group;
            Name = group.Name;
            Window.Instance.AddEventListener("Count" + Name, RealtimeUpdateBadge);
        }

        public virtual string Badge
        {
            get => _badge;
            set
            {
                _badge = value;
                if (BadgeElement != null)
                {
                    BadgeElement.TextContent = _badge;
                }
            }
        }

        public bool DisplayBadge
        {
            get => _displayBadge;
            set
            {
                _displayBadge = value;
                if (value)
                {
                    BadgeElement.Show();
                }
                else
                {
                    BadgeElement.Hide();
                }
            }
        }

        public override void Render()
        {
            var policies = EditForm.GetElementPolicies(new string[] { ComponentGroup.Id }, Utils.ComponentGroupId);
            var readPermission = !ComponentGroup.IsPrivate || policies.HasElementAndAll(x => x.CanRead);
            if (!readPermission)
            {
                return;
            }

            Html.Take(Parent.As<TabGroup>().Ul).Li
                .A.ClassName("nav-link tab-default")
                .I.ClassName(ComponentGroup.Icon ?? string.Empty).End
                .IText(ComponentGroup.Label ?? ComponentGroup.Name)
                .Span.ClassName("ml-1 badge badge-warning");
            BadgeElement = Html.Context;
            if (DisplayBadge)
            {
                Html.Instance.Text(Badge ?? string.Empty);
            }
            else
            {
                BadgeElement.Hide();
            }

            _li = Html.Context.ParentElement.ParentElement;
            Html.Instance.End.Div.ClassName("desc").IText(ComponentGroup.Description ?? string.Empty).End.Render();
            _li.AddEventListener(EventType.Click, () =>
            {
                if (HasRendered)
                {
                    Focus();
                }
                else
                {
                    Focus();
                    RenderTabContent();
                }
            });
            Task.Run(CountBadge);
        }

        internal void RealtimeUpdateBadge(dynamic updatedData)
        {
            DisplayBadge = true;
            Badge = updatedData;
        }

        internal async Task CountBadge()
        {
            var parent = Parent as TabGroup;
            if (ComponentGroup.BadgeMonth is null)
            {
                return;
            }
            var gridView = ComponentGroup.Component.FirstOrDefault();
            if (gridView is null || gridView.DataSourceFilter.IsNullOrEmpty())
            {
                return;
            }
            try
            {
                var query = ListView.GetFormattedDataSource(this, gridView.DataSourceFilter);
                var badgeMonth = ComponentGroup.BadgeMonth;
                if (badgeMonth == null)
                {
                    return;
                }
                var finalCount = CalcBadgeMonth(query, badgeMonth) + "&$count=true";
                var res = await new Clients.Client(gridView.RefName).GetAsync<OdataResult<object>>(finalCount);
                if (res is null || res.Odata is null)
                {
                    return;
                }
                DisplayBadge = true;
                var count = res.Odata.Count;
                if (count <= 1000)
                {
                    Badge = count.ToString();
                }
                else if (count < 1000000)
                {
                    Badge = (count / 1000) + "K";
                }
                else
                {
                    Badge = (count / 1000000) + "M";
                }
            }
            catch
            {
            }
        }

        public static string CalcBadgeMonth(string query, int? badgeMonth)
        {
            if (badgeMonth is null)
            {
                return query;
            }
            var startDate = DateTime.Now.AddMonths(-badgeMonth.Value);
            startDate = startDate.AddDays(-startDate.Day);
            var filterPart = OdataExt.GetClausePart(query, OdataExt.FilterKeyword) + $" and {nameof(User.InsertedDate)} ge cast({startDate.ToISOFormat()},Edm.DateTimeOffset)";
            return OdataExt.ApplyClause(query, filterPart, OdataExt.FilterKeyword);
        }

        public void RenderTabContent()
        {
            Html.Take(Parent.As<TabGroup>().TabContent).Div.ClassName("tab-content");
            Element = Html.Context;
            var editForm = this.FindClosest<EditForm>();
            RenderSection(this, ComponentGroup);
            HasRendered = true;
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(ComponentGroup.Events, EventType.DOMContentLoaded, Entity);
            });
        }

        public override void Focus()
        {
            Parent.As<TabGroup>().Children
                .Cast<TabComponent>().Where(x => x != this)
                .ToArray()
                .ForEach(x => x.Show = false);
            Show = true;
            EditForm.ResizeListView();
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(ComponentGroup.Events, EventType.FocusIn, Entity);
            });
        }

        public override bool Show
        {
            get => base.Show;
            set
            {
                if (_li is null)
                {
                    return;
                }

                base.Show = value;
                if (value)
                {
                    _li.AddClass(ActiveClass);
                    _li.QuerySelector(ElementType.a.ToString()).AddClass(ActiveClass);
                    Task.Run(async () =>
                    {
                        await this.DispatchEventToHandlerAsync(ComponentGroup.Events, EventType.FocusIn, Entity);
                    });
                }
                else
                {
                    _li.RemoveClass(ActiveClass);
                    _li.QuerySelector(ElementType.a.ToString()).RemoveClass(ActiveClass);
                    Task.Run(async () =>
                    {
                        await this.DispatchEventToHandlerAsync(ComponentGroup.Events, EventType.FocusOut, Entity);
                    });
                }
            }
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                if (_li is null)
                {
                    return;
                }

                if (value)
                {
                    _li.SetAttribute("disabled", "");
                }
                else
                {
                    _li.RemoveAttribute("disabled");
                }
            }
        }

        public bool Hidden
        {
            get => base.Disabled;
            set
            {
                _li.Hidden = value;
                if (Show)
                {
                    Show = false;
                }
                else
                {
                    return;
                }

                if (NextTab.HasRendered)
                {
                    NextTab.Focus();
                }
                else
                {
                    NextTab.RenderTabContent();
                    NextTab.Focus();
                }
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Task.Run(CountBadge);
            base.UpdateView(force, dirty, componentNames);
        }
    }
}
