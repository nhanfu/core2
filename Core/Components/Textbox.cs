using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Components
{
    public class Textbox : EditableComponent
    {
        public HTMLInputElement Input { get; set; }
        private HTMLTextAreaElement TextArea { get; set; }
        public bool MultipleLine { get; set; }
        public bool Password { get; set; }
        private object _value;
        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                if (_value != null && _value is string str_val && ((EditForm.Feature != null && !EditForm.Feature.IgnoreEncode) || EditForm.Feature is null))
                {
                    Entity?.SetComplexPropValue(FieldName, str_val.DecodeSpecialChar().EncodeSpecialChar());
                }
                if (Entity != null)
                {
                    Entity.SetComplexPropValue(FieldName, _value);
                }

                var text = (EditForm.Feature != null && EditForm.Feature.IgnoreEncode) ? _value?.ToString() : _value?.ToString().DecodeSpecialChar();
                if (GuiInfo.FormatData.HasAnyChar())
                {
                    text = Utils.FormatEntity(GuiInfo.FormatData, Entity?.GetPropValue(FieldName));
                }

                if (GuiInfo.FormatEntity.HasAnyChar())
                {
                    text = Utils.FormatEntity(GuiInfo.FormatEntity, null, Entity, Utils.EmptyFormat, Utils.EmptyFormat);
                }
                if (!GuiInfo.ChildStyle.IsNullOrWhiteSpace())
                {
                    if (Utils.IsFunction(GuiInfo.ChildStyle, out var fn))
                    {
                        fn.Call(this, Entity, Element).ToString();
                    }
                }
                Text = text;
                PopulateFields();
            }
        }

        private string _oldText;
        private string _text;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                if (Input != null)
                {
                    Input.Value = _text;
                }

                if (TextArea != null)
                {
                    TextArea.Value = _text;
                }
            }
        }
        public Textbox(Component ui, HTMLElement ele = null) : base(ui)
        {
            if (ele is HTMLInputElement)
            {
                Input = ele as HTMLInputElement;
            }
            else if (ele is HTMLTextAreaElement)
            {
                TextArea = ele as HTMLTextAreaElement;
            }
            Document.AddEventListener(EventType.VisibilityChange, e =>
            {
                if (Dirty)
                {
                    PopulateUIChange(EventType.VisibilityChange);
                }
            });
        }

        public override void Render()
        {
            SetDefaultVal();
            var val = Entity?.GetPropValue(FieldName);
            if (val != null && val is string str_val && EditForm != null && EditForm.Feature != null && !EditForm.Feature.IgnoreEncode)
            {
                Entity?.SetComplexPropValue(FieldName, str_val.DecodeSpecialChar().EncodeSpecialChar());
            }
            var text = val?.ToString();
            if (GuiInfo.FormatData.HasAnyChar())
            {
                text = Utils.FormatEntity(GuiInfo.FormatData, val);
            }

            if (GuiInfo.FormatEntity.HasAnyChar())
            {
                text = Utils.FormatEntity(GuiInfo.FormatEntity, Entity);
            }
            _text = EditForm != null && EditForm.Feature != null && EditForm.Feature.IgnoreEncode ? text : text.DecodeSpecialChar();
            if (MultipleLine || TextArea != null)
            {
                if (TextArea is null)
                {
                    Html.Take(ParentElement).TextArea.Value(_text).PlaceHolder(GuiInfo.PlainText);
                    Element = TextArea = Html.Context as HTMLTextAreaElement;
                }
                else
                {
                    Element = TextArea;
                    TextArea.Value = _text;
                }
                if (GuiInfo.Row > 0)
                {
                    Html.Instance.Attr("rows", GuiInfo.Row ?? 1);
                }

                TextArea.OnInput += (e) => PopulateUIChange(EventType.Input);
                TextArea.OnChange += (e) => PopulateUIChange(EventType.Change);
            }
            else
            {
                if (Input is null)
                {
                    Html.Take(ParentElement).Input.Value(_text).PlaceHolder(GuiInfo.PlainText);
                    Element = Input = Html.Context as HTMLInputElement;
                }
                else
                {
                    Element = Input;
                    Input.Value = _text;
                }
                Input.AutoComplete = AutoComplete.Off;
                Input.Name = GuiInfo.DataSourceFilter ?? FieldName;
                Input.OnInput += (e) => PopulateUIChange(EventType.Input);
                Input.OnChange += (e) => PopulateUIChange(EventType.Change);
            }
            if (!GuiInfo.ChildStyle.IsNullOrWhiteSpace())
            {
                if (Utils.IsFunction(GuiInfo.ChildStyle, out var fn))
                {
                    fn.Call(this, Entity, Element).ToString();
                }
            }
            if (Password)
            {
                Html.Instance.Style("text-security: disc;-webkit-text-security: disc;-moz-text-security: disc;");
            }
            if (!GuiInfo.ShowLabel)
            {
                Html.Instance.PlaceHolder(GuiInfo.PlainText);
            }
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
            DOMContentLoaded?.Invoke();
        }

        private void PopulateUIChange(EventType type, bool shouldTrim = false)
        {
            if (Disabled)
            {
                return;
            }
            _oldText = _text;
            _text = Input?.Value ?? TextArea.Value;
            _text = Password ? _text : shouldTrim ? _text?.Trim() : _text;
            if (GuiInfo.UpperCase && _text != null)
            {
                Text = _text.ToLocaleUpperCase();
            }
            _value = (EditForm != null && EditForm.Feature != null && EditForm.Feature.IgnoreEncode) ? _text : _text.EncodeSpecialChar();
            Entity?.SetComplexPropValue(FieldName, _value);
            Dirty = true;
            UserInput?.Invoke(new ObservableArgs { NewData = _text, OldData = _oldText, EvType = type });
            PopulateFields();
            this.DispatchEvent(GuiInfo.Events, type, Entity).Done();
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            Value = Entity?.GetPropValue(FieldName);
            if (!Dirty)
            {
                DOMContentLoaded?.Invoke();
                OldValue = _text;
            }
        }

        public override Task<bool> ValidateAsync()
        {
            if (ValidationRules.Nothing())
            {
                return Task.FromResult(true);
            }
            var tcs = new TaskCompletionSource<bool>();
            ValidationResult.Clear();
            Validate(ValidationRule.MinLength, _text, (string value, long minLength) => _text != null && _text.Length >= minLength);
            Validate(ValidationRule.CheckLength, _text, (string text, long checkLength) => _text == null || _text == "" || _text.Length == checkLength);
            Validate(ValidationRule.MaxLength, _text, (string text, long maxLength) => _text == null || _text.Length <= maxLength);
            Validate<string, string>(ValidationRule.RegEx, _text, ValidateRegEx);
            ValidateRequired(Text);
            ValidateUnique().Done(() =>
            {
                tcs.TrySetResult(IsValid);
            });
            return tcs.Task;
        }

        protected Task ValidateUnique()
        {
            if (!ValidationRules.ContainsKey(ValidationRule.Unique))
            {
                return Task.FromResult(true);
            }
            var rule = ValidationRules[ValidationRule.Unique];
            if (rule is null || _text.IsNullOrWhiteSpace())
            {
                return Task.FromResult(true);
            }
            var isFn = Utils.IsFunction(GuiInfo.PreQuery, out var fn);
            var table = GuiInfo.RefName.HasNonSpaceChar() ? GuiInfo.RefName : EditForm.Feature.EntityName;
            var sql = new SqlViewModel
            {
                ComId = GuiInfo.Id,
                Params = isFn ? JSON.Stringify(fn.Call(null, this)) : null,
                ConnKey = ConnKey
            };
            var tcs = new TaskCompletionSource<object>();
            Client.Instance.ComQuery(sql).Done(ds =>
            {
                var exists = ds.Length > 0 && ds[0].Length > 0;
                if (exists)
                {
                    ValidationResult.TryAdd(ValidationRule.Unique, string.Format(rule.Message, LangSelect.Get(GuiInfo.Label), _text));
                }
                else
                {
                    ValidationResult.Remove(ValidationRule.Unique);
                }
                tcs.TrySetResult(true);
            });
            return tcs.Task;
        }

        private bool ValidateRegEx(string value, string regText)
        {
            if (value is null)
            {
                return true;
            }

            var regEx = new RegExp(regText);
            var res = regEx.Test(value);
            var rule = ValidationRules[ValidationRule.RegEx];
            if (!res && rule.RejectInvalid)
            {
                var end = Input.SelectionEnd;
                Text = _oldText;
                _value = _oldText;
                Input.SelectionStart = end;
                Input.SelectionEnd = end;
                return regEx.Test(_oldText);
            }
            return res;
        }

        protected override void SetDisableUI(bool value)
        {
            if (Input != null)
            {
                Input.ReadOnly = value;
            }

            if (TextArea != null)
            {
                TextArea.ReadOnly = value;
            }
        }
    }
}
