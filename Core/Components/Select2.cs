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
        private string _value;

        public string Value
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
        public List<object> RowData;
        private string dataSourceFilter;
        public string DataSourceFilter { get => dataSourceFilter; set => dataSourceFilter = value.DecodeSpecialChar(); }
        public object Matched { get; set; }

        public Select2(Component ui, HTMLElement ele = null) : base(ui)
        {
            DataSourceFilter = ui.DataSourceFilter;
            GuiInfo.ComponentGroup = null;
            idGuid = "js-example-templating" + Guid.NewGuid().ToString();
            RowData = new List<object>();
            Element = ele;
            IdFieldName = GuiInfo.FieldName;
        }

        public override void Render()
        {
            SetDefaultVal();
            var entityVal = Entity.GetPropValue(IdFieldName)?.ToString();
            if (entityVal is string str_value)
            {
                _value = str_value;
            }
            else
            {
                _value = entityVal;
            }
            RenderInputAndEvents();
            if (!TabEditor.DataSearchEntry.Any(x => x.Key == GuiInfo.DataSourceFilter + GuiInfo.RefName))
            {
                LoadData();
            }
            else
            {
                RowData = TabEditor.DataSearchEntry.GetValueOrDefault(GuiInfo.DataSourceFilter + GuiInfo.RefName);
                RenderOption();
            }
            FindMatchText();
            var td = Element.Closest("td");
            td?.AddEventListener(EventType.KeyDown, ListViewItemTab);
        }

        private void LoadData()
        {
            Task.Run(async () =>
            {
                await Client.LoadScript("https://lib.softek.com.vn/js/select2.min.js");
                if (!TabEditor.DataSearchEntry.Any(x => x.Key == GuiInfo.DataSourceFilter + GuiInfo.RefName))
                {
                    var rsData = await new Client(GuiInfo.RefName, GuiInfo.Reference != null ? GuiInfo.Reference.Namespace : null).GetList<object>(GuiInfo.DataSourceFilter);
                    RowData = rsData.Value;
                    RenderOption();
                    TabEditor.DataSearchEntry.Add(GuiInfo.DataSourceFilter + GuiInfo.RefName, rsData.Value);
                }
            });
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
            _select.Add(new HTMLOptionElement() { Text = "--Chọn dữ diệu--", Selected = false, Value = null });
            RowData.ForEach(x =>
            {
                _select.Add(new HTMLOptionElement() { Text = Utils.FormatEntity(GuiInfo.FormatData, x).DecodeSpecialChar(), Selected = Value == x[IdField]?.ToString(), Value = x[IdField].ToString(), Title = Utils.FormatEntity(GuiInfo.FormatEntity, x).DecodeSpecialChar() });
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
              var id = e.params.data.id;
              var math = System.Linq.Enumerable.from(seft.RowData, System.Object).firstOrDefault(function (x) {
                  return x[Core.Components.EditableComponent.IdField] === id;
              }, null);
              seft.EntrySelected(math);
            });
             */
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
            _select.Value = _value?.ToString();
            UpdateValue();
        }

        private void UpdateValue()
        {
            if (!Dirty)
            {
                OriginalText = _select.Title;
                DOMContentLoaded?.Invoke();
                OldValue = _value.ToString();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0031:Use null propagation", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1005:Delegate invocation can be simplified.", Justification = "<Pending>")]
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
            _value = rowData[IdField]?.ToString();
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
            _value = Entity?.GetPropValue(GuiInfo.FieldName)?.ToString();
            if (_value is null)
            {
                Matched = null;
                _select.Value = null;
                UpdateValue();
                /*@
                $("#" + this.idGuid).val(null);
                $("#" + this.idGuid).trigger("change.select2");
                */
                return;
            }
            FindMatchTextAsync(force);
            /*@
                $("#" + this.idGuid).val(this._value);
                $("#" + this.idGuid).trigger("change.select2");
                */
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
            Validate(ValidationRule.Equal, _value, (string value, string ruleValue) => value == ruleValue);
            Validate(ValidationRule.NotEqual, _value, (string value, string ruleValue) => value != ruleValue);
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
