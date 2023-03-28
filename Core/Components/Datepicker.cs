using Bridge.Html5;
using Core.Models;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Enums;
using System.Linq;

namespace Core.Components
{
    public class Datepicker : EditableComponent
    {
        public const string HHmmFormat = "00";
        private DateTime? _value;
        private bool _nullable;
        private bool _simpleNoEvent;
        public HTMLInputElement Input { get; set; }
        public string InitFormat = "";
        private string _currentFormat;
        private static HTMLElement _calendar;
        private static int _renderAwaiter;
        private static int _closeAwaiter;
        private bool show;
        private DateTime _someday;
        private HTMLInputElement _hour;
        private HTMLInputElement _minute;
        public static string[] formats = { "dd/MM/yyyy - HH:mm", "dd/MM/yyyy - HH:m", "dd/MM/yyyy - h:mm",
            "dd/MM/yyyy - HH:", "dd/MM/yyyy - h:", "dd/MM/yyyy - h:m", "dd/MM/yyyy - HH", "dd/MM/yyyy - h", "dd/MM/yyyy", "ddMMyyyy", "d/M/yyyy", "dMyyyy", "dd/MM/yy", "ddMMyy", "d/M", "dM", "dd/MM", "ddMM" };

        public DateTime? Value
        {
            get => _value; set
            {
                if (_value == value)
                {
                    return;
                }
                _value = value;
                if (_value.HasValue)
                {
                    var selectionEnd = Input.SelectionEnd;
                    Input.Value = _value != DateTime.MinValue ? _value.Value.ToString(_currentFormat) : string.Empty;
                    Input.SelectionStart = selectionEnd;
                    Input.SelectionEnd = selectionEnd;
                }
                else if (!_nullable)
                {
                    _value = DateTime.Now;
                    Input.Value = _value.Value.ToString(_currentFormat);
                }
                else
                {
                    Input.Value = null;
                }
                Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
                Dirty = true;
            }
        }

        public Datepicker(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            InitFormat = GuiInfo.FormatData.HasAnyChar() ? GuiInfo.FormatData.Replace("{0:", string.Empty).Replace("}", string.Empty)
                : (GuiInfo.Precision == 7 ? "dd/MM/yyyy - HH:mm" : "dd/MM/yyyy");
            _currentFormat = InitFormat;
            if (ele != null)
            {
                if (!(ele.FirstElementChild is HTMLInputElement input))
                {
                    input = Html.Take(ele).Input.GetContext() as HTMLInputElement;
                }
                Input = input;
            }
        }

        public override void Render()
        {
            SetDefaultVal();
            var fieldValue = Entity.GetComplexPropValue(GuiInfo.FieldName);
            Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
            DateTime parsedVal = DateTime.MinValue;
            var parsed = fieldValue is string strVal && strVal.HasAnyChar() && DateTime.TryParse(strVal, out parsedVal);
            _value = parsed ? parsedVal
                : fieldValue is null ? null
                : (fieldValue.GetType().IsDate() ? (DateTime?)fieldValue : null);
            _nullable = IsNullable<DateTimeOffset>() || IsNullable<DateTime>();
            Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
            var str = _value.HasValue && _value != DateTime.MinValue ? _value.Value.ToString(InitFormat) : string.Empty;
            OriginalText = str;
            OldValue = _value != DateTime.MinValue ? _value?.ToString().DateConverter() : string.Empty;
            Html.Take(ParentElement);
            if (Input is null)
            {
                Html.Instance.Div.ClassName("datetime-picker").TabIndex(-1).Input.Render();
                Element = Input = Html.Context as HTMLInputElement;
            }
            else
            {
                Html.Take(Input);
                Element = Input;
            }
            Html.Instance.Event(EventType.KeyDown, (Event e) =>
            {
                if (Disabled || e is null)
                {
                    return;
                }
                var code = e.KeyCode();
                if (code == (int)KeyCodeEnum.Enter)
                {
                    ParseDate();
                }
            });
            Html.Instance.Value(str)
                .Event(EventType.Focus, () =>
                {
                    if (_simpleNoEvent)
                    {
                        _simpleNoEvent = false;
                        return;
                    }
                    if (!GuiInfo.FocusSearch)
                    {
                        RenderCalendar();
                    }
                })
                .Event(EventType.Change, () => ParseDate())
                .PlaceHolder(GuiInfo.PlainText);
            Input.AutoComplete = AutoComplete.Off;
            Input.Name = GuiInfo.FieldName;
            Input.ParentElement.AddEventListener(EventType.FocusOut, CloseCalendar);
            Input.AddEventListener(EventType.KeyDown, async (e) => await KeyDownDateTime(e));
            Html.Instance.End.Div.ClassName("btn-group").Button.TabIndex(-1).Span.ClassName("icon mif-calendar")
                .Event(EventType.Click, () =>
                {
                    if (Input.Disabled)
                    {
                        return;
                    }

                    if (show)
                    {
                        CloseCalendar();
                    }
                    else
                    {
                        RenderCalendar();
                    }
                });
            DOMContentLoaded?.Invoke();
        }

