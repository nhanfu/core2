using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;

namespace Core.Components
{
    public class Select2 : EditableComponent
    {
        public string IdFieldName { get; private set; }

        private const string SEntryClass = "search-entry";
        private int? _value;

        public int? Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    Dirty = true;
                }
                _value = value;
                Entity?.SetComplexPropValue(GuiInfo.FieldName, value);

                _value = value;
                FindMatchTextAsync();
            }
        }
        public string Text { get { return _select.Value; } set { _select.Value = value; } }
        public HTMLSelectElement _select;
        protected int _waitForDispose;
        public ObservableList<object> RowData;
        private string dataSourceFilter;
        private string idGuid;
        public string DataSourceFilter { get => dataSourceFilter; set => dataSourceFilter = value.DecodeSpecialChar(); }
        public object Matched { get; set; }

        public Select2(Component ui, HTMLElement ele = null) : base(ui)
        {
            DataSourceFilter = ui.DataSourceFilter;
            GuiInfo.ComponentGroup = null;
            idGuid = "js-example-templating" + Guid.NewGuid().ToString();
            RowData = new ObservableList<object>();
            Element = ele;
            IdFieldName = GuiInfo.FieldName;
        }

        public override void Render()
        {
            SetDefaultVal();
            var entityVal = Entity.GetComplexPropValue(IdFieldName);
            if (entityVal is string str_value)
            {
                _value = str_value.TryParseInt();
            }
            else
            {
                _value = entityVal as int?;
            }
            RenderInputAndEvents();
            RenderOption();
            FindMatchText();
            Element.Closest("td")?.AddEventListener(EventType.KeyDown, ListViewItemTab);
        }

        private void RenderInputAndEvents()
        {
            if (Element == null)
            {
                Element = _select = Html.Take(ParentElement).Div.Position(Position.relative).ClassName(SEntryClass).Select.Id(idGuid).GetContext() as HTMLSelectElement;
            }
            else
            {
                _select = Element as HTMLSelectElement;
                if (!_select.ParentElement.HasClass(SEntryClass))
                {
                    var parent = Document.CreateElement(ElementType.div.ToString());
                    parent.AddClass(SEntryClass);
                    _select.ParentElement.AppendChild(parent);
                    _select.ParentElement.InsertBefore(parent, _select);
                }
            }
            Html.Take(_select).PlaceHolder(GuiInfo.PlainText).Attr("name", IdFieldName);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public void RenderOption()
        {
            Task.Run(async () =>
            {
                await Client.LoadScript("https://lib.softek.com.vn/js/select2.min.js");
                var result = new List<object>();
                if (!TabEditor.DataSearchEntry.Any(x => x.Key == GuiInfo.DataSourceFilter))
                {
                    result = (await new Client(GuiInfo.RefName, GuiInfo.Reference != null ? GuiInfo.Reference.Namespace : null).GetList<object>(GuiInfo.DataSourceFilter)).Value;
                    TabEditor.DataSearchEntry.Add(GuiInfo.DataSourceFilter, result);
                }
                else
                {
                    result = TabEditor.DataSearchEntry.GetValueOrDefault(GuiInfo.DataSourceFilter);
                }
                RowData.Data = result;
                _select.Add(new HTMLOptionElement() { Text = "--Chọn dữ diệu--", Selected = false, Value = null });
                RowData.Data.ForEach(x =>
                {
                    _select.Add(new HTMLOptionElement() { Text = Utils.FormatEntity(GuiInfo.FormatData, x), Selected = Value == (int)x[IdField] ? true : false, Value = x[IdField].ToString(), Title = Utils.FormatEntity(GuiInfo.FormatEntity, x) });
                });
                var eq = GuiInfo.FilterEq;
                var seft = this;
                /*@
                 $("#" + seft.idGuid).select2({
                  matcher: function(params, data) {
                    if(eq)
                    {
                        if (params.term == null || params.term == "" || data.title.toLowerCase().startsWith(params.term.toLowerCase())) {
                          return data;
                        }
                    }
                    else
                    {
                        if (params.term == null || params.term == "" || data.text.toLowerCase().indexOf(params.term.toLowerCase()) >= 0) {
                          return data;
                        }
                    }
                    
                    return null;
                  },
                  templateSelection: function (selectedOption) {
                    return selectedOption.title;
                  }
                }).on('select2:select', function (e) {
                  var id = parseInt(e.params.data.id);
                  var math = System.Linq.Enumerable.from(seft.RowData.Data, System.Object).firstOrDefault(function (x) {
                                                return System.Nullable.getValue(Bridge.cast(Bridge.unbox(x[Core.Components.EditableComponent.IdField], System.Int32), System.Int32)) === id;
                                            }, null);
                  seft.EntrySelected(math);
                });
                 */
            });
        }

        public virtual void FindMatchText(int delay = 0)
        {
            FindMatchTextAsync();
        }

        protected void FindMatchTextAsync(bool force = false)
        {
            if (EmptyRow || !force)
            {
                return;
            }
            SetMatchedValue();
        }

        protected void CascadeAndPopulate()
        {
            CascadeField();
            PopulateFields(Matched);
        }

        public virtual void SetMatchedValue()
        {
            _select.Value = _value is null ? null : _value.ToString();
            UpdateValue();
        }

        private void UpdateValue()
        {
            /*@
            $("#" + this.idGuid).trigger("change.select2");
            */
            if (!Dirty)
            {
                OriginalText = _select.Title;
                DOMContentLoaded?.Invoke();
                OldValue = _value.ToString();
            }
        }

        private void EntrySelected(object rowData)
        {
            Window.ClearTimeout(_waitForDispose);
            EmptyRow = false;
            if (rowData is null || Disabled)
            {
                return;
            }

            var oldMatch = Matched;
            Matched = rowData;
            var oldValue = _value;
            _value = (int)rowData[IdField];
            if (Entity != null && GuiInfo.FieldName.HasAnyChar())
            {
                Entity.SetComplexPropValue(GuiInfo.FieldName, _value);
                Entity.SetComplexPropValue(GuiInfo.FieldName.Substr(0, GuiInfo.FieldName.Length - 2), rowData);
            }
            Dirty = true;
            Matched = rowData;
            SetMatchedValue();
            CascadeAndPopulate();
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity, rowData, oldMatch);
            });
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = _value, OldData = oldValue, EvType = EventType.Change });
            }
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            int? updatedValue = null;
            var fieldVal = Entity?.GetComplexPropValue(GuiInfo.FieldName);
            if (fieldVal != null)
            {
                if (fieldVal.GetType().IsNumber())
                {
                    updatedValue = Convert.ToInt32(fieldVal);
                }
            }
            else
            {
                updatedValue = null;
            }
            _value = updatedValue;
            if (updatedValue is null)
            {
                Matched = null;
                _select.Value = null;
                UpdateValue();
                return;
            }
            FindMatchTextAsync(force);
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
            Validate(ValidationRule.Equal, _value, (long? value, long? ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (long? value, long? ruleValue) => value != ruleValue);
            return IsValid;
        }

        protected override void SetDisableUI(bool value)
        {
            if (_select != null)
            {
                _select.Disabled = value;
            }
        }

        protected override void RemoveDOM()
        {
            if (_select != null && _select.ParentElement != null)
            {
                _select.ParentElement.Remove();
            }
        }
    }
}
