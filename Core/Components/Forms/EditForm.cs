using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Framework;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.Structs;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components.Forms
{
    public partial class EditForm : EditableComponent
    {
        public const string NotDirtyMessage = "Data was not changed!";
        public const string ExpiredDate = "ExpiredDate";
        public const string BtnExpired = "btnExpired";
        public const string BtnSave = "btnSave";
        public const string BtnSend = "btnSend";
        public const string BtnApprove = "btnApprove";
        public const string BtnReject = "btnReject";
        public const string StatusIdField = "StatusId";
        public const string BtnCancel = "btnCancel";
        public const string BtnPrint = "btnPrint";
        public const string BtnPreview = "btnPreview";
        private const string SpecialEntryPoint = "entry";

        public static EditForm LastForm;

        protected readonly string _entity;
        protected URLSearchParams UrlSearch;
        protected Entity _entityEnum;
        public List<TabGroup> TabGroup;
        protected ConfirmDialog _confirm;
        private string _title;
        private string _icon;
        protected HTMLElement TitleElement;
        protected HTMLElement IconElement;
        public string CurrentUserId { get; private set; }
        public string RegionId { get; set; }
        public string CenterIds { get; private set; }
        public string RoleIds { get; private set; }
        public string CostCenterId { get; private set; }
        public string RoleNames { get; private set; }

        public bool ShouldUpdateParentForm { get; set; }
        public DateTime Now => DateTime.Now;
        public string FeatureConnKey => Feature?.ConnKey ?? Client.ConnKey;
        public Action<bool> AfterSaved;
        public Func<bool> BeforeSaved;
        public bool IsEditMode => Entity != null && Entity[IdField].As<int>() > 0;
        public static WebSocketClient NotificationClient;
        protected ListView _currentListView;
        protected Component _componentCoppy;
        private HTMLElement InnerEntry => Document.GetElementById("entry");
        public string EntityName => Feature?.EntityName;

        public bool IsLock { get; private set; }

        public HashSet<ListView> ListViews { get; set; } = new HashSet<ListView>();

        public Vendor UserVendor => Client.Token?.Vendor;
        public bool ShouldLoadEntity { get; set; }
        public Feature Feature { get; set; }
        public virtual string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                if (IconElement != null)
                {
                    Html.Take(IconElement).IconForSpan(value);
                }
            }
        }
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                if (TitleElement != null)
                {
                    TitleElement.InnerHTML = null;
                    Html.Take(TitleElement).IText(value);
                }
            }
        }

        public bool Public { get; set; }
        public EditableComponent OpenFrom { get; set; }
        public EditForm ParentForm { get; set; }
        public static EditForm LayoutForm { get; set; }
        public string ReasonOfChange { get; set; }
        public static bool Portal { get; internal set; }
        public bool Popup { get; set; }

        public EditForm(string entity) : base(null)
        {
            UrlSearch = new URLSearchParams(Window.Location.Search);
            if (entity is null)
            {
                return;
            }
            ListViews = new HashSet<ListView>();
            _entity = entity;
            var entityType = Type.GetType(Client.ModelNamespace + entity);
            if (entityType != null)
            {
                Entity = Activator.CreateInstance(entityType);
            }
            Window.AddEventListener(EventType.Resize, ResizeHandler);
            LayoutForm = LayoutForm ?? new EditForm(null);
        }

        public PatchVM GetPatchEntity()
        {
            var shouldGetAll = EntityId is null;
            var details = FilterChildren(child =>
            {
                return child is EditableComponent editable && !(child is Button)
                    && (shouldGetAll || editable.Dirty) && child.GuiInfo != null
                    && child.GuiInfo.FieldName.HasNonSpaceChar();
            }, x => x is ListView || x.AlwaysValid || !x.PopulateDirty)
            .DistinctBy(x => x.GuiInfo.Id)
            .SelectMany(child =>
            {
                if (child[nameof(PatchDetail)] is Func<PatchDetail[]> fn)
                {
                    return fn.Call(child) as PatchDetail[];
                }
                var value = Utils.GetPropValue(child.Entity, child.FieldName);
                var propType = child.Entity.GetType().GetComplexPropType(child.FieldName, child.Entity);
                var patch = new PatchDetail
                {
                    Label = child.Label,
                    Field = child.FieldName,
                    OldVal = (child.OldValue != null && propType.IsDate()) ? child.OldValue.ToString().DateConverter() : child.OldValue?.ToString(),
                    Value = (value != null && propType.IsDate()) ? value.ToString().DateConverter() : !EditForm.Feature.IgnoreEncode ? value?.ToString().Trim().EncodeSpecialChar() : value?.ToString().Trim(),
                };
                return new PatchDetail[] { patch };
            }).DistinctBy(x => x.Field).ToList();
            AddIdToPatch(details);
            return new PatchVM { Changes = details, Table = Feature.EntityName, QueueName = QueueName, CacheName = CacheName };
        }

        public virtual Task<bool> SavePatch(object entity = null)
        {
            if (!Dirty)
            {
                Toast.Warning(NotDirtyMessage);
                return Task.FromResult(false);
            }
            var tcs = new TaskCompletionSource<bool>();
            IsFormValid().Done(valid =>
            {
                if (valid) ValidSavePatch().Done(sucess => tcs.TrySetResult(sucess));
                else tcs.TrySetResult(false);
            });
            return tcs.Task;
        }

        private Task<bool> ValidSavePatch()
        {
            var tcs = new TaskCompletionSource<bool>();
            var pathModel = GetPatchEntity();
            EntityId = pathModel.EntityId;
            var details = UpdateIndependantGridView() ?? new List<PatchVM>();
            details.Insert(0, pathModel);
            BeforeSaved?.Invoke();
            Client.Instance.PatchAsync(details).Done(rs =>
            {
                if (rs == 0)
                {
                    Toast.Warning("Dữ liệu của bạn chưa được lưu vui lòng nhập lại!");
                    tcs.TrySetResult(false);
                    return;
                }
                if (Feature.DeleteTemp)
                {
                    DeleteGridView();
                }
                Toast.Success($"The data was saved");
                Dirty = false;
                AfterSaved?.Invoke(true);
                tcs.TrySetResult(true);
                return;
            }).Catch(e =>
            {
                tcs.TrySetResult(false);
                Toast.Warning(e.Message);
            });
            return tcs.Task;
        }

        private ListView[] GetDirtyGrid()
        {
            return ListViews
                .Where(x => x.GuiInfo.IdField.HasAnyChar() && x.GuiInfo.CanAdd)
                .Where(x => x.FilterChildren<EditableComponent>(com => com._dirty, com => !com.PopulateDirty).Any())
                .ToArray();
        }

        private ListView[] GetDeleteGrid()
        {
            return ListViews
                .Where(x => x.GuiInfo.Id.HasAnyChar())
                .Where(x => x.DeleteTempIds.Any())
                .ToArray();
        }

        private List<PatchVM> UpdateIndependantGridView()
        {
            var dirtyGrid = GetDirtyGrid();
            if (dirtyGrid.Nothing())
            {
                return null;
            }
            var id = EntityId;
            return dirtyGrid.SelectMany(x => x.GetPatches()).ToList();
        }

        private void DeleteGridView()
        {
            var dirtyGrid = GetDeleteGrid();
            if (dirtyGrid.Nothing())
            {
                return;
            }
            foreach (var item in dirtyGrid)
            {
                Client.Instance.HardDeleteAsync(item.DeleteTempIds.ToArray(), item.GuiInfo.RefName, item.ConnKey)
                .Done(deleteSuccess =>
                {
                    if (!deleteSuccess)
                    {
                        Toast.Warning("Lỗi xóa chi tiết vui lòng kiểm tra lại");
                        return;
                    }
                    item.RowAction(x =>
                    {
                        if (item.DeleteTempIds.Contains(x.EntityId))
                        {
                            x.Dispose();
                        }
                    });
                    item.DeleteTempIds.Clear();
                });
            }
        }

        public Task<bool> IsFormValid(bool showMessage = true, Func<EditableComponent, bool> predicate = null, Func<EditableComponent, bool> ignorePredicate = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (predicate == null)
            {
                predicate = (EditableComponent x) => x.Children.Nothing();
            }
            if (ignorePredicate == null)
            {
                ignorePredicate = (EditableComponent x) => x.AlwaysValid || x.EmptyRow;
            }
            var allValid = FilterChildren(
                predicate: predicate,
                ignorePredicate: ignorePredicate
            ).ForEachAsync(x => x.ValidateAsync());
            allValid.Done((validities) =>
            {
                var res = validities.ToArray();
                var invalid = res.Any(x => !x.IsValid);
                if (!invalid)
                {
                    tcs.TrySetResult(true);
                    return;
                }
                if (showMessage)
                {
                    var invalidCom = res.Where(x => !x.IsValid).ToArray();
                    invalidCom.ForEach(x => { x.Disabled = false; });
                    invalidCom.FirstOrDefault().Focus();
                    var message = string.Join(Utils.BreakLine, invalidCom.SelectMany(x => x.ValidationResult.Values));
                    Toast.Warning(message);
                }
                tcs.TrySetResult(false);
            });
            return tcs.Task;
        }

        protected List<ComponentGroup> BuildTree(List<ComponentGroup> componentGroup)
        {
            var componentGroupMap = componentGroup.ToDictionary(x => x.Id);
            ComponentGroup parent;
            foreach (var item in componentGroup)
            {
                if (item.IsVertialTab && Element.ClientWidth < SmallScreen)
                {
                    item.IsVertialTab = false;
                }

                if (item.ParentId is null)
                {
                    continue;
                }

                if (!componentGroupMap.ContainsKey(item.ParentId))
                {
                    Console.WriteLine($"The parent key {item.ParentId} of {item.Name} doesn't exist");
                    continue;
                }
                parent = componentGroupMap[item.ParentId];
                if (parent.InverseParent == null)
                {
                    parent.InverseParent = new List<ComponentGroup>();
                }

                if (!parent.InverseParent.Contains(item))
                {
                    parent.InverseParent.Add(item);
                }

                item.Parent = parent;
            }
            foreach (var item in componentGroup)
            {
                if (item.Component == null || !item.Component.Any())
                {
                    continue;
                }

                foreach (var ui in item.Component)
                {
                    ui.ComponentGroup = item;
                }
                if (item.InverseParent != null)
                {
                    item.InverseParent = item.InverseParent.OrderBy(x => x.Order).ToList();
                }
            }
            componentGroup.ForEach(x => CalcItemInRow(x.InverseParent.ToList()));
            var res = componentGroup.Where(x => x.ParentId is null);
            if (res.Nothing())
            {
                Console.WriteLine("No component group is root component. Wrong feature name or the configuration is wrong");
            }
            return res.ToList();
        }

        private void CalcItemInRow(List<ComponentGroup> componentGroup)
        {
            var cumulativeColumn = 0;
            var itemInRow = 0;
            var startRowIndex = 0;
            for (var i = 0; i < componentGroup.Count; i++)
            {
                var group = componentGroup[i];
                var parentInnerCol = GetInnerColumn(group.Parent);
                var outerCol = GetOuterColumn(group);
                if (parentInnerCol <= 0)
                {
                    continue;
                }

                itemInRow++;
                cumulativeColumn += outerCol;
                if (cumulativeColumn % parentInnerCol == 0)
                {
                    var sameRow = i;
                    while (sameRow >= startRowIndex)
                    {
                        componentGroup[sameRow].ItemInRow = itemInRow;
                        sameRow--;
                    }
                    itemInRow = 0;
                    startRowIndex = i;
                }
            }
        }

        public override void Render()
        {
            if (Portal)
            {
                ParentForm = ParentForm ?? LastForm;
            }
            LoadFeatureAndRender();
            LastForm = this;
        }

        protected virtual void LoadFeatureAndRender(Action callback = null)
        {
            var featureTask = Feature != null ? Task.FromResult(Feature)
                : ComponentExt.LoadFeature(FeatureConnKey, Name);
            var entityTask = LoadEntity();
            Task.WhenAll(featureTask, entityTask).Done(() =>
                FeatureLoaded(featureTask.Result, entityTask.Result, callback));
        }

        private void FeatureLoaded(Feature feature, object entity, Action loadedCallback = null)
        {
            if (feature.LayoutId is null || InnerEntry != null)
            {
                LayoutLoaded(feature, entity, loadedCallback: loadedCallback);
                return;
            }
            ComponentExt.LoadFeature(Client.ConnKey, null, id: feature.LayoutId)
            .Done(layout =>
            {
                LayoutLoaded(feature, entity, layout, loadedCallback);
            });
        }

        private void LayoutLoaded(Feature feature, object entity, Feature layout = null, Action loadedCallback = null)
        {
            var token = Client.Token;
            Entity.CopyPropFrom(entity);
            SetFeatureProperties(feature);
            CurrentUserId = token?.UserId;
            RegionId = token?.RegionId;
            CenterIds = token?.CenterIds != null ? string.Join(",", token.CenterIds) : string.Empty;
            RoleIds = token?.RoleIds != null ? string.Join(",", token.RoleIds) : string.Empty;
            CostCenterId = token?.CostCenterId;
            RoleNames = token?.RoleNames != null ? string.Join(",", token.RoleNames) : string.Empty;
            var groupTree = BuildTree(feature.ComponentGroup.ToList().OrderBy(x => x.Order).ToList());
            Element = RenderTemplate(layout, feature);
            SetFeatureStyleSheet(feature.StyleSheet);
            RenderTabOrSection(groupTree);
            ResizeHandler();
            LockUpdate();
            Html.Take(Element).TabIndex(-1).Trigger(EventType.Focus)
                .Event(EventType.FocusIn, () => this.DispatchEvent(Feature.Events, EventType.FocusIn, Entity).Done())
                .Event(EventType.KeyDown, (e) => KeyDownIntro(e).Done())
                .Event(EventType.FocusOut, () => this.DispatchEvent(Feature.Events, EventType.FocusOut, Entity).Done());
            DOMContentLoaded?.Invoke();
            loadedCallback?.Invoke();
            this.DispatchEvent(Feature.Events, EventType.DOMContentLoaded, Entity).Done();
        }

        private async Task KeyDownIntro(Event evt)
        {
            if (evt.KeyCode() == (int)KeyCodeEnum.H && evt.CtrlOrMetaKey())
            {
                evt.PreventDefault();
                await Client.LoadScript("https://unpkg.com/intro.js/intro.js");
                var sql = new SqlViewModel
                {
                    ComId = "Intro",
                    Action = "GetByFeatureId",
                    Params = JSON.Stringify(new { id = Feature.Id })
                };
                var xhr = new XHRWrapper
                {
                    Url = Utils.UserSvc,
                    Method = HttpMethod.POST,
                    IsRawString = true,
                    Value = JSON.Stringify(sql)
                };
                var intro = await Client.Instance.SubmitAsync<dynamic[]>(xhr);
                var script = @"(x) => {
                                introJs().setOptions({
                                  steps: [
                                  {
                                    intro: ""Hướng dẫn sử dụng chức năng!""
                                  }";
                foreach (var item in intro)
                {
                    script += @",{
                                    intro: '" + item.Label + @"',
                                    element: Core.Components.Extensions.ComponentExt.FindComponentByName(Core.Components.EditableComponent, x, '" + item.FieldName + @"').Element
                                  }";
                }
                script += @"]}).start();}";
                if (Utils.IsFunction(script, out Function fn))
                {
                    fn.Call(this, this);
                }
            }
        }

        private HTMLElement RenderTemplate(Feature layout, Feature feature)
        {
            HTMLElement entryPoint = Document.GetElementById(SpecialEntryPoint) ?? Document.GetElementById("template") ?? Element;
            if (ParentForm != null && Portal && !Popup)
            {
                ParentForm.Element = null;
                ParentForm.Dispose();
                ParentForm = null;
            }
            if (layout != null)
            {
                var root = Document.GetElementById("template");
                if (root != null)
                {
                    Html.Take(root).InnerHTML(layout.Template);
                    var style = Document.CreateElement(ElementType.style.ToString());
                    style.AppendChild(new Text(layout.StyleSheet));
                    root.AppendChild(style);
                    BindingTemplate(root, this, isLayout: true);
                    entryPoint = root.FilterElement(x => x.Id == SpecialEntryPoint).FirstOrDefault();
                    ResetEntryPoint(entryPoint);
                }
            }
            else
            {
                entryPoint.InnerHTML = null;
            }
            if (!feature.Template.HasAnyChar())
            {
                return entryPoint;
            }
            Html.Take(entryPoint).InnerHTML(feature.Template);
            BindingTemplate(entryPoint, this);
            var innerEntry = entryPoint.FilterElement(x => x.Id == "inner-entry").FirstOrDefault();
            ResetEntryPoint(innerEntry);
            var res = innerEntry ?? entryPoint;
            if (res.Style.Display.ToString() == Display.None.ToString())
            {
                res.Style.Display = string.Empty;
            }
            return res;
        }

        private void ResetEntryPoint(HTMLElement entryPoint)
        {
            if (entryPoint != null)
            {
                entryPoint.InnerHTML = string.Empty;
            }
        }

        public void BindingTemplate(HTMLElement ele, EditableComponent parent, bool isLayout = false, object entity = null,
            Func<HTMLElement, Component, EditableComponent, bool, object, EditableComponent> factory = null, HashSet<HTMLElement> visited = null)
        {
            if (visited is null)
            {
                visited = new HashSet<HTMLElement>();
            }
            if (ele is null || visited.Contains(ele))
            {
                return;
            }
            visited.Add(ele);
            if (ele.Children.Length == 0 && RenderCellText(ele, entity, isLayout) != null)
            {
                return;
            }
            var meta = ResolveMeta(ele);
            var newCom = factory?.Invoke(ele, meta, parent, isLayout, entity)
                ?? BindingCom(ele, meta, parent, isLayout, entity);
            parent = newCom is Section ? newCom : parent;
            ele.Children.SelectForEach(child => BindingTemplate(child, parent, isLayout, entity, factory, visited));
        }

        private Component ResolveMeta(HTMLElement ele)
        {
            Component component = null;
            var id = ele.Dataset[IdField.ToLowerCase()];
            if (id != null)
            {
                component = Feature.Component.FirstOrDefault(x => x.Id == id);
            }
            foreach (var prop in typeof(Component).GetProperties().Where(x => x.CanRead && x.CanWrite))
            {
                var value = ele.Dataset[prop.Name.ToLower()];
                if (value == null)
                {
                    continue;
                }
                object propVal = null;
                try
                {
                    propVal = prop.PropertyType == typeof(string) ? value : Utils.ChangeType(value, prop.PropertyType);
                    component = component ?? new Component();
                    component.SetPropValue(prop.Name, propVal);
                }
                catch
                {
                    continue;
                }
            }

            return component;
        }

        private static Label RenderCellText(HTMLElement ele, object entity, bool isLayout)
        {
            var text = ele.TextContent?.Trim();
            if (text.HasAnyChar() && text.StartsWith("{") && text.EndsWith("}"))
            {
                var cellText = new Label(new Component
                {
                    FieldName = text.SubStrIndex(1, text.Length - 1)
                }, ele)
                { Entity = entity };
                if (isLayout && LayoutForm != null)
                {
                    LayoutForm.AddChild(cellText);
                }
                else
                {
                    cellText.Render();
                }
                return cellText;
            }
            return null;
        }

        public EditableComponent BindingCom(HTMLElement ele, Component com, EditableComponent parent, bool isLayout, object entity)
        {
            EditableComponent child = null;
            if (ele is null)
            {
                return null;
            }
            if (com is null || com.ComponentType.IsNullOrEmpty())
            {
                return null;
            }
            if (com.ComponentType == nameof(Section))
            {
                child = new Section(ele)
                {
                    GuiInfo = com,
                };
            }
            else if (child is null)
            {
                child = ComponentFactory.GetComponent(com, this, ele);
            }
            child.ParentElement = child.ParentElement ?? ele;
            child.Entity = entity ?? child.EditForm?.Entity ?? LayoutForm.Entity;
            if (isLayout)
            {
                child.EditForm = parent as EditForm;
                child.Render();
                LayoutForm.Children.Add(child);
                child.ToggleShow(com.ShowExp);
                child.ToggleDisabled(com.DisabledExp);
            }
            else
            {
                parent.AddChild(child);
            }
            return child;
        }

        protected virtual Task<object> LoadEntity()
        {
            if (!ShouldLoadEntity || EntityId.IsNullOrWhiteSpace())
            {
                return Task.FromResult(null as object);
            }
            var tcs = new TaskCompletionSource<object>();
            Client.Instance.GetByIdAsync(EntityName, Feature?.ConnKey ?? Client.ConnKey, EntityId).Done(ds =>
            {
                if (ds.Nothing()) tcs.TrySetResult(null);
                else tcs.TrySetResult(ds[0]);
            });
            return tcs.Task;
        }

        private void LockUpdate()
        {
            var generalRule = Feature.FeaturePolicy.Where(x => x.RecordId.IsNullOrEmpty()).ToArray();
            if (!Feature.IsPublic &&
                (generalRule.All(x => !x.CanWrite)
                || !Utils.IsOwner(Entity) && generalRule.All(x => !x.CanWriteAll)))
            {
                LockUpdateButCancel();
            }
        }

        protected virtual void LockUpdateButCancel()
        {
            Disabled = true;
            this.SetDisabled(false, BtnCancel, BtnPrint);
        }

        private void SetFeatureProperties(Feature feature)
        {
            if (feature is null)
            {
                return;
            }

            Feature = feature;
            Element.AddClass(feature.ClassName);
            Html.Take(Element).Style(feature.Style);
            if (Icon.IsNullOrEmpty())
            {
                Icon = feature.Icon;
            }
            if (Title.IsNullOrEmpty())
            {
                Title = feature.Label;
            }
        }

        private void SetFeatureStyleSheet(string styleSheet)
        {
            if (styleSheet.IsNullOrWhiteSpace())
            {
                return;
            }
            var style = Document.CreateElement(ElementType.style.ToString()) as HTMLStyleElement;
            style.AppendChild(new Text(styleSheet));
            style.SetAttribute("source", "feature");
            Element.AppendChild(style);
        }

        public void RenderTabOrSection(IEnumerable<ComponentGroup> componentGroup)
        {
            foreach (var group in componentGroup.OrderBy(x => x.Order))
            {
                group.Disabled = Disabled || group.Disabled;
                if (group.IsTab)
                {
                    Section.RenderTabGroup(this, group);
                }
                else
                {
                    Section.RenderSection(this, group);
                }
            }
        }

        public int GetInnerColumn(ComponentGroup group)
        {
            if (group is null)
            {
                return 0;
            }

            var screenWidth = Element.ClientWidth;
            int? res;
            if (screenWidth < ExSmallScreen && group.XsCol > 0)
            {
                res = group.XsCol;
            }
            else if (screenWidth < SmallScreen && group.SmCol > 0)
            {
                res = group.SmCol;
            }
            else if (screenWidth < MediumScreen && group.Column > 0)
            {
                res = group.Column;
            }
            else if (screenWidth < LargeScreen && group.LgCol > 0)
            {
                res = group.LgCol;
            }
            else if (screenWidth < ExLargeScreen && group.XlCol > 0)
            {
                res = group.XlCol;
            }
            else
            {
                res = group.XxlCol ?? group.Column;
            }

            return res ?? 0;
        }

        internal int GetOuterColumn(ComponentGroup group)
        {
            var screenWidth = Element.ClientWidth;
            int? res;
            if (screenWidth < ExSmallScreen && group.XsOuterColumn > 0)
            {
                res = group.XsOuterColumn;
            }
            else if (screenWidth < SmallScreen && group.SmOuterColumn > 0)
            {
                res = group.SmOuterColumn;
            }
            else if (screenWidth < MediumScreen && group.OuterColumn > 0)
            {
                res = group.OuterColumn;
            }
            else if (screenWidth < LargeScreen && group.LgOuterColumn > 0)
            {
                res = group.LgOuterColumn;
            }
            else if (screenWidth < ExLargeScreen && group.XlOuterColumn > 0)
            {
                res = group.XlOuterColumn;
            }
            else
            {
                res = group.XxlOuterColumn ?? group.OuterColumn;
            }

            return res ?? 0;
        }

        public virtual void SysConfigMenu(Event e, Component component, ComponentGroup group)
        {
            if (!Client.SystemRole)
            {
                return;
            }
            var menuItems = new List<ContextMenuItem>()
            {
                new ContextMenuItem { Icon = "fas fa-link mt-2", Text = "Add Link", Click = AddComponent, Parameter = new { group = group, action = "AddLink" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add Input", Click = AddComponent, Parameter = new { group = group, action = "AddInput" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add Timepicker", Click = AddComponent, Parameter = new { group = group, action = "AddTimepicker" } },
                new ContextMenuItem { Icon = "fas fa-lock mt-2", Text = "Add Password", Click = AddComponent, Parameter = new { group = group, action = "AddPassword" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add Label", Click = AddComponent, Parameter = new { group = group, action = "AddLabel" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add Textarea", Click = AddComponent, Parameter = new { group = group, action = "AddTextarea" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add Dropdown", Click = AddComponent, Parameter = new { group = group, action = "AddDropdown" } },
                new ContextMenuItem { Icon = "fas fa-images mt-2", Text = "Add Image", Click = AddComponent, Parameter = new { group = group, action = "AddImage" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add GridView", Click = AddComponent, Parameter = new { group = group, action = "AddGridView" } },
                new ContextMenuItem { Icon = "fas fa-plus-circle mt-2", Text = "Add ListView", Click = AddComponent, Parameter = new { group = group, action = "AddListView" } },
            };
            e.PreventDefault();
            e.StopPropagation();
            var ctxMenu = ContextMenu.Instance;
            ctxMenu.Top = e.Top();
            ctxMenu.Left = e.Left();
            ctxMenu.MenuItems = new List<ContextMenuItem>
            {
                    component is null ? null : new ContextMenuItem { Icon = "fal fa-cog", Text = "Tùy chọn dữ liệu", Click = ComponentProperties, Parameter = component },
                    component is null ? null : new ContextMenuItem { Icon = "fal fa-clone", Text = "Sao chép", Click = CopyComponent, Parameter = component },
                    new ContextMenuItem { Icon = "fal fa-cogs", Text = "Thêm Component", MenuItems = menuItems },
                    new ContextMenuItem { Icon = "fal fa-cogs", Text = "Tùy chọn vùng dữ liệu", Click = SectionProperties, Parameter = group },
                    new ContextMenuItem { Icon = "fal fa-folder-open", Text = "Thiết lập chung", Click = FeatureProperties },
                    new ContextMenuItem { Icon = "fal fa-clone", Text = "Clone feature", Click = CloneFeature, Parameter = Feature },
            };
            ctxMenu.Render();
        }

        public void CloneFeature(object ev)
        {
            var feature = ev as Feature;
            var confirmDialog = new ConfirmDialog
            {
                Content = "Bạn có muốn clone feature này?",
                Title = "Xác nhận"
            };
            confirmDialog.YesConfirmed += () =>
            {
                var sql = new SqlViewModel
                {
                    ComId = "Feature",
                    Action = "Clone",
                    Ids = new string[] { feature.Id }
                };
                Client.Instance.SubmitAsync<bool>(new XHRWrapper
                {
                    IsRawString = true,
                    Url = Utils.UserSvc,
                    Method = HttpMethod.POST,
                    Value = JSON.Stringify(sql)
                }).Done();
            };
            AddChild(confirmDialog);
        }

        public FeaturePolicy[] GetElementPolicies(string[] recordIds, string entityId = Utils.ComponentGroupId) // Default of component group
        {
            var hasHidden = Feature.FeaturePolicy
                    .Where(x => x.RoleId.HasAnyChar() || (x.UserId.HasAnyChar() && Client.Token.UserId == x.UserId))
                    .Where(x => x.EntityId == entityId && recordIds.Contains(x.RecordId))
                    .ToArray();
            return hasHidden;
        }

        public FeaturePolicy[] GetGridPolicies(string[] recordIds, string entityId = Utils.ComponentGroupId) // Default of component group
        {
            var hasHidden = Feature.FeaturePolicy
                    .Where(x => x.RoleId.HasAnyChar() || (x.UserId.HasAnyChar() && Client.Token.UserId == x.UserId))
                    .Where(x => x.EntityId == entityId && recordIds.Contains(x.RecordId))
                    .ToArray();
            return hasHidden;
        }

        public FeaturePolicy[] GetElementPolicies(string recordId, string entityId = Utils.ComponentId) // Default of component
        {
            var hasHidden = Feature.FeaturePolicy
                    .Where(x => x.RoleId.HasAnyChar() || (x.UserId.HasAnyChar() && Client.Token.UserId == x.UserId))
                    .Where(x => x.EntityId == entityId && recordId == x.RecordId)
                    .ToArray();
            return hasHidden;
        }

        public FeaturePolicy[] GetGridPolicies(string recordId, string entityId = Utils.ComponentId) // Default of component
        {
            var hasHidden = Feature.FeaturePolicy
                    .Where(x => x.RoleId.HasAnyChar() || (x.UserId.HasAnyChar() && Client.Token.UserId == x.UserId))
                    .Where(x => x.EntityId == entityId && recordId == x.RecordId)
                    .ToArray();
            return hasHidden;
        }

        public void ComponentProperties(object arg)
        {
            var editor = new ComponentBL()
            {
                Entity = arg,
                ParentElement = Element,
                OpenFrom = this.FindClosest<EditForm>(),
            };
            AddChild(editor);
        }

        public void CopyComponent(object arg)
        {
            var component = arg.CastProp<Component>();
            _componentCoppy = component;
        }

        public void AddComponent(object arg)
        {
            var action = arg["action"] as string;
            var componentGroup = arg["group"].CastProp<ComponentGroup>();
            var com = new Component();
            var childComponent = Feature.ComponentGroup.FirstOrDefault(x => x.Id == componentGroup.Id);
            var lastOrder = childComponent.Component.Max(x => x.Order);

            switch (action)
            {
                case "AddLink":
                    com.ComponentType = nameof(Link);
                    break;
                case "AddInput":
                    com.ComponentType = "Input";
                    break;
                case "AddTimepicker":
                    com.ComponentType = nameof(Timepicker);
                    break;
                case "AddPassword":
                    com.ComponentType = "Password";
                    break;
                case "AddTextarea":
                    com.ComponentType = "Textarea";
                    break;
                case "AddDropdown":
                    com.ComponentType = nameof(SearchEntry);
                    break;
                case "AddImage":
                    com.ComponentType = "Image";
                    break;
                case "AddGridView":
                    com.ComponentType = nameof(GridView);
                    break;
                case "AddListView":
                    com.ComponentType = nameof(ListView);
                    break;
                case "AddLabel":
                    com.ComponentType = "Label";
                    com.Visibility = true;
                    com.Order = lastOrder;
                    com.ComponentGroupId = componentGroup.Id;
                    com.FieldName = "";
                    com.ShowLabel = true;

                    var confirm = new ConfirmDialog
                    {
                        NeedAnswer = true,
                        ComType = nameof(Textbox),
                        Content = "Hãy nhập label?",
                    };

                    confirm.Render();
                    confirm.YesConfirmed += () =>
                    {
                        com.Label = confirm.Textbox?.Text;
                        SaveNewComponent(componentGroup, com);
                    };
                    return;
            }
            com.Id = Uuid7.Id25();
            com.Visibility = true;
            com.Label = "";
            com.ComponentGroupId = componentGroup.Id;
            com.Order = lastOrder;
            com.FieldName = "";
            SaveNewComponent(componentGroup, com);
        }

        private void SaveNewComponent(ComponentGroup componentGroup, Component com)
        {
            Client.Instance.PatchAsync(com.MapToPatch()).Done(x =>
            {
                UpdateRender(com, componentGroup);
                Toast.Success("Tạo thành công!");
            });
        }

        private void UpdateRender(Component component, ComponentGroup componentGroup)
        {
            var section = this.FindComponentByName<Section>(componentGroup.Name);
            var childComponent = ComponentFactory.GetComponent(component, EditForm);
            childComponent.ParentElement = section.Element;
            section.AddChild(childComponent);
        }

        public void SectionProperties(object arg)
        {
            var group = arg.CastProp<ComponentGroup>();
            group.InverseParent = null;
            var editor = new ComponentGroupBL
            {
                Entity = group,
                ParentElement = Element,
                OpenFrom = this.FindClosest<EditForm>()
            };
            AddChild(editor);
        }

        public void FeatureProperties(object arg)
        {
            var editor = new FeatureDetailBL()
            {
                Entity = Feature,
                ParentElement = this.FindClosest<EditForm>().Element,
                OpenFrom = this.FindClosest<EditForm>(),
            };
            AddChild(editor);
        }

        public void SecurityRecord(object arg)
        {
            var security = new SecurityBL
            {
                Entity = arg,
                ParentElement = Element
            };
            TabEditor.AddChild(security);
        }

        public virtual void Cancel()
        {
            DirtyCheckAndCancel();
        }

        public virtual void CancelWithoutAsk() => Dispose();

        public override void Dispose()
        {
            Window.RemoveEventListener(EventType.Resize, ResizeHandler);
            base.Dispose();
        }

        public virtual void DirtyCheckAndCancel() => DirtyCheckAndCancel(null);
        public virtual void DirtyCheckAndCancel(Action closeCallback)
        {
            if (!Dirty)
            {
                Dispose();
                return;
            }
            _confirm = new ConfirmDialog()
            {
                Content = "Bạn có muốn lưu dữ liệu trước khi đóng?",
                OpenEditForm = this
            };
            _confirm.YesConfirmed += () =>
            {
                _confirm.OpenEditForm.SavePatch().Done(success =>
                {
                    if (!success)
                    {
                        Toast.Warning("Update data failed");
                        return;
                    }
                    _confirm.OpenEditForm.Dispose();
                    _confirm.Dispose();
                });
            };
            _confirm.NoConfirmed += () =>
            {
                _confirm.OpenEditForm.Dispose();
                _confirm.Dispose();
            };
            _confirm.IgnoreCancelButton = true;
            _confirm.Render();
        }

        public virtual Task<bool> EmailPdf(EmailVM email, params string[] pdfSelector)
        {
            if (email is null)
            {
                throw new ArgumentNullException(nameof(email));
            }
            var tcs = new TaskCompletionSource<bool>();
            if (pdfSelector.HasElement())
            {
                var pdfText = pdfSelector.Select(x =>
                {
                    var ele = Element.QuerySelector(x) as HTMLElement;
                    return PrintSection(ele, false);
                });
                email.PdfText.AddRange(pdfText);
            }
            Client.Instance
                .PostAsync<bool>(email, "/user/EmailAttached", allowNested: true)
                .Done(sucess =>
                {
                    Toast.Success("Send email success!");
                    tcs.TrySetResult(sucess);
                }).Catch(e =>
                {
                    Toast.Success("Error occurs while sending email!");
                    tcs.TrySetException(e);
                });
            return tcs.Task;
        }

        public virtual void Print(string selector = ".printable")
        {
            var printable = Element.QuerySelector(selector) as HTMLElement;
            PrintSection(printable);
        }

        public string PrintSection(HTMLElement ele, bool openWindow = true, List<string> styles = null, bool printPreview = false, Component component = null)
        {
            if (ele is null)
            {
                return null;
            }
            if (!openWindow)
            {
                return ele.InnerHTML;
            }
            var printWindow = Window.Open("", "_blank");
            printWindow.Document.Body.InnerHTML = ele.InnerHTML;
            printWindow.Document.Close();
            if (printPreview)
            {
                Window.SetTimeout(() =>
                {
                    printWindow.AddEventListener(EventType.BeforePrint, e =>
                    {
                        var pageStyle = printWindow.Document.CreateElement(ElementType.style.ToString());
                        pageStyle.InnerHTML = component.Style;
                        printWindow.Document.Head.AppendChild(pageStyle);
                    });
                    printWindow.Print();
                    printWindow.AddEventListener(EventType.AfterPrint, async e =>
                    {
                        await this.DispatchEvent(component.Events, EventType.AfterPrint, this);
                    });
                    printWindow.AddEventListener(EventType.MouseMove, e => printWindow.Close());
                    printWindow.AddEventListener(EventType.Click, e => printWindow.Close());
                    printWindow.AddEventListener(EventType.KeyUp, e => printWindow.Close());
                }, 250);
            }
            return ele.InnerHTML;
        }

        public override void Focus()
        {
            var ele = this.FirstOrDefault(x => x.GuiInfo != null && x.GuiInfo.Focus);
            if (ele is null)
            {
                Element.Focus();
            }
            else
            {
                ele.Focus();
            }
            ResizeHandler();
        }

        protected void UpdateViewByName(params string[] fieldNames)
        {
            if (fieldNames.Nothing())
            {
                return;
            }

            this.FilterChildren(x =>
            {
                if (fieldNames.Contains(x.Name))
                {
                    x.UpdateView();
                }

                return false;
            });
        }

        protected virtual void ResizeHandler()
        {
            ResizeTabGroup();
            ResizeListView();
        }

        public void ResizeListView()
        {
            var visibleListView = ListViews.FirstOrDefault(x => !x.Element.Hidden());
            if (visibleListView is null)
            {
                return;
            }

            var allListView = visibleListView.Parent.Children.Where(x => typeof(ListView).IsAssignableFrom(x.GetType()));
            var responsive = allListView.Any(x => x.Name.Contains("Mobile"));
            allListView.ForEach(x =>
            {
                if (responsive)
                {
                    x.Show = IsSmallUp ? !x.Name.Contains("Mobile") : x.Name.Contains("Mobile");
                    if (x.Show)
                    {
                        _currentListView = x as ListView;
                    }
                }
                else
                {
                    _currentListView = x as ListView;
                }
            });
        }

        public void ResizeTabGroup()
        {
            if (Element != null && Element.HasClass("mobile") || TabGroup.Nothing())
            {
                return;
            }

            TabGroup.ForEach(tg =>
            {
                if (tg is null || tg.Element is null)
                {
                    return;
                }

                if (IsLargeUp && tg.ComponentGroup.Responsive && tg.Element.ParentElement.HasClass("tab-horizontal"))
                {
                    tg.Element.ParentElement.ReplaceClass("tab-horizontal", "tab-vertical");
                }
                else if (!IsLargeUp && tg.ComponentGroup.Responsive && tg.Element.ParentElement.HasClass("tab-vertical"))
                {
                    tg.Element.ParentElement.ReplaceClass("tab-vertical", "tab-horizontal");
                }
            });
        }

        public virtual void Delete()
        {
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn xóa không?",
            };
            confirm.Render();
            confirm.YesConfirmed += () =>
            {
                Client.Instance.HardDeleteAsync(new string[] { EntityId }, Feature.EntityName, Feature.ConnKey)
                .Done(success =>
                {
                    if (!success)
                    {
                        Toast.Warning("An error occurs while deleting data");
                        return;
                    }
                    Toast.Success("Delete data succeeded");
                    ParentForm.UpdateView(true);
                    Dispose();
                });
            };
        }

        public void SignIn()
        {
            Client.UnAuthorizedEventHandler?.Invoke(null);
        }

        public void SignOut()
        {
            var e = Window.Instance["event"] as Event;
            e.PreventDefault();
            Client.Instance.PostAsync<bool>(Client.Token, "user/SignOut")
            .Done(success =>
            {
                Toast.Success("Bạn đã đăng xuất!", 3000);
                Client.SignOutEventHandler?.Invoke();
                Client.Token = null;
                NotificationClient?.Close();
                Window.Location.Reload();
            });
        }
    }
}
