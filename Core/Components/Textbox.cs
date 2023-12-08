using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
                    Input.Value = values[0];
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
                        var updated = upItem.FilterChildren<Textbox>(x => x.FieldName == FieldName).FirstOrDefault();
                        updated.Dirty = true;
                        updated.Value = item;
                        updated.UpdateView();
                        updated.PopulateFields();
                        await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, upItem.Entity);
                        await upItem.ListViewSection.ListView.DispatchEventToHandlerAsync(upItem.ListViewSection.ListView.GuiInfo.Events, EventType.Change, upItem.Entity);
                        if (gridView.GuiInfo.IsRealtime)
                        {
                            upItem.PatchUpdateOrCreate();
                        }
                        gridView.DataTable.ParentElement.ScrollTop += 26;
                        startNo++;
                        await Task.Delay(500);
                    }
                    gridView.AutoFocus = false;
                }
            }
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
                TextArea.AddEventListener(EventType.KeyDown, async (e) => await KeyDownNumber(e));
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
                Input.AddEventListener(EventType.KeyDown, async (e) => await KeyDownNumber(e));
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
            if (Entity != null)
            {
                Entity.SetComplexPropValue(FieldName, _value);
            }
            Dirty = true;
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _text, OldData = _oldText, EvType = type });
            }
            PopulateFields();
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, type, Entity);
            });
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
            ValidateUnique().Done(() => {
                tcs.TrySetResult(IsValid);
            });
            return tcs.Task;
        }

        protected async Task ValidateUnique()
        {
            if (!ValidationRules.ContainsKey(ValidationRule.Unique))
            {
                return;
            }
            var rule = ValidationRules[ValidationRule.Unique];
            if (rule is null || _text.IsNullOrWhiteSpace())
            {
                return;
            }
            var fieldName = FieldName;
            var entityId = Entity[IdField].As<int?>();
            var filter = rule.Condition ?? $"Active eq true and ";
            if (entityId > 0)
            {
                filter += $"{fieldName} eq '{_text.EncodeSpecialChar()}' and Id ne {entityId}";
            }
            else
            {
                filter += $"{fieldName} eq '{_text.EncodeSpecialChar()}'";
            }
            var entity = Utils.GetEntity(EditForm.Feature.EntityId)?.Name;
            var exists = await new Client(entity).GetAsync<bool>($"/Exists/?$select={IdField},{fieldName}&$filter={filter}");
            if (!exists)
            {
                ValidationResult.Remove(ValidationRule.Unique);
            }
            else
            {
                ValidationResult.TryAdd(ValidationRule.Unique, string.Format(rule.Message, LangSelect.Get(GuiInfo.Label), _text));
            }
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
