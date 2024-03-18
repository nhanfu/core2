using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.Structs;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public abstract class EditableComponent
    {
        protected const string IdField = "Id";
        public const int SmallScreen = 768;
        public const int ExSmallScreen = 567;
        public const int MediumScreen = 992;
        public const int LargeScreen = 1200;
        public const int ExLargeScreen = 1452;
        public string idGuid;
        public string Id { get; set; }
        public string FieldName
        {
            get => Meta?.FieldName; set
            {
                if (Meta != null) Meta.FieldName = value;
            }
        }
        public string Label
        {
            get => Meta?.Label; set
            {
                if (Meta != null) Meta.Label = value;
            }
        }
        public string ComponentType { get; set; }
        public string Name { get; set; }
        public EditableComponent Parent { get; set; }
        public Component Meta { get; set; }
        public List<EditableComponent> Children { get; set; } = new List<EditableComponent>();
        public EditableComponent FirstChild => Children.FirstOrDefault();
        /// <summary>
        /// The root element of tab or popup
        /// </summary>
        public virtual HTMLElement ParentElement { get; set; }
        public virtual HTMLElement Element { get; set; }
        public event Action Disposed;
        public Action DOMContentLoaded { get; set; }
        public virtual string MetaConn => Meta != null && Meta.MetaConn.IsNullOrWhiteSpace() ? Client.MetaConn : Meta?.MetaConn;
        public virtual string DataConn => Meta != null && Meta.DataConn.IsNullOrWhiteSpace() ? Client.DataConn : Meta?.DataConn;
        public string EntityId
        {
            get => Entity?[IdField]?.ToString();
            set
            {
                if (Entity != null) Entity[IdField] = value;
            }
        }
        public object Entity { get; set; }
        private bool _show = true;
        public bool IsSingleton { get; set; }
        public Action<bool> OnToggle;
        private EditForm editForm;
        private TabEditor rootTab;
        protected bool? emptyRow;

        public virtual bool EmptyRow
        {
            get
            {
                if (emptyRow is null)
                {
                    emptyRow = this.FindClosest<ListViewItem>()?.EmptyRow;
                }
                return emptyRow.Value;
            }
            set
            {
                emptyRow = value;
            }
        }

        public virtual bool Show
        {
            get => _show;
            set
            {
                Toggle(value);
            }
        }

        public void ListViewItemTab(Event e)
        {
            var code = e.KeyCodeEnum();
            var listViewItem = this.FindClosest<ListViewItem>();
            if (listViewItem is null)
            {
                return;
            }
            switch (code)
            {
                case KeyCodeEnum.Tab:
                    if (listViewItem != null)
                    {
                        var td = (e.Target as HTMLElement).Closest("td");
                        if (td != null && td.NextElementSibling is null)
                        {
                            e.PreventDefault();
                            var nextElement = listViewItem.Children.FirstOrDefault(x => x.Meta.Editable);
                            if (nextElement is null)
                            {
                                nextElement = listViewItem.Children.FirstOrDefault(x => x.Meta.Id.HasAnyChar());
                            }
                            FocusElement(nextElement);
                            return;
                        }
                        if (e.ShiftKey())
                        {
                            if (this is Label && Meta.ComponentType != null && !Meta.Editable && td != null && td.PreviousElementSibling != null)
                            {
                                e.PreventDefault();
                                var nextElement = listViewItem.Children.FirstOrDefault(x => x.Element.Closest("td") == td.PreviousElementSibling);
                                FocusElement(nextElement);
                                return;
                            }
                        }
                        else
                        {
                            if (this is Label && Meta.ComponentType != null && !Meta.Editable && td != null && td.NextElementSibling != null)
                            {
                                e.PreventDefault();
                                var nextElement = listViewItem.Children.FirstOrDefault(x => x.Element.Closest("td") == td.NextElementSibling);
                                FocusElement(nextElement);
                                return;
                            }
                        }
                    }
                    break;
                case KeyCodeEnum.Enter:
                    if (listViewItem != null && EditForm.Feature.CustomNextCell)
                    {
                        if (this is SearchEntry search)
                        {
                            if (search._gv != null && search._gv.Show)
                            {
                                return;
                            }
                        }
                        var td = Element.Closest("td");
                        if (Meta.ComponentType != null && td != null && td.PreviousElementSibling != null)
                        {
                            if (e.ShiftKey())
                            {
                                var nextElement = listViewItem.FilterChildren(x => x.Element.Closest("td") == td.PreviousElementSibling).FirstOrDefault();
                                if (nextElement != null)
                                {
                                    FocusElement(nextElement);
                                }
                            }
                            else
                            {
                                var nextElement = listViewItem.FilterChildren(x => x.Element.Closest("td") == td.NextElementSibling).FirstOrDefault();
                                if (nextElement is null)
                                {
                                    nextElement = listViewItem.Children.FirstOrDefault();
                                }
                                FocusElement(nextElement);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private static void FocusElement(EditableComponent nextElement)
        {
            nextElement.ParentElement.Focus();
            nextElement.Focus();
            if (nextElement.Element is HTMLInputElement html)
            {
                html.SelectionStart = 0;
                html.SelectionEnd = nextElement.GetValueText().Length;
            }
            else if (nextElement.Element is HTMLTextAreaElement htmlArea)
            {
                htmlArea.SelectionStart = 0;
                htmlArea.SelectionEnd = nextElement.GetValueText().Length;
            }
        }

        protected void Toggle(bool value)
        {
            if (Element is null)
            {
                return;
            }

            _show = value;
            if (!_show)
            {
                Element.Style.Display = "none";
                if (Meta != null && Meta.ShowLabel && Parent is Section && FieldName != null)
                {
                    Element.ParentElement.Style.Display = "none";
                    Element.ParentElement.PreviousElementSibling.Style.Display = "none";
                }
            }
            else
            {
                Element.Style.Display = "";
                if (Meta != null && Meta.ShowLabel && Parent is Section && FieldName != null)
                {
                    Element.ParentElement.Style.Display = "";
                    Element.ParentElement.PreviousElementSibling.Style.Display = "";
                }
            }

            OnToggle?.Invoke(_show);
        }

        public static readonly ValidationRule RequiredRule = new ValidationRule() { Rule = ValidationRule.Required, Message = "{0} không được để trống!" };

        /// <summary>
        /// The root tab of all component
        /// </summary>
        public virtual TabEditor TabEditor { get => rootTab ?? this.FindClosest<TabEditor>(x => !x.Popup); internal set => rootTab = value; }

        /// <summary>
        /// Current form
        /// </summary>
        public virtual EditForm EditForm { get => editForm ?? this.FindClosest<EditForm>(); internal set => editForm = value; }
        public Action<ObservableArgs> UserInput { get; set; }
        public static bool IsSmallUp => Document.DocumentElement.ClientWidth > SmallScreen;
        public static bool IsMediumUp => Document.DocumentElement.ClientWidth > MediumScreen;
        public static bool IsLargeUp => Document.DocumentElement.ClientWidth > LargeScreen;
        public bool AlwaysLogHistory { get; set; }
        public string OldValue { get; internal set; }
        public Dictionary<string, string> ValidationResult { get; set; }
        public string ClassName { get => Element.ClassName; set => Element.ClassName = value; }
        public Dictionary<string, ValidationRule> ValidationRules { get; set; }
        public virtual bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                SetDisableUI(value);
                Children?.ForEach(x =>
                {
                    if (x is EditableComponent editable)
                    {
                        editable.Disabled = value;
                    }
                });
            }
        }

        /// <summary>
        /// This flag is populated to all parent if it is dirty
        /// </summary>
        public virtual bool Dirty
        {
            get => _dirty && !AlwaysValid || FilterChildren<EditableComponent>(x => x._dirty, x => !x.PopulateDirty || x.AlwaysValid).Any();
            set
            {
                UpdateDirty(value);
            }
        }

        public bool AlwaysValid { get; set; }

        public bool IsValid => ValidationResult.Count == 0;

        public bool PopulateDirty { get; set; } = true;

        public string CacheName
        {
            get
            {
                string exp = this is EditForm form ? form.Feature?.CacheName : Meta?.CacheName;
                if (exp.IsNullOrWhiteSpace()) return null;
                if (Utils.IsFunction(exp, out var cacheNameFn))
                {
                    return cacheNameFn.Call(null, this) as string;
                }
                return exp;
            }
        }

        public string QueueName
        {
            get
            {
                return this is EditForm form ? form.Feature?.QueueName : Meta?.QueueName;
            }
        }
        public object DefaultValue { get; set; }

        public EditableComponent(Component guiInfo)
        {
            Meta = guiInfo;
            if (Meta != null && !Meta.Validation.IsNullOrWhiteSpace())
            {
                var rules = JsonConvert.DeserializeObject<List<ValidationRule>>(Meta.Validation);
                if (rules.HasElement())
                {
                    ValidationRules = rules.ToDictionary(x => x.Rule);
                }
            }
            else
            {
                ValidationRules = new Dictionary<string, ValidationRule>();
            }

            ValidationResult = new Dictionary<string, string>();
            Children = new List<EditableComponent>();
            DOMContentLoaded += () =>
            {
                SetRequired();
                SendQueueAction("Subscribe");
                if (Meta != null && Meta.Events.HasAnyChar())
                {
                    this.DispatchEvent(Meta.Events, EventType.DOMContentLoaded, Entity).Done();
                }
            };
        }

        public void AddChild(EditableComponent child, int? index = null, string showExp = null, string disabledExp = null)
        {
            if (child.IsSingleton)
            {
                child.Render();
                return;
            }
            if (child.ParentElement is null)
            {
                if (child is TabEditor tab)
                {
                    if (tab.Popup)
                    {
                        child.ParentElement = Element ?? TabEditor.TabContainer;
                    }
                    else
                    {
                        tab.ParentElement = TabEditor.TabContainer;
                    }
                }
                else
                {
                    child.ParentElement = Html.Context;
                }
            }

            if (child.Entity is null)
            {
                child.Entity = Entity;
            }

            if (Children is null)
            {
                Children = new List<EditableComponent>();
            }

            if (index is null || index >= Children.Count || index < 0)
            {
                Children.Add(child);
            }
            else
            {
                Children.Insert(index.Value, child);
            }

            if (child.Parent is null)
            {
                child.Parent = this;
            }

            Html.Take(child.ParentElement);
            child.Render();
            child.ToggleShow(showExp ?? (child.Meta is null ? "" : child.Meta.ShowExp));
            child.ToggleDisabled(disabledExp ?? (child.Meta is null ? "" : child.Meta.DisabledExp));
        }

        public void RemoveChild(EditableComponent child)
        {
            Children.Remove(child);
        }

        public abstract void Render();

        public virtual void Focus()
        {
            Element?.Focus();
        }

        private int _updateViewAwaiter;
        internal bool _dirty;

        protected bool _disabled;

        protected bool _setDirty = true;
        public virtual void UpdateViewAwait(bool force = false)
        {
            Window.ClearTimeout(_updateViewAwaiter);
            _updateViewAwaiter = Window.SetTimeout(() => UpdateView(force: force), 100);
        }

        public void UpdateView(bool force, params string[] componentNames) => UpdateView(force, null, componentNames);
        public virtual void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            PrepareUpdateView(force, dirty);
            if (Children.Nothing())
            {
                return;
            }

            if (componentNames.HasElement())
            {
                var coms = FilterChildren<Section>(x => componentNames.Contains(x.Name) && x is Section)
                    .SelectMany(x => x.FilterChildren<EditableComponent>(com => !(com is Section)));
                var coms2 = FilterChildren<EditableComponent>(x => componentNames.Contains(x.Name)).Where(x => !(x is Section));
                var shouldUpdate = coms.Union(coms2).Where(x => !(x is Section)).ToArray();
                foreach (var child in shouldUpdate)
                {
                    child.PrepareUpdateView(force, dirty);
                    child.UpdateView(force, dirty, componentNames);
                }
            }
            else
            {
                var shouldUpdate = FilterChildren<EditableComponent>().Where(x => !(x is Section)).ToArray();
                foreach (var child in shouldUpdate)
                {
                    child.PrepareUpdateView(force, dirty);
                    child.UpdateView(force, dirty, componentNames);
                }
            }
        }

        public virtual void PrepareUpdateView(bool force, bool? dirty)
        {
            if (force)
            {
                EmptyRow = false;
            }
            ToggleShow(Meta?.ShowExp);
            ToggleDisabled(Meta?.DisabledExp);
            if (dirty.HasValue)
            {
                _setDirty = dirty.Value;
            }
        }

        public virtual void Dispose()
        {
            SendQueueAction("Unsubscribe");
            DisposeChildren();
            RemoveDOM();
            Children = null;
            DOMContentLoaded = null;
            OnToggle = null;
            if (Parent != null && Parent.Children != null && Parent.Children.HasElement() && Parent.Children.Contains(this))
            {
                Parent.Children.Remove(this);
            }
            Disposed?.Invoke();
        }

        public virtual void DisposeChildren()
        {
            if (Children.Nothing())
            {
                return;
            }
            var leaves = Children.Flattern(x => x.Children)?.Where(x => x.Element != null && x.Parent != null && x.Children.Nothing()).ToArray();
            while (leaves.HasElement())
            {
                leaves.ForEach(x =>
                {
                    if (x is null)
                    {
                        return;
                    }
                    x.Dispose();
                    if (x.Parent != null && x.Parent.Children != null)
                    {
                        x.Parent.Children.Remove(x);
                    }
                });
                leaves = Children.Flattern(x => x.Children)?.Where(x => x.Element != null && x.Parent != null && x.Children.Nothing()).ToArray();
            }
        }

        protected virtual void RemoveDOM()
        {
            if (Element != null)
            {
                Element.Remove();
                Element = null;
            }
        }

        protected void SetDefaultVal()
        {
            if (Entity is null || EntityId != null)
            {
                return;
            }
            if (Utils.IsFunction(Meta.DefaultVal, out var fn))
            {
                fn.Call(this, this);
            }
            else if (Entity.GetPropValue(FieldName) == null)
            {
                Entity.SetPropValue(FieldName, DefaultValue);
            }
        }

        public IEnumerable<EditableComponent> FilterChildren(Func<EditableComponent, bool> predicate, Func<EditableComponent, bool> stopWhere = null) => FilterChildren<EditableComponent>(predicate, stopWhere);
        public IEnumerable<T> FilterChildren<T>(Func<T, bool> predicate = null, Func<T, bool> ignorePredicate = null, HashSet<T> visited = null) where T : EditableComponent
        {
            if (Children.Nothing())
            {
                yield break;
            }

            if (visited is null)
            {
                visited = new HashSet<T>();
            }
            foreach (var child in Children)
            {
                var t = child as T;
                if (t is null && child.Children.Nothing())
                {
                    continue;
                }
                if (ignorePredicate != null && ignorePredicate(t))
                {
                    continue;
                }

                if (t != null && (predicate is null || predicate(t)) && !visited.Contains(t))
                {
                    visited.Add(t);
                    yield return t;
                }

                foreach (var inner in child.FilterChildren(predicate, ignorePredicate, visited))
                {
                    yield return inner;
                }
            }
        }

        public void ToggleShow(string showExp)
        {
            if (showExp.HasAnyChar() && Utils.IsFunction(showExp, out var fn))
            {
                var shown = fn.Call(null, this) as bool?;
                Show = shown ?? false;
            }
        }

        public void ToggleDisabled(string disabled)
        {
            if (disabled.HasAnyChar() && Utils.IsFunction(disabled, out var fn))
            {
                var shouldDisabled = fn.Call(null, this) as bool?;
                Disabled = shouldDisabled ?? false;
            }
        }

        protected virtual void SetDisableUI(bool disabled)
        {
            if (Element is null)
            {
                return;
            }

            if (disabled)
            {
                Element.SetAttribute("disabled", "disabled");
            }
            else
            {
                Element.RemoveAttribute("disabled");
                Element.SetAttribute("enable", "true");
            }
        }

        protected void UpdateDirty(bool dirty)
        {
            if (dirty)
            {
                SetDirtyInternal();
            }
            else
            {
                ClearDirtyInternal();
                FilterChildren<EditableComponent>(x => x._dirty).SelectForEach(x => x.ClearDirtyInternal());
            }
        }

        private void SetDirtyInternal()
        {
            _dirty = _setDirty;
            if (!_setDirty)
            {
                _setDirty = true;
            }
        }

        private void ClearDirtyInternal()
        {
            _dirty = false;
        }

        public virtual string GetValueText()
        {
            if (Element is null)
            {
                return string.Empty;
            }
            if (Element is HTMLInputElement input)
            {
                return input.Value;
            }
            if (Element is HTMLTextAreaElement text)
            {
                return text.Value;
            }
            return string.Empty;
        }

        public virtual Task<bool> ValidateAsync()
        {
            return Task.FromResult(true);
        }

        protected void SetRequired()
        {
            if (ValidationRules.HasElement() && ValidationRules.ContainsKey(ValidationRule.Required))
            {
                Element?.SetAttribute(ValidationRule.Required, true.ToString());
            }
            else
            {
                Element?.RemoveAttribute(ValidationRule.Required);
            }
        }

        protected bool Validate<T, K>(string ruleType, T value, Func<T, K, bool> validPredicate)
        {
            if (!ValidationRules.ContainsKey(ruleType))
            {
                return true;
            }

            var rule = ValidationRules[ruleType];
            if (rule is null || rule.Value1 is null)
            {
                return true;
            }

            var field = rule.Value1.ToString();
            if (field.IsNullOrWhiteSpace())
            {
                return true;
            }

            var ruleValue = rule.Value1.As<K>();
            object label = ruleValue;
            var (hasField, fieldVal) = Entity.GetComplexProp(field);
            if (hasField)
            {
                label = Parent.FirstOrDefault(x => x.Name == field)?.Meta?.Label;
                ruleValue = fieldVal.As<K>();
            }
            if (!validPredicate(value, ruleValue))
            {
                ValidationResult.TryAdd(ruleType, string.Format(rule.Message, LangSelect.Get(Meta.Label), label));
                return true;
            }
            else
            {
                ValidationResult.Remove(ruleType);
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "<Pending>")]
        protected bool ValidateRequired<T>(T Value)
        {
            if (Element is null || ValidationRules.Nothing() || EmptyRow || AlwaysValid)
            {
                return true;
            }

            if (!ValidationRules.ContainsKey(ValidationRule.Required))
            {
                Element.RemoveAttribute(ValidationRule.Required);
                return true;
            }
            var requiredRule = ValidationRules[ValidationRule.Required];
            Element.SetAttribute(ValidationRule.Required, true.ToString());
            if (EqualityComparer<T>.Default.Equals(Value, default(T)) || Value.ToString().IsNullOrWhiteSpace())
            {
                Element.RemoveAttribute("readonly");
                ValidationResult.TryAdd(ValidationRule.Required, string.Format(requiredRule.Message, LangSelect.Get(Meta.Label), Entity));
                return false;
            }
            else
            {
                ValidationResult.Remove(ValidationRule.Required);
                return true;
            }
        }

        public virtual void AddRule(ValidationRule rule)
        {
            ValidationRules.TryAdd(rule.Rule, rule);
            if (rule.Rule == ValidationRule.Required)
            {
                Element.SetAttribute(ValidationRule.Required, true.ToString());
            }
        }

        public virtual void RemoveRule(string ruleName)
        {
            ValidationRules.Remove(ruleName);
            if (!ValidationRules.ContainsKey(ValidationRule.Required))
            {
                Element.RemoveAttribute(ValidationRule.Required);
            }
        }

        protected void CascadeField()
        {
            if (Meta.CascadeField.IsNullOrEmpty())
            {
                return;
            }

            var gridRow = this.FindClosest<ListViewItem>() as EditableComponent;
            var root = gridRow ?? this.FindClosest<EditForm>();
            var cascadeFields = Meta.CascadeField.Split(",").Where(x => x.HasAnyChar()).Select(x => x.Trim());
            if (cascadeFields.Nothing())
            {
                return;
            }

            cascadeFields.ForEach(field =>
            {
                root.FilterChildren(x => x.Name == field)
                 .SelectForEach(x =>
                 {
                     if (x is SearchEntry com && com != null)
                     {
                         com.Value = null;
                         com.Meta?.LocalData?.Clear();
                     }
                     else
                     {
                         x.UpdateView();
                     }
                 });
            });
        }

        public virtual IEnumerable<EditableComponent> GetInvalid()
        {
            if (AlwaysValid)
            {
                yield break;
            }
            if (!IsValid)
            {
                yield return this;
            }
            if (Children.HasElement())
            {
                foreach (var child in Children.Where(x => x is EditableComponent editable).Cast<EditableComponent>())
                {
                    foreach (var invalid in child.GetInvalid())
                    {
                        yield return invalid;
                    }
                }
            }
        }

        public virtual void PopulateFields(object entity = null)
        {
            if (Entity == null || Meta.PopulateField.IsNullOrEmpty())
            {
                return;
            }

            var gridRow = this.FindClosest<ListViewItem>() as EditableComponent;
            var root = gridRow ?? EditForm;
            var isFunc = Utils.IsFunction(Meta.PopulateField, out Function func);
            if (isFunc)
            {
                try
                {
                    func.Call(null, this, entity);
                }
                catch
                {
                }
                root.UpdateViewAwait(true);
                return;
            }

            var populatedFields = Meta.PopulateField.Split(",").Where(x => x.HasAnyChar()).Select(x => x.Trim());
            if (entity is null || populatedFields.Nothing())
            {
                return;
            }

            populatedFields.ForEach(field =>
            {
                var isEditing = Entity[IdField].As<int>() <= 0;
                root.FilterChildren<EditableComponent>(x => x.Name == field)
                    .SelectForEach(target =>
                    {
                        var value = Utils.GetPropValue(entity, field);
                        var oldVal = Utils.GetPropValue(Entity, field);
                        var targetType = Entity.GetType().GetComplexPropType(field);
                        if (value == oldVal || targetType is null || Activator.CreateInstance(targetType) != oldVal)
                        {
                            return;
                        }
                        Entity.SetComplexPropValue(field, value);
                        target.UpdateView(force: true, dirty: false);
                    });
            });
        }

        public virtual string GetValueTextAct()
        {
            return Element.TextContent;
        }

        protected void AddIdToPatch(List<PatchDetail> details)
        {
            var idField = details.FirstOrDefault(x => x.Field == IdField);
            if (idField != null) details.Remove(idField);
            if (EntityId is null)
            {
                details.Add(new PatchDetail
                {
                    Field = Utils.IdField,
                    Value = Uuid7.Id25()
                });
            }
            else
            {
                details.Add(new PatchDetail
                {
                    Field = Utils.IdField,
                    Value = EntityId,
                    OldVal = EntityId
                });
            }
        }

        protected void SendQueueAction(string action)
        {
            var queueName = QueueName;
            if (queueName.IsNullOrWhiteSpace()) return;
            EditForm.NotificationClient.Send($"{{\"QueueName\": \"{queueName}\", \"Action\": \"{action}\"}}");
            if (action == "Subscribe")
            {
                /*@
                window.addEventListener(queueName, this.QueueHandler);
                 */
            }
            else
            {
                /*@
                window.removeEventListener(queueName, this.QueueHandler);
                 */
            }
        }

        protected virtual void QueueHandler(CustomEvent e)
        {
            Console.WriteLine(e.Detail.ToJson());
        }

        protected Dictionary<string, Action<ObservableArgs>> _events = new Dictionary<string, Action<ObservableArgs>>();
        public Action<bool> AfterSaved;

        public Func<bool> BeforeSaved;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public void addEventListener(string name, Action<ObservableArgs> handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            var handlers = _events.GetValueOrDefault(name);
            var isNull = handlers is null;
            if (isNull)
            {
                _events.TryAdd(name, handler);
            }
            else
            {
                handlers += handler;
                _events[name] = handlers;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public void removeEventListener(string name, Action<ObservableArgs> handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            var handlers = _events.GetValueOrDefault(name);
            var isNull = handlers is null;
            if (isNull)
            {
                _events.Remove(name);
            }
        }

        public T FindComponentByName<T>(string name) where T : EditableComponent
        {
            return FirstOrDefault(x => x.Name == name && typeof(T).IsAssignableFrom(x.GetType())) as T;
        }

        public EditableComponent FirstOrDefault(Func<EditableComponent, bool> predicate, Func<EditableComponent, bool> ignorePredicate = null)
        {
            if (Children.Nothing())
            {
                return null;
            }

            foreach (var child in Children)
            {
                if (ignorePredicate != null && ignorePredicate(child))
                {
                    continue;
                }

                if (predicate(child))
                {
                    return child;
                }
            }
            foreach (var child in Children)
            {
                if (child.Children.HasElement())
                {
                    var res = child.FirstOrDefault(predicate, ignorePredicate);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }
            return null;
        }

        public IEnumerable<T> FindActiveComponent<T>(Func<T, bool> predicate = null) where T : EditableComponent
        {
            var result = new HashSet<T>();
            var type = typeof(T);
            if (Children.Nothing())
            {
                return Enumerable.Empty<T>();
            }

            foreach (var child in Children)
            {
                if (type.IsAssignableFrom(child.GetType())
                    && child.ParentElement != null
                    && !child.ParentElement.Hidden()
                    && (predicate == null || predicate(child as T)))
                {
                    result.Add(child as T);
                }

                if (child.Children.Nothing())
                {
                    continue;
                }

                var res = child.FindActiveComponent<T>();
                res.SelectForEach(x => result.Add(x));
            }
            return result.Distinct();
        }
    }
}
