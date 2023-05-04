using Bridge.Html5;
using Core.Components.Extensions;
using Core.Enums;
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
                    var precision = Convert.ToInt32(GuiInfo.Precision ?? 0);
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
                Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
                PopulateFields();
                if (!GuiInfo.ChildStyle.IsNullOrWhiteSpace())
                {
                    if (Utils.IsFunction(GuiInfo.ChildStyle, out var fn))
                    {
                        fn.Call(this, Entity, _input).ToString();
                    }
                }
            }
        }

        public Number(Component ui, HTMLElement ele = null) : base(ui)
        {
            _input = ele as HTMLInputElement;
        }

        public override void Render()
        {
            SetDefaultVal();
            if (Entity != null)
            {
                var fieldVal = Entity.GetComplexPropValue(GuiInfo.FieldName);
                _isString = fieldVal is string;
                _nullable = IsNullable<int>() || IsNullable<long>() || IsNullable<decimal>();
                _value = GetDecimalValue();
                Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
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
            _input.AddEventListener(EventType.KeyDown, async (e) => await KeyDownNumber(e));
            _input.AddEventListener(EventType.Change, ChangeSetValue);
            _input.AutoComplete = AutoComplete.Off;
            Value = _value; // set again to render in correct format
            if (GuiInfo.AutoFit)
            {
                this.SetAutoWidth(_input.Value, _input.GetComputedStyle().Font);
            }
            Window.SetTimeout(() =>
            {
                if (!GuiInfo.ChildStyle.IsNullOrWhiteSpace())
                {
                    if (Utils.IsFunction(GuiInfo.ChildStyle, out var fn))
                    {
                        fn.Call(this, Entity, _input).ToString();
                    }
                }
            }, 100);
            DOMContentLoaded?.Invoke();
        }

        private async Task KeyDownNumber(Event evt)
        {
            if (!(Parent is ListViewItem))
            {
                return;
            }
            var check = evt.KeyCodeEnum() == KeyCodeEnum.V && evt.CtrlOrMetaKey() && evt.ShiftKey();
            var tcs = new TaskCompletionSource<string>();
            if (check)
            {
                /*@
                 navigator.clipboard.readText().then(clipText => tcs.setResult(clipText));
                */
                var te = await tcs.Task;
                var checkMulti = te.Contains("\r\n");
                if (checkMulti)
                {
                    var values = te.Split("\r\n").ToList();
                    _input.Value = values[0];
                    SetValue();
                    var current = this.FindClosest<ListViewItem>();
                    var startNo = current.RowNo;
                    var varCount = values.Count + startNo;
                    var gridView = this.FindClosest<VirtualGrid>();
                    gridView.AutoFocus = true;
                    foreach (var item in values.Take(values.Count - 1))
                    {
                        var upItem = gridView.AllListViewItem.FirstOrDefault(x => x.RowNo == startNo);
                        if (upItem is null)
                        {
                            if (startNo <= varCount)
                            {
                                await Task.Delay(1000);
                                upItem = gridView.AllListViewItem.FirstOrDefault(x => x.RowNo == startNo);
                            }
                        }
                        var updated = upItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == GuiInfo.FieldName).FirstOrDefault();
                        updated.Dirty = true;
                        updated.Value = item.Replace(",", "").Replace(".", "").IsNullOrWhiteSpace() ? default(decimal) : decimal.Parse(item.Replace(",", "").Replace(".", ""));
                        updated.UpdateView();
                        updated.PopulateFields();
                        await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, upItem.Entity);
                        await upItem.ListViewSection.ListView.DispatchEventToHandlerAsync(upItem.ListViewSection.ListView.GuiInfo.Events, EventType.Change, upItem.Entity);
                        if (gridView.GuiInfo.IsRealtime)
                        {
                            await upItem.PatchUpdate();
                        }
                        gridView.DataTable.ParentElement.ScrollTop += 26;
                        startNo++;
                        await Task.Delay(300);
                    }
                    gridView.AutoFocus = false;
                }
            }
        }

        private bool IsNullable<T>() where T : struct => Utils.IsNullable<T>(Entity.GetType(), GuiInfo.FieldName, Entity);

        private void ChangeSetValue()
        {
            var oldVal = _value;
            EmptyRow = false;
            if (_input.Value.IsNullOrWhiteSpace())
            {
                Value = null;
                if (UserInput != null)
                {
                    UserInput.Invoke(new ObservableArgs { NewData = null, OldData = oldVal, EvType = EventType.Change });
                }
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
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = value, OldData = oldVal, EvType = EventType.Change });
            }
            PopulateFields();
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity, value, oldVal);
            });
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
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = value, OldData = oldVal, EvType = EventType.Input });
            }
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Input, Entity, value, oldVal);
            });
        }

        private decimal? GetDecimalValue()
        {
            if (Entity is null)
            {
                return null;
            }

            var value = Entity.GetComplexPropValue(GuiInfo.FieldName);
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
                OriginalText = _input.Value;
                DOMContentLoaded?.Invoke();
                OldValue = _input.Value;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<bool> ValidateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (ValidationRules.Nothing())
            {
                return true;
            }
            ValidationResult.Clear();
            ValidateRequired(_value);
            Validate(ValidationRule.GreaterThan, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value > ruleValue);
            Validate(ValidationRule.LessThan, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value < ruleValue);
            Validate(ValidationRule.GreaterThanOrEqual, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value >= ruleValue);
            Validate(ValidationRule.LessThanOrEqual, _value, (decimal? value, decimal? ruleValue) => ruleValue is null || value != null && value <= ruleValue);
            Validate(ValidationRule.Equal, _value, (decimal? value, decimal? ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (decimal? value, decimal? ruleValue) => value != ruleValue);
            return IsValid;
        }

        protected override void SetDisableUI(bool value)
        {
            _input.ReadOnly = value;
        }
    }
}
