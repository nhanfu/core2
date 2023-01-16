using Bridge.Html5;
using Core.Models;
using Core.Components.Extensions;
using Core.Extensions;
using Core.MVVM;
using System.Linq;

namespace Core.Components
{
    public class Timepicker : Textbox
    {
        private static int _waiter;
        private static HTMLInputElement _hour;
        private static HTMLInputElement _minute;
        private string HHmmFormat => Datepicker.HHmmFormat;

        public long Hour
        {
            get
            {
                if (Text.IsNullOrWhiteSpace() || !Text.IsMatch(@"\d{2}h\d{2}"))
                {
                    Value = "00h00";
                }

                return Text.Split("h").FirstOrDefault().TryParseInt() ?? 0;
            }
        }

        public long Minute
        {
            get
            {
                if (Text.IsNullOrWhiteSpace() || !Text.IsMatch(@"\d{2}h\d{2}"))
                {
                    Value = "00h00";
                }

                return Text.Split("h").LastOrDefault().TryParseInt() ?? 0;
            }
        }

        public Timepicker(Component ui) : base(ui)
        {
        }

        public override void Render()
        {
            base.Render();
            Disabled = true;
            Input.Style.BackgroundColor = "transparent";
            Input.AddEventListener(EventType.FocusIn, RenderTimepicker);
            Input.AddEventListener(EventType.FocusOut, WaitToClose);
        }

        private void WaitToClose()
        {
            Window.ClearTimeout(_waiter);
            _waiter = Window.SetTimeout(() =>
            {
                _hour.ParentElement.ParentElement.Remove();
            }, 250);
        }

        private void RenderTimepicker()
        {
            Html.Take(Element.ParentElement).Div.ClassName("time-picker").TabIndex(-1)
                    .Event(EventType.FocusIn, ClearClosingWaiter)
                    .Event(EventType.FocusOut, WaitToClose)
                    .Div.ClassName("hour").Icon("fa fa-chevron-up").Event(EventType.Click, () => IncreaseTime(1)).End
                        .Input.Value(Hour.ToString(HHmmFormat));
            _hour = Html.Context as HTMLInputElement;
            _hour.AddEventListener(EventType.Change, ChangeHour);
            Html.Instance.End.Icon("fa fa-chevron-down").Event(EventType.Click, () => IncreaseTime(-1)).EndOf(".hour")
                .Div.ClassName("minute").Icon("fa fa-chevron-up").Event(EventType.Click, () => IncreaseTime(5, true)).End
                    .Input.Value(Minute.ToString(HHmmFormat));
            _minute = Html.Context as HTMLInputElement;
            _minute.AddEventListener(EventType.Change, ChangeMinute);
            Html.Instance.End.Icon("fa fa-chevron-down").Event(EventType.Click, () => IncreaseTime(-5, true)).EndOf(".minute");
        }

        private void ChangeMinute(Event e)
        {
            var parsed = long.TryParse(_minute.Value, out var newMinute);
            _minute.Value = parsed ? newMinute.ToString(HHmmFormat) : Minute.ToString(HHmmFormat);
            Value = _hour.Value + "h" + _minute.Value;
        }

        private void ChangeHour()
        {
            var parsed = long.TryParse(_hour.Value, out var newHour);
            _hour.Value = parsed ? newHour.ToString(HHmmFormat) : Hour.ToString(HHmmFormat);
            Value = _hour.Value + "h" + _minute.Value;
        }

        private void IncreaseTime(long value, bool minute = false)
        {
            ClearClosingWaiter();
            var afterIncreaseHour = Hour + value;
            var afterIncreaseMinute = Minute + value;
            if (!minute && afterIncreaseHour >= 0 && afterIncreaseHour < 24)
            {
                _hour.Value = afterIncreaseHour.ToString(HHmmFormat);
            }
            else if (afterIncreaseMinute >= 0 && afterIncreaseMinute < 60)
            {
                _minute.Value = afterIncreaseMinute.ToString(HHmmFormat);
            }
            Value = _hour.Value + "h" + _minute.Value;
            Dirty = true;
        }

        private static void ClearClosingWaiter()
        {
            Window.ClearTimeout(_waiter);
        }
    }
}