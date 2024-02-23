using Bridge.Html5;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class Number : EditableComponent
    {
        protected HTMLInputElement _input;
        private decimal? _value;
        private bool _nullable;
        private bool _isString;
        private const char _decimalSeparator = '.';
        internal bool SetSeclection { get; set; } = true;

        public decimal? Value
        {
            get => _value; set
            {
                var oldValue = _value;
                _value = value;
                if (_value != null)
                {
                    _value = Convert.ToDecimal(_value);
                    var precision = Convert.ToInt32(Meta.Precision ?? 0);
                    _value = decimal.Round(_value.Value, precision, MidpointRounding.Ceil);
                    var dotCount = _input.Value.Where(x => x == ',').Count();
                    var selectionEnd = _input.SelectionEnd;
                    _input.Value = EmptyRow ? string.Empty : string.Format("{0:n" + precision + "}", _value);
                    var addedDot = _input.Value.Where(x => x == ',').Count() - dotCount;
                    if (SetSeclection)
                    {
                        _input.SelectionStart = selectionEnd + addedDot;
                        _input.SelectionEnd = selectionEnd + addedDot;
                    }
                }
                else if (!_nullable)
                {
                    _value = 0;
                    _input.Value = _value.ToString();
                }
                else
                {
                    _input.Value = null;
                }
                if (oldValue != _value)
                {
                    Dirty = true;
                }
                Entity.SetComplexPropValue(FieldName, _value);
                PopulateFields();
                if (!Meta.ChildStyle.IsNullOrWhiteSpace())
                {
                    if (Utils.IsFunction(Meta.ChildStyle, out var fn))
                    {
                        fn.Call(this, Entity, _input).ToString();
                    }
                }
            }
        }

        public Number(Component ui, HTMLElement ele = null) : base(ui)
        {
            DefaultValue = 0;
            _input = ele as HTMLInputElement;
        }

        public override void Render()
        {
            SetDefaultVal();
            if (Entity != null)
            {
                var fieldVal = Utils.GetPropValue(Entity, FieldName);
                _isString = fieldVal is string;
                _nullable = IsNullable<int>() || IsNullable<long>() || IsNullable<decimal>();
                _value = GetDecimalValue();
                Entity.SetComplexPropValue(FieldName, _value);
            }
            if (_input is null)
            {
                Html.Take(ParentElement).Input.Render();
                Element = _input = Html.Context as HTMLInputElement;
            }
            else
            {
                Element = _input;
            }
            _input.Type = InputType.Tel;
            _input.SetAttribute("autocorrect", "off");
            _input.SetAttribute("spellcheck", "false");
            _input.AddEventListener(EventType.Input, SetValue);
            _input.AddEventListener(EventType.Change, ChangeSetValue);
            _input.AutoComplete = AutoComplete.Off;
            Value = _value; // set again to render in correct format
            if (!Meta.ChildStyle.IsNullOrWhiteSpace() && Utils.IsFunction(Meta.ChildStyle, out var fn))
            {
                Window.SetTimeout(() => fn.Call(this, Entity, _input).ToString(), 100);
            }
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
            DOMContentLoaded?.Invoke();
        }

        private bool IsNullable<T>() where T : struct => Utils.IsNullable<T>(Entity.GetType(), FieldName, Entity);

        private void ChangeSetValue()
        {
            var oldVal = _value;
            EmptyRow = false;
            if (_input.Value.IsNullOrWhiteSpace())
            {
                Value = null;
                UserInput?.Invoke(new ObservableArgs { NewData = null, OldData = oldVal, EvType = EventType.Change });
                return;
            }
            _input.Value = _input.Value.Trim();
            if (_input.Value.Last() == _decimalSeparator)
            {
                _input.Value = _input.Value.Substr(0, _input.Value.Length - 1);
            }

            var text = _input.Value.Replace(",", "");
            var parsedResult = decimal.TryParse(text, CultureInfo.InvariantCulture.NumberFormat, out decimal value);
            if (!parsedResult)
            {
                Value = _value; // Set old value to avoid accept invalid value
                return;
            }
            Value = value;
            UserInput?.Invoke(new ObservableArgs { NewData = value, OldData = oldVal, EvType = EventType.Change });
            PopulateFields();
            this.DispatchEvent(Meta.Events, EventType.Change, Entity, value, oldVal).Done();
        }

        private void SetValue()
        {
            EmptyRow = false;
            if (_input.Value.IsNullOrWhiteSpace())
            {
                Value = null;
                return;
            }
            _input.Value = _input.Value.Trim();
            if (_input.Value.Last() == _decimalSeparator)
            {
                _input.Value = _input.Value.Substr(0, _input.Value.Length - 1);
            }

            var text = _input.Value.Replace(",", "");
            var parsedResult = decimal.TryParse(text, CultureInfo.InvariantCulture.NumberFormat, out decimal value);
            if (!parsedResult)
            {
                Value = _value; // Set old value to avoid accept invalid value
                return;
            }
            var oldVal = _value;
            Value = value;
            UserInput?.Invoke(new ObservableArgs { NewData = value, OldData = oldVal, EvType = EventType.Input });
            this.DispatchEvent(Meta.Events, EventType.Input, Entity, value, oldVal).Done();
        }

        private decimal? GetDecimalValue()
        {
            if (Entity is null)
            {
                return null;
            }

            var value = Utils.GetPropValue(Entity, FieldName);
            if (value is null)
            {
                return null;
            }

            if (_isString && value.ToString().IsNullOrWhiteSpace())
            {
                return null;
            }

            try
            {
                var result = Convert.ToDecimal(value);
                return result;
            }
            catch { }
            return null;
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Value = GetDecimalValue();
            if (!Dirty)
            {
                DOMContentLoaded?.Invoke();
                OldValue = _input.Value;
            }
        }

        public override Task<bool> ValidateAsync()
        {
            if (ValidationRules.Nothing())
            {
                return Task.FromResult(true);
            }
            ValidationResult.Clear();
            ValidateRequired(_value);
            Validate(ValidationRule.GreaterThan, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value > ruleValue);
            Validate(ValidationRule.LessThan, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value < ruleValue);
            Validate(ValidationRule.GreaterThanOrEqual, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value >= ruleValue);
            Validate(ValidationRule.LessThanOrEqual, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value <= ruleValue);
            Validate(ValidationRule.Equal, _value, (decimal? value, decimal? ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (decimal? value, decimal? ruleValue) => value != ruleValue);
            return Task.FromResult(IsValid);
        }

        protected override void SetDisableUI(bool value)
        {
            _input.ReadOnly = value;
        }
    }
}