        private async Task KeyDownDateTime(Event evt)
        {
            if (evt.KeyCodeEnum() == KeyCodeEnum.Enter && _value is null)
            {
                if (Input.Disabled)
                {
                    return;
                }

                if (show)
                {
                    CloseCalendar();
                }
                else
                {
                    RenderCalendar();
                }
            }
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
                    Input.Value = values[0];
                    ParseDate();
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
                        var updated = upItem.FilterChildren<Datepicker>(x => x.GuiInfo.FieldName == GuiInfo.FieldName && x.GuiInfo.Editable).FirstOrDefault();
                        updated.Dirty = true;
                        var (parsed, datetime, format) = TryParseDateTime(item);
                        updated.Value = datetime;
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

        private bool IsNullable<T>() where T : struct => Entity == null || Utils.IsNullable<T>(Entity.GetType(), GuiInfo.FieldName, Entity);

        private void ParseDate()
        {
            var (parsed, datetime, format) = TryParseDateTime(Input.Value);
            if (!parsed || Input.Value.IsNullOrWhiteSpace())
            {
                Input.Value = string.Empty;
                TriggerUserChange(null);
            }
            else
            {
                Value = datetime;
                TriggerUserChange(datetime);
            }
        }

        private void CloseCalendar()
        {
            _closeAwaiter = Window.SetTimeout(() =>
            {
                show = false;
                if (_calendar != null)
                {
                    _calendar.Style.Display = Display.None;
                }
                Input.Value = _value?.ToString(InitFormat);
                _hour = null;
                _minute = null;
            }, 250);
        }

        private void RenderCalendar(DateTime? someday = null)
        {
            Window.ClearTimeout(_renderAwaiter);
            Window.ClearTimeout(_closeAwaiter);
            _renderAwaiter = Window.SetTimeout(() => RenderCalendarTask(someday), 100);
        }

        private void RenderCalendarTask(DateTime? someday = null)
        {
            if (Disabled)
            {
                return;
            }

            var _someday = someday ?? _value ?? DateTime.Now;
            if (_value is null)
            {
                _someday = _someday.Date.AddHours(12);
            }

            show = true;
            Window.ClearTimeout(_closeAwaiter);
            if (_calendar != null)
            {
                _calendar.InnerHTML = null;
                _calendar.Style.Display = string.Empty;
            }
            else
            {
                Html.Take(Document.Body).Div.ClassName("calendar compact open open-up").TabIndex(-1).Trigger(EventType.Focus);
                _calendar = Html.Context;
            }
            Html.Take(_calendar).Div.ClassName("calendar-header")
                    .Div.ClassName("header-year").Text(_someday.Year.ToString()).End
                    .Div.ClassName("header-day").Text($"{_someday.DayOfWeek}, {_someday.Month:00} {_someday.Day:00}").End.End
                .Div.ClassName("calendar-content")
                    .Div.ClassName("calendar-toolbar")
                        .Span.ClassName("prev-month").Event(EventType.Click, () =>
                        {
                            _someday = _someday.AddMonths(-1);
                            RenderCalendar(_someday);
                        }).Span.ClassName("fa fa-chevron-left").End.End
                        .Span.ClassName("curr-month").Text(string.Format("{0:00}", _someday.Month)).End
                        .Span.ClassName("next-month").Event(EventType.Click, () =>
                        {
                            _someday = _someday.AddMonths(1);
                            RenderCalendar(_someday);
                        }).Span.ClassName("fa fa-chevron-right").End.End

                        .Span.ClassName("prev-year").Event(EventType.Click, () =>
                        {
                            _someday = _someday.AddYears(-1);
                            RenderCalendar(_someday);
                        }).Span.ClassName("fa fa-chevron-left").End.End
                        .Span.ClassName("curr-year").Text(_someday.Year.ToString()).End
                        .Span.ClassName("next-year").Event(EventType.Click, () =>
                        {
                            _someday = _someday.AddYears(1);
                            RenderCalendar(_someday);
                        }).Span.ClassName("fa fa-chevron-right").End.End
                    .End
                    .Div.ClassName("week-days")
                        .Span.ClassName("day").IText("Mo").End
                        .Span.ClassName("day").IText("Tu").End
                        .Span.ClassName("day").IText("We").End
                        .Span.ClassName("day").IText("Th").End
                        .Span.ClassName("day").IText("Fr").End
                        .Span.ClassName("day").IText("Sa").End
                        .Span.ClassName("day").IText("Su").End
                    .End
                    .Div.ClassName("days");

            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(_someday.Year, _someday.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var firstOutsideDayOfMonth = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek + 1);
            var lastOutsideDayOfMonth = lastDayOfMonth.AddDays(6 - (int)lastDayOfMonth.DayOfWeek + 1);
            if ((lastOutsideDayOfMonth - firstOutsideDayOfMonth).Days / 7 < 5)
            {
                lastOutsideDayOfMonth = lastOutsideDayOfMonth.AddDays(7);
            }
            var runner = firstOutsideDayOfMonth;
            while (runner <= lastOutsideDayOfMonth)
            {
                foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    if (runner.DayOfWeek == DayOfWeek.Monday)
                    {
                        Html.Instance.Div.ClassName("days-row");
                    }
                    Html.Instance.Div.ClassName("day").Text(runner.Day.ToString())
                        .Event(EventType.Click, SetSelectedDay, runner);
                    if (runner.Day == now.Day && runner.Month == now.Month && now.Year == _someday.Year)
                    {
                        Html.Instance.ClassName("showed today");
                    }

                    if (_value.HasValue && runner.Day == _value.Value.Day && runner.Month == _value.Value.Month && runner.Year == _value.Value.Year)
                    {
                        Html.Instance.ClassName("selected");
                    }

                    if (runner < firstDayOfMonth || runner > lastDayOfMonth)
                    {
                        Html.Instance.ClassName("outside");
                    }

                    Html.Instance.End.Render();
                    if (runner.DayOfWeek == DayOfWeek.Sunday)
                    {
                        Html.Instance.End.Render();
                    }
                    runner = runner.AddDays(1);
                }
            }
            if (GuiInfo.Precision == 7)
            {
                Html.Instance.EndOf(".calendar-content").Div.ClassName("time-picker")
                    .Div.ClassName("hour").Icon("fa fa-chevron-up").Event(EventType.Click, () => IncreaseTime(1)).End
                        .Input.Value(_someday.Hour.ToString(HHmmFormat))
                        .Event(EventType.Focus, () => Window.ClearTimeout(_closeAwaiter))
                        .Event(EventType.Change, ChangeHour)
                        .Event(EventType.KeyDown, ChangeHourHotKey);
                _hour = Html.Context as HTMLInputElement;
                Html.Instance.End.Render();
                Html.Instance.Icon("fa fa-chevron-down")
                    .Event(EventType.MouseDown, () => IncreaseTime(-1)).EndOf(".hour")
                    .Div.ClassName("minute").Icon("fa fa-chevron-up").Event(EventType.Click, () => IncreaseTime(10, true)).End
                        .Input.Value(_someday.Minute.ToString(HHmmFormat))
                        .Event(EventType.Focus, () => Window.ClearTimeout(_closeAwaiter))
                        .Event(EventType.Change, ChangeMinute)
                        .Event(EventType.KeyDown, ChangeMinuteHotKey);
                _minute = Html.Context as HTMLInputElement;
                Html.Instance.End.Icon("fa fa-chevron-down").Event(EventType.MouseDown, () => IncreaseTime(-10, true)).EndOf(".minute");
            }
            _calendar.AlterPosition(Element);
        }

