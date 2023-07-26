using Bridge.Html5;
using Core.Models;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Core.Enums;

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
        public string Id { get; set; }
        public string ComponentType { get; set; }
        public string Name { get; set; }
        public EditableComponent Parent { get; set; }
        public Component GuiInfo { get; set; }
        public List<EditableComponent> Children { get; set; } = new List<EditableComponent>();
        public EditableComponent FirstChild => Children.FirstOrDefault();
        /// <summary>
        /// The root element of tab or popup
        /// </summary>
        public virtual HTMLElement ParentElement { get; set; }
        public virtual HTMLElement Element { get; set; }
        public event Action Disposed;
        public Action DOMContentLoaded { get; set; }
        public Type EntityType { get; set; }
        public int EntityId => Entity == null ? 0 : Entity[IdField].As<int>();
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
                        e.PreventDefault();
                        var td = Element.Closest("td");
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
                    break;
                case KeyCodeEnum.Enter:
                    if (listViewItem != null && GuiInfo.ShowAudit)
                    {
                        e.PreventDefault();
                        var td = Element.Closest("td");
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
                if (GuiInfo != null && GuiInfo.ShowLabel && Parent is Section && GuiInfo.FieldName != null)
                {
                    Element.ParentElement.Style.Display = "none";
                    Element.ParentElement.PreviousElementSibling.Style.Display = "none";
                }
            }
            else
            {
                Element.Style.Display = "";
                if (GuiInfo != null && GuiInfo.ShowLabel && Parent is Section && GuiInfo.FieldName != null)
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
        public string BasicUpdateText => Updating ? "Cập nhật" : "Thêm mới";
        public virtual bool Updating => Entity != null && Entity[IdField].As<int>() > 0;
        protected bool StopChildrenHistory { get; set; }
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

        public virtual string OriginalText { get; internal set; }

        public bool AlwaysValid { get; set; }

        public bool IsValid => ValidationResult.Count == 0;

        public bool PopulateDirty { get; set; } = true;

        public EditableComponent(Component guiInfo)
        {
            GuiInfo = guiInfo;
            if (GuiInfo != null && !GuiInfo.Validation.IsNullOrWhiteSpace())
            {
                var rules = JsonConvert.DeserializeObject<List<ValidationRule>>(GuiInfo.Validation);
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
            DOMContentLoaded += async () =>
            {
                SetRequired();
                if (GuiInfo != null && GuiInfo.Events.HasAnyChar())
                {
                    await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.DOMContentLoaded, Entity);
                }
                SetOldTextAndVal();
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
            child.ToggleShow(showExp ?? child.GuiInfo?.ShowExp);
            child.ToggleDisabled(disabledExp ?? child.GuiInfo?.DisabledExp);
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
            ToggleShow(GuiInfo?.ShowExp);
            ToggleDisabled(GuiInfo?.DisabledExp);
            if (dirty.HasValue)
            {
                _setDirty = dirty.Value;
            }
        }

        public virtual void Dispose()
        {
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
            var id = Entity?[IdField].As<int?>();
            if (Entity is null || (id != null && id > 0) || GuiInfo.DefaultVal.IsNullOrWhiteSpace())
            {
                return;
            }
            var old = Entity[GuiInfo.FieldName];
            var type = Entity.GetType().GetProperty(GuiInfo.FieldName);
            if (type is null)
            {
                TrySetDFValue();
                return;
            }
            var defaultVal = Activator.CreateInstance(type.PropertyType);
            if (!defaultVal.Equals(old) && (type.PropertyType != typeof(string) || !(old is null)))
            {
                return;
            }
            TrySetDFValue();
        }

        private void TrySetDFValue()
        {
            try
            {
                var obj = Window.Eval<object>(GuiInfo.DefaultVal);
                Entity[GuiInfo.FieldName] = obj is Function ? (obj as Function).Call(this, this) : obj;
            }
            catch
            {

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
                FilterChildren<EditableComponent>(x => x._dirty).ForEach(x => x.ClearDirtyInternal());
            }
        }

        private void SetDirtyInternal()
        {
            _dirty = _setDirty;
            if (!_setDirty)
            {
                _setDirty = true;
            }
            if (!Updating)
            {
                SetOldTextAndVal();
            }
        }

        private void ClearDirtyInternal()
        {
            _dirty = false;
            SetOldTextAndVal();
        }

        internal void SetOldTextAndVal()
        {
            OriginalText = GetValueText();
            OldValue = Entity.GetComplexPropValue(GuiInfo?.FieldName)?.ToString();
        }

        public virtual void UpdateDirty(bool dirty, params string[] componentNames)
        {
            if (componentNames.Nothing())
            {
                return;
            }

            FilterChildren<EditableComponent>(child => child.GuiInfo != null && componentNames.Contains(child.Name))
                .ForEach(x =>
                {
                    if (dirty)
                    {
                        x.SetDirtyInternal();
                    }
                    else
                    {
                        x.ClearDirtyInternal();
                    }
                });
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

        public virtual async Task<bool> ValidateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            ValidationResult.Clear();
            return true;
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
                label = Parent.FirstOrDefault(x => x.Name == field)?.GuiInfo?.Label;
                ruleValue = fieldVal.As<K>();
            }
            if (!validPredicate(value, ruleValue))
            {
                ValidationResult.TryAdd(ruleType, string.Format(rule.Message, LangSelect.Get(GuiInfo.Label), label));
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
            if (Element is null || ValidationRules.Nothing())
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
                ValidationResult.TryAdd(ValidationRule.Required, string.Format(requiredRule.Message, LangSelect.Get(GuiInfo.Label), Entity));
                return true;
            }
            else
            {
                ValidationResult.Remove(ValidationRule.Required);
                return false;
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
            if (GuiInfo.CascadeField.IsNullOrEmpty())
            {
                return;
            }

            var gridRow = this.FindClosest<ListViewItem>() as EditableComponent;
            var root = gridRow ?? this.FindClosest<EditForm>();
            var cascadeFields = GuiInfo.CascadeField.Split(",").Where(x => x.HasAnyChar()).Select(x => x.Trim());
            if (cascadeFields.Nothing())
            {
                return;
            }

            cascadeFields.ForEach(field =>
            {
                root.FilterChildren(x => x.Name == field)
                 .ForEach(x =>
                 {
                     if (x is SearchEntry com && com != null)
                     {
                         com.Value = null;
                         com.GuiInfo?.LocalData?.Clear();
                     }
                     else
                     {
                         x.UpdateView();
                     }
                 });
            });
        }

        public virtual StringBuilder BuildTextHistory(StringBuilder builder = null, HashSet<object> visited = null)
        {
            if (builder is null)
            {
                builder = new StringBuilder();
            }
            if (visited is null)
            {
                visited = new HashSet<object>();
            }
            if (visited.Contains(this))
            {
                return builder;
            }
            visited.Add(this);
            BuildInternal(builder, visited);
            return builder;
        }

        private void BuildInternal(StringBuilder builder, HashSet<object> visited)
        {
            BuildHistoryInternal(builder);
            if (Children.HasElement() && !StopChildrenHistory)
            {
                foreach (var child in Children.Where(x => x is EditableComponent).Cast<EditableComponent>())
                {
                    child.BuildTextHistory(builder, visited);
                }
            }
            return;
        }

        protected virtual void BuildHistoryInternal(StringBuilder builder)
        {
            if (GuiInfo is null)
            {
                return;
            }

            var updatedText = GetValueText();
            if (updatedText.IsNullOrWhiteSpace())
            {
                updatedText = "N/A";
            }
            if (Updating)
            {
                var originText = OriginalText.IsNullOrWhiteSpace() ? "N/A" : OriginalText;
                if (originText.Trim() == updatedText.Trim())
                {
                    return;
                }
                builder.Append(LangSelect.Get(GuiInfo.Label)).Append(": ").Append(originText).Append(" -> ").Append(updatedText).Append(Utils.NewLine);
            }
            else if (!Updating && AlwaysLogHistory)
            {
                builder.Append(LangSelect.Get(GuiInfo.Label)).Append(": ").Append(updatedText).Append(Utils.NewLine);
            }
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
            if (Entity == null || GuiInfo.PopulateField.IsNullOrEmpty())
            {
                return;
            }

            var gridRow = this.FindClosest<ListViewItem>() as EditableComponent;
            var root = gridRow ?? EditForm;
            var isFunc = Utils.IsFunction(GuiInfo.PopulateField, out Function func);
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

            var populatedFields = GuiInfo.PopulateField.Split(",").Where(x => x.HasAnyChar()).Select(x => x.Trim());
            if (entity is null || populatedFields.Nothing())
            {
                return;
            }

            populatedFields.ForEach(field =>
            {
                var isEditing = Entity[IdField].As<int>() <= 0;
                root.FilterChildren<EditableComponent>(x => x.Name == field)
                    .ForEach(target =>
                    {
                        var value = entity.GetComplexPropValue(field);
                        var oldVal = Entity.GetComplexPropValue(field);
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
    }
}
