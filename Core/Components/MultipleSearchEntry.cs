using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Components
{
    public class MultipleSearchEntry : SearchEntry
    {
        private const string MultipleClass = "multiple";
        private bool _isStringSource;
        private HTMLButtonElement _toggleButton;

        public MultipleSearchEntry(Component ui) : base(ui)
        {
        }

        public override void Render()
        {
            _isStringSource = Entity != null && Entity.GetType().GetComplexPropType(FieldName, Entity).Equals(typeof(string));
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
            var source = Utils.GetPropValue(Entity, FieldName);
            if (source == null)
            {
                return;
            }
            ICollection<string> list = null;
            if (_isStringSource)
            {
                list = (source as string).Split(",").Where(x => x.HasAnyChar()).ToList();
            }
            else
            {
                list = source.As<ICollection<string>>();
            }
            list.Except(_listValues).ToArray().ForEach(_listValues.Add);
        }

        private readonly List<string> _listValues = new List<string>();

        public List<string> ListValues
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
                ListValues = new List<string>();
                _toggleButton.SetAttribute("title", "Chọn tất cả");
            }
            else
            {
                ListValues = null;
                _toggleButton.SetAttribute("title", "Hủy chọn");
            }
            FindMatchText();
        }

        private void SetEntityValue(ICollection<string> value)
        {
            if (_isStringSource)
            {
                Entity.SetComplexPropValue(FieldName, value.Combine());
            }
            else
            {
                Entity.SetComplexPropValue(FieldName, value);
            }
        }

        public List<object> MatchedItems { get; set; } = new List<object>();

        public override void FindMatchText()
        {
            if (EmptyRow || ProcessLocalMatch())
            {
                return;
            }
            var values = ListValues;
            ClearTagIfNotExists();
            if (MatchedItems.HasElement() && values.Except(MatchedItems.Select(x => x[IdField].As<string>())).Nothing())
            {
                SetMatchedValue();
                return;
            }
            Client.Instance.ComQuery(new SqlViewModel
            {
                ComId = GuiInfo.Id,

            }).Done(ds => {
                MatchedItems = ds.Length > 0 ? ds[0].ToList() : MatchedItems;
                SetMatchedValue();
            });
        }

        protected override bool ProcessLocalMatch()
        {
            var isLocalMatched = _gv != null && RowData.Data.HasElement() || GuiInfo.LocalData != null;
            if (isLocalMatched)
            {
                var rows = GuiInfo.LocalData.Nothing() ? RowData.Data : GuiInfo.LocalData;
                MatchedItems = rows.Where(x => _listValues.Contains(x[IdField]?.ToString())).ToList();
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
                var item = MatchedItems.FirstOrDefault(x => x[IdField]?.ToString() == ListValues[i]);
                RenderTag(item);
            }
        }

        private void ClearTagIfNotExists()
        {
            var tags =
                from HTMLElement tag in ParentElement.QuerySelectorAll("div > span")
                let id = tag.Dataset["id"]
                where id != null && !ListValues.Contains(id)
                select tag;
            foreach (var tag in tags)
            {
                tag.Remove();
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
            Html.Instance.Button.ClassName("fa fa-times").End.Event(EventType.Click, () =>
            {
                var oldList = ListValues.ToList();
                MatchedItems.Remove(item);
                var id = item[IdField]?.ToString();
                while (ListValues.Contains(id))
                {
                    ListValues.Remove(id);
                }
                ListValues = ListValues;
                Dirty = true;
                FindMatchText();
                UserInput?.Invoke(new ObservableArgs { NewData = ListValues, OldData = oldList, EvType = EventType.Change });
                tag.Remove();
                this.DispatchEvent(GuiInfo.Events, EventType.Change, Entity, ListValues, oldList).Done();
            }).End.Render();
        }

        protected override void EntrySelected(object rowData)
        {
            Window.ClearTimeout(_waitForDispose);
            if (rowData is null)
            {
                return;
            }

            var id = rowData[IdField]?.ToString();
            if (ListValues is null)
            {
                ListValues = new List<string>();
            }
            if (!ListValues.Contains(id))
            {
                ListValues.Add(id);
                MatchedItems.Add(rowData);
            }
            else
            {
                ListValues.Remove(id);
                var exist = MatchedItems.FirstOrDefault(x => x[IdField]?.ToString() == id);
                MatchedItems.Remove(exist);
            }
            ListValues = ListValues;
            Dirty = true;
            FindMatchText();
            _input.Focus();
            UserInput?.Invoke(new ObservableArgs { NewData = ListValues, OldData = ListValues, NewMatch = rowData, EvType = EventType.Change });
            this.DispatchEvent(GuiInfo.Events, EventType.Change, Entity).Done();
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            TryParseData();
            FindMatchText();
        }
    }
}
