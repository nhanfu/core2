using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;

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
            Html.Take(_input).Event(EventType.Input, UserChange);
            SetDisableUI(!GuiInfo.Editable);
            SetDefaultVal();
            Value = (bool?)Utils.GetPropValue(Entity, FieldName);
            Entity.SetComplexPropValue(FieldName, _value);
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
            DOMContentLoaded?.Invoke();
        }

        public override string GetValueText()
        {
            return _value is null ? "N/A" : (_value == true ? "Check" : "Không check");
        }

        private void UserChange(Event e)
        {
            if (Disabled)
            {
                e.PreventDefault();
                return;
            }
            var check = _input.Checked;
            DataChanged(check);
        }

        private void DataChanged(bool check)
        {
            var oldVal = _value;
            _value = check;
            if (Entity != null)
            {
                Entity.SetComplexPropValue(FieldName, check);
            }
            Dirty = true;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldVal, EvType = EventType.Change });
            }
            PopulateFields();
            CascadeField();
            Client.ExecTaskNoResult(this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity));
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            var val = (bool?)Entity?.GetPropValue(FieldName);
            Value = val;
            if (!Dirty)
            {
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