        private void ChangeMinuteHotKey(Event e)
        {
            var keyCode = e.KeyCode();
            if (keyCode == 38 || keyCode == 40)
            {
                IncreaseTime(keyCode == 38 ? 1 : -1);
            }
        }

        private void ChangeHourHotKey(Event e)
        {
            var keyCode = e.KeyCode();
            if (keyCode == 38 || keyCode == 40)
            {
                IncreaseTime(keyCode == 38 ? 1 : -1);
            }
        }

        private void ChangeMinute(Event e)
        {
            if (_value is null || _someday == DateTime.MinValue)
            {
                _value = _someday;
            }

            var parsed = long.TryParse(_minute.Value, out var newMinute);
            if (!parsed || newMinute < 0 || newMinute > 59)
            {
                return;
            }
            if (_someday == DateTime.MinValue && OldValue != null)
            {
                DateTime dateTime = DateTime.Parse(OldValue);
                _someday = GuiInfo.Precision == 7 ? dateTime : DateTime.Now;
            }
            _someday = _someday.AddMinutes(-_someday.Minute).AddMinutes(newMinute);
            var innerTime = _someday;
            TriggerUserChange(innerTime);
        }

        private void ChangeHour(Event e)
        {
            var parsed = long.TryParse(_hour.Value, out var newHour);
            if (!parsed || newHour < 0 || newHour > 23)
            {
                return;
            }

            if (OldValue == null)
            {
                _someday = DateTime.Now.Date;
            }
            if (_someday == DateTime.MinValue && OldValue != null)
            {
                DateTime dateTime = DateTime.Parse(OldValue);
                _someday = GuiInfo.Precision == 7 ? dateTime : DateTime.Now;
            }

            _someday = _someday.AddHours(-_someday.Hour).AddHours(newHour);
            var innerTime = _someday;
            TriggerUserChange(innerTime);
        }

