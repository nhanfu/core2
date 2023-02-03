using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Components
{
    public class MultipleSearchEntry : SearchEntry
    {
        private const string MultipleClass = "multiple";
        private bool _isStringSource;
        private readonly string _fieldName;
        private HTMLButtonElement _toggleButton;

        public MultipleSearchEntry(Component ui) : base(ui)
        {
            _fieldName = GuiInfo.FieldName;
        }

        public override void Render()
        {
            _isStringSource = Entity != null && Entity.GetType().GetComplexPropType(_fieldName, Entity).Equals(typeof(string));
            base.Render();
            Element.ParentElement.AddClass(MultipleClass);
            TryParseData();
            FindMatchText();
        }

        private void TryParseData()
        {
            if (Entity is null)
            {
                return;
            }
            var source = Entity.GetComplexPropValue(_fieldName);
            if (source == null)
            {
                return;
            }
            ICollection<int> list = null;
            if (_isStringSource)
            {
                list = (source as string).Split(",").Select(x => x.TryParseInt() ?? 0).Where(x => x != 0).ToList();
            }
            else
            {
                list = source.As<ICollection<int>>();
            }
            list.Except(_listValues).ToArray().ForEach(_listValues.Add);
        }

        private readonly List<int> _listValues = new List<int>();

        public List<int> ListValues
        {
            get => _listValues;
            set
            {
                if (value.Nothing())
                {
                    _listValues?.Clear();
                }
                else
                {
                    if (_listValues != value)
                    {
                        _listValues.Clear();
                        value.Distinct().ForEach(_listValues.Add);
                    }
                }

                SetEntityValue(value);
                CascadeField();
                PopulateFields();
            }
        }

        internal override void GridResultDomLoaded()
        {
            base.GridResultDomLoaded();
            if (_toggleButton != null)
            {
                return;
            }
            Html.Take(_gv.Element).Div.ClassName("dropdown-toolbar")
            .Button.Event(EventType.Click, ToggleAllRecord).ClassName("fa fa-check").Attr("title", "Chọn tất cả").End
            .Button.Event(EventType.Click, Dispose).ClassName("fa fa-times").Attr("title", "Đóng gợi ý");
            _toggleButton = Html.Context.PreviousElementSibling as HTMLButtonElement;
        }

        public void ToggleAllRecord()
        {
            var value = ListValues;
            if (value is null)
            {
                ListValues = new List<int>();
                _toggleButton.SetAttribute("title", "Chọn tất cả");
            }
            else
            {
                ListValues = null;
                _toggleButton.SetAttribute("title", "Hủy chọn");
            }
            FindMatchText();
        }

        private void SetEntityValue(ICollection<int> value)
        {
            if (_isStringSource)
            {
                Entity.SetComplexPropValue(_fieldName, value.Combine());
            }
            else
            {
                Entity.SetComplexPropValue(_fieldName, value);
            }
        }

        public List<object> MatchedItems { get; set; } = new List<object>();

        public override void FindMatchText(int delay = 250)
        {
            if (EmptyRow || ProcessLocalMatch())
            {
                return;
            }
            var values = ListValues;
            ClearTagIfNotExists();
            if (MatchedItems.HasElement() && values.Except(MatchedItems.Select(x => x[IdField].As<int>())).Nothing())
            {
                SetMatchedValue();
                return;
            }
            Task.Run(async () =>
            {
                OdataResult<object> source;
                var formatted = FormattedDataSource;
                if (formatted.StartsWith("/"))
                {
                    formatted = OdataExt.ApplyClause(formatted, $"Id in ({values.Combine()})");
                    source = await new Client(GuiInfo.RefName).GetList<object>(formatted);
                }
                else
                {
                    source = await new Client(GuiInfo.RefName).LoadById(values.Combine());
                }
                if (source is null || source.Value is null)
                {
                    return;
                }

                MatchedItems = source.Value;
                SetMatchedValue();
            });
        }

        protected override bool ProcessLocalMatch()
        {
            var isLocalMatched = _gv != null && RowData.Data.HasElement() || GuiInfo.LocalData != null;
            if (isLocalMatched)
            {
                var rows = GuiInfo.LocalData.Nothing() ? RowData.Data : GuiInfo.LocalData;
                MatchedItems = rows.Where(x => _listValues.Contains((int)x[IdField])).ToList();
            }
            if (MatchedItems.HasElement() && MatchedItems.Count == _listValues.Count)
            {
                SetMatchedValue();
                return true;
            }
            return false;
        }

        public override void SetMatchedValue()
        {
            _input.Value = string.Empty;
            ClearTagIfNotExists();
            for (var i = 0; i < ListValues.Count; i++)
            {
                var item = MatchedItems.FirstOrDefault(x => x[IdField].As<int>() == ListValues[i]);
                RenderTag(item);
            }
        }

        private void ClearTagIfNotExists()
        {
            var tags = ParentElement.QuerySelectorAll("div > span");
            foreach (HTMLElement tag in tags)
            {
                var id = tag.Dataset["id"];
                if (id != null && !ListValues.Contains(id.TryParseInt() ?? 0))
                {
                    tag.Remove();
                }
            }
        }

        private void RenderTag(object item)
        {
            if (item is null)
            {
                return;
            }
            var idAttr = item[IdField].ToString();
            var exist = Element.ParentElement.QuerySelector($"span[data-id='{idAttr}']");
            if (exist != null)
            {
                return;
            }
            Html.Take(Element.ParentElement).Span.Attr("data-id", idAttr).InnerHTML(GetMatchedText(item));
            var tag = Html.Context;
            Element.ParentElement.InsertBefore(Html.Context, _input);
            Html.Instance.Button.ClassName("fa fa-times").End.Event(EventType.Click, async () =>
            {
                var oldList = ListValues.ToList();
                MatchedItems.Remove(item);
                var id = item[IdField].As<int>();
                while (ListValues.Contains(id))
                {
                    ListValues.Remove(id);
                }
                ListValues = ListValues;
                Dirty = true;
                FindMatchText();
                if (UserInput != null)
                {
                    UserInput.Invoke(new ObservableArgs { NewData = ListValues, OldData = oldList });
                }
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity, ListValues, oldList);
                tag.Remove();
            }).End.Render();
        }

        protected override void EntrySelected(object rowData)
        {
            Window.ClearTimeout(_waitForDispose);
            if (rowData is null)
            {
                return;
            }

            var id = rowData[IdField].As<int>();
            if (ListValues is null)
            {
                ListValues = new List<int>();
            }
            if (!ListValues.Contains(id))
            {
                ListValues.Add(id);
                MatchedItems.Add(rowData);
            }
            else
            {
                ListValues.Remove(id);
                var exist = MatchedItems.FirstOrDefault(x => x[IdField].As<int>() == id);
                MatchedItems.Remove(exist);
            }
            ListValues = ListValues;
            Dirty = true;
            FindMatchText();
            _input.Focus();
            if (UserInput != null)
            {
                UserInput.Invoke(new ObservableArgs { NewData = ListValues, OldData = ListValues, NewMatch = rowData });
            }
            Task.Run(async () =>
            {
                await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, Entity);
            });
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            TryParseData();
            FindMatchText();
        }
    }
}
