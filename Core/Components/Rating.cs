using Bridge.Html5;
using Core.MVVM;
using Core.Extensions;
using Core.Models;
using Core.Components.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class Rating : EditableComponent
    {
        private readonly List<HTMLInputElement> InputList = new List<HTMLInputElement>();
        private int? _value;
        public int? Value
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
                    _value = _value.Value;
                    SetSelected(value);
                }
                Entity.SetComplexPropValue(FieldName, _value);
                Dirty = true;
            }
        }

        private void SetSelected(int? value)
        {
            if (value is null || value <= 0 || value > Meta.Precision)
            {
                return;
            }
            InputList[Meta.Precision.Value - value.Value].Checked = true;
        }

        public Rating(Component ui, HTMLElement ele = null) : base(ui)
        {
            Meta = ui ?? throw new ArgumentNullException(nameof(ui));
            Meta.Precision = Meta.Precision ?? 5;
            ParentElement = ele;
        }

        public override void Render()
        {
            Html.Take(ParentElement).Div.ClassName("rate");
            Element = Html.Context;
            var radioGroup = FieldName + Meta.Id + GetHashCode();
            for (var item = Meta.Precision; item >= 1; item--)
            {
                var radioId = $"{radioGroup}_{item}";
                Html.Take(Element).Input.Attr("type", "radio").Id(radioId).Attr("name", radioGroup)
                    .Value(item.ToString()).Event(EventType.Change, DispatchChange).Style(Meta.Style);
                InputList.Add(Html.Context.As<HTMLInputElement>());
                Html.Take(Element).Label.Attr("for", radioId).Text($"{item} stars");
            }
            Html.Take(Element).End.Render();
            _value = Utils.GetPropValue(Entity, FieldName).As<int?>();
            SetSelected(_value);
            DOMContentLoaded?.Invoke();
        }

        private void DispatchChange(Event e)
        {
            if (Disabled)
            {
                return;
            }

            if (InputList.Nothing())
            {
                return;
            }

            var check = InputList.FirstOrDefault(x => x.Checked);
            var oldVal = Value;
            Value = int.Parse(check.Value);
            Entity.SetComplexPropValue(FieldName, _value);
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = Value, OldData = oldVal });
            }
            Task.Run(async () =>
            {
                await this.DispatchEvent(Meta.Events, EventType.Click, Entity);
            });
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Value = Utils.GetPropValue(Entity, FieldName).As<int?>();
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                base.Disabled = value;
                InputList.ForEach(x => x.Disabled = value);
            }
        }

        public override string GetValueText()
        {
            return _value is null ? "Không đánh giá" : _value + " sao";
        }

        public override Task<bool> ValidateAsync()
        {
            ValidationResult.Clear();
            if (Value is null) return Task.FromResult(false);
            var isValid = Value.HasValue && ValidateRequired((int)Value);
            return Task.FromResult(isValid);
        }
    }
}
