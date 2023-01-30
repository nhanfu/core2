using Bridge.Html5;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Threading.Tasks;

namespace Core.Components
{
    public class Checkbox : EditableComponent
    {
        private bool? _value;
        private HTMLInputElement _input;
        public bool? Value
        {
            get => _value; set
            {
                _value = value;
                _input.Checked = value ?? false;
            }
        }

        public Checkbox(Component ui, HTMLElement ele = null) : base(ui)
        {
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            ParentElement = ele;
        }

        public override void Render()
        {
            Html.Take(ParentElement).TabIndex(-1).SmallCheckbox(_value ?? false);
            _input = Html.Context.PreviousElementSibling as HTMLInputElement;
            Element = _input.ParentElement;
            Html.Take(_input).AsyncEvent(EventType.Input, UserChange);
            SetDisableUI(!GuiInfo.Editable);
            SetDefaultVal();
            Value = (bool?)Entity.GetComplexPropValue(GuiInfo.FieldName);
            Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
            DOMContentLoaded?.Invoke();
        }

        public override string GetValueText()
        {
            return _value is null ? "N/A" : (_value == true ? "Check" : "Không check");
        }

        private async Task UserChange(Event e)
        {
            if (Disabled)
            {
                e.PreventDefault();
                return;
            }
            var check = _input.Checked;
            await DataChanged(check);
        }

        private async Task DataChanged(bool check)
        {
            var oldVal = _value;
            _value = check;
            if (Entity != null)
            {
                Entity.SetComplexPropValue(GuiInfo.FieldName, check);
            }
            Dirty = true;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldVal, EvType = EventType.Change });
            }
            PopulateFields();
            CascadeField();
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var val = (bool?)Entity?.GetComplexPropValue(GuiInfo.FieldName);
            Value = val;
            if (!Dirty)
            {
                OriginalText = _input.Value;
                DOMContentLoaded?.Invoke();
                OldValue = _input.Value;
            }
        }

        protected override void SetDisableUI(bool value)
        {
            if (value)
            {
                Element.SetAttribute("disabled", "disabled");
            }
            else
            {
                Element.RemoveAttribute("disabled");
            }
            _input.Disabled = value;
        }
    }
}