        private void IncreaseTime(long value, bool minute = false)
        {
            Window.ClearTimeout(_closeAwaiter);
            var innerTime = _value ?? _someday;
            if (!minute)
            {
                innerTime = innerTime.AddHours(value);
                _hour.Value = innerTime.Hour.ToString(HHmmFormat);
            }
            else
            {
                innerTime = innerTime.AddMinutes(value);
                _minute.Value = innerTime.Minute.ToString(HHmmFormat);
            }
            TriggerUserChange(innerTime);
        }

        private void SetSelectedDay(DateTime selected)
        {
            if (_value != null)
            {
                selected = selected.Date.AddHours(_value.Value.Hour).AddMinutes(_value.Value.Minute);
            }
            else
            {
                selected = selected.Date.AddHours(_someday.Hour).AddMinutes(_someday.Minute);
            }
            TriggerUserChange(selected);
        }

        private void TriggerUserChange(DateTime? selected, bool date = false)
        {
            var oldVal = _value;
            Value = selected;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldVal, EvType = EventType.Change });
            }
            PopulateFields();
            CascadeField();
            _simpleNoEvent = true;
            if (!date)
            {
                Input.Focus();
            }
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
            });
        }

        public static (bool, DateTime, string) TryParseDateTime(string value)
        {
            DateTime dateTime = DateTime.Now;
            bool parsed = false;
            string format = null;
            for (long i = 0; i < formats.Length; i++)
            {
                parsed = DateTime.TryParseExact(value, formats[i], System.Globalization.CultureInfo.InvariantCulture, out dateTime);
                if (parsed)
                {
                    format = formats[i];
                    break;
                }
            }

            return (parsed, dateTime, format);
        }

        public override void UpdateView(bool force = false, bool? setDirty = null, params string[] componentNames)
        {
            var value = Entity?.GetComplexPropValue(GuiInfo.FieldName);
            if (value is string strVal)
            {
                var parsed = DateTime.TryParse(strVal, out DateTime dateVal);
                if (parsed)
                {
                    Value = dateVal;
                }
            }
            else
            {
                Value = (DateTime?)Entity?.GetComplexPropValue(GuiInfo.FieldName);
            }
        }

        public override async Task<bool> ValidateAsync()
        {
            if (ValidationRules.Nothing())
            {
                return true;
            }
            await base.ValidateAsync();
            Validate(ValidationRule.GreaterThan, _value, (DateTime? value, DateTime? ruleValue) => ruleValue is null || value != null && value > ruleValue);
            Validate(ValidationRule.LessThan, _value, (DateTime? value, DateTime? ruleValue) => ruleValue is null || value != null && value < ruleValue);
            Validate(ValidationRule.GreaterThanOrEqual, _value, (DateTime? value, DateTime? ruleValue) => ruleValue is null || value != null && value >= ruleValue);
            Validate(ValidationRule.LessThanOrEqual, _value, (DateTime? value, DateTime? ruleValue) => ruleValue is null || value != null && value <= ruleValue);
            Validate(ValidationRule.Equal, _value, (DateTime? value, DateTime? ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (DateTime? value, DateTime? ruleValue) => value != ruleValue);
            return IsValid;
        }

        protected override void RemoveDOM()
        {
            if (Element != null && Element.ParentElement != null)
            {
                Element.ParentElement.Remove();
            }
        }

        protected override void SetDisableUI(bool value)
        {
            Input.ReadOnly = value;
        }
    }
}
