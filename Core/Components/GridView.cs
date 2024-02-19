using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Core.Structs;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;
using TextAlign = Core.Enums.TextAlign;
using Position = Core.MVVM.PositionEnum;
using Direction = Core.MVVM.Direction;

namespace Core.Components
{
    public class GridView : ListView
    {
        private const string SummaryClass = "summary";
        private const int CellCountNoSticky = 50;
        public List<HTMLElement> _summarys = new List<HTMLElement>();
        public HTMLElement LastThClick;
        public int? LastNumClick;
        public bool AutoFocus = false;
        public bool LoadRerender = false;
        public bool _waitingLoad;
        public int _renderPrepareCacheAwaiter;
        public HTMLElement DataTable { get; set; }
        public static Component ToolbarColumn = new Component
        {
            StatusBar = true,
            ShortDesc = string.Empty,
            Frozen = true
        };

        public GridView(Component ui) : base(ui)
        {
            DOMContentLoaded += DOMContentLoadedHandler;
        }

        protected virtual void DOMContentLoadedHandler()
        {
            if (Meta.IsSumary)
            {
                AddSummaries();
            }
            PopulateFields();
        }

        private void PopulateFields()
        {
            if (!Meta.PopulateField.IsNullOrWhiteSpace())
            {
                var fields = Meta.PopulateField.Split(",");
                if (fields.Length > 0)
                {
                    EditForm.UpdateView(true, componentNames: fields);
                }
            }
        }

        protected override void Rerender()
        {
            LoadRerender = true;
            Header = Header.Where(x => !x.Hidden).ToList();
            RenderTableHeader(Header);
            if (Editable)
            {
                AddNewEmptyRow();
            }
            RenderContent();
            StickyColumn(this);
            if (!Editable && RowData.Data.Nothing())
            {
                NoRecordFound();
            }
            else
            {
                DisposeNoRecord();
            }
            if (Editable)
            {
                MainSection.Element.AddEventListener(EventType.ContextMenu, BodyContextMenuHandler);
                EmptySection.Element.AddEventListener(EventType.ContextMenu, BodyContextMenuHandler);
            }
            RenderIndex();
        }

        private void StickyColumn(EditableComponent rows, string top = null)
        {
            var shouldStickEle = new string[] { "th", "td" };
            var frozen = rows.FilterChildren<EditableComponent>(predicate: x => x.Meta != null && x.Meta.Frozen, ignorePredicate: x => x is ListViewSearch).ToArray();
            frozen.ForEach(x =>
            {
                HTMLElement cell = x.Element;
                var isCell = shouldStickEle.Contains(x.Element.TagName.ToLowerCase());
                if (!isCell)
                {
                    cell = x.Element.Closest("td");
                }
                if (top.HasAnyChar())
                {
                    Html.Take(cell).Sticky(top: top);
                }
                else
                {
                    Html.Take(cell).Sticky(left: 0.ToString());
                }
            });
        }

        internal override void AddSections()
        {
            if (HeaderSection?.Element != null)
            {
                return;
            }
            var html = Html.Take(ParentElement);
            var id = "collapse" + Meta.Id;
            var idtb = "tb" + Meta.Id;
            if (Meta.IsCollapsible)
            {
                html.Div.ClassName("card mb-0")
                    .Div.ClassName("card-header")
                    .H5.ClassName("mb-0")
                    .A
                    .ClassName("btn btn-primary")
                    .DataAttr("toggle", "collapse").Href("#" + id)
                    .Attr("aria-expanded", "false")
                    .Attr("aria-controls", id).Text(Meta.Label).EndOf(".card");
            }
            html.Div.Event(EventType.KeyDown, (e) => HotKeyF6Handler(e, e.KeyCodeEnum())).ClassName("grid-wrapper " + (Meta.IsCollapsible ? "collapse multi-collapse" : "")).Id(id)
            .ClassName(Editable ? "editable" : string.Empty);
            Element = Html.Context;
            if (Meta.CanSearch)
            {
                Html.Instance.Div.ClassName("grid-toolbar search").End.Render();
            }
            ListViewSearch = new ListViewSearch(Meta)
            {
                Entity = new ListViewSearchVM()
            };
            if (Meta.DefaultAddStart.HasValue)
            {
                var pre = Convert.ToDouble(Meta.DefaultAddStart.Value);
                ListViewSearch.EntityVM.StartDate = DateTime.Now.AddDays(pre);
            }
            var lFrom = Window.LocalStorage.GetItem("FromDate" + Meta.Id);
            if (lFrom != null)
            {
                ListViewSearch.EntityVM.StartDate = DateTime.Parse(lFrom.ToString());
                if (ListViewSearch.EntityVM.StartDate < DateTime.Now.AddMonths(-2))
                {
                    ListViewSearch.EntityVM.StartDate = DateTime.Now.AddMonths(-2);
                }
            }
            else
            {
                if (Meta.ComponentType == nameof(VirtualGrid) && Meta.AddDate)
                {
                    ListViewSearch.EntityVM.StartDate = DateTime.Now.AddMonths(-2);
                }
            }
            if (Meta.DefaultAddEnd.HasValue)
            {
                var pre = Convert.ToDouble(Meta.DefaultAddEnd.Value);
                ListViewSearch.EntityVM.EndDate = DateTime.Now.AddDays(pre);
            }
            var lTo = Window.LocalStorage.GetItem("ToDate" + Meta.Id);
            if (lTo != null)
            {
                ListViewSearch.EntityVM.EndDate = DateTime.Parse(lTo.ToString());
            }
            AddChild(ListViewSearch);
            DataTable = Html.Take(Element).Div.ClassName("table-wrapper").Table.ClassName("table").Id(idtb).GetContext();
            Html.Instance.Thead.TabIndex(-1).End.TBody.ClassName("empty").End.TBody.End.TFooter.Render();

            FooterSection = new ListViewSection(Html.Context) { ParentElement = DataTable };
            AddChild(FooterSection);

            MainSection = new ListViewSection(FooterSection.Element.PreviousElementSibling) { ParentElement = DataTable };
            AddChild(MainSection);

            EmptySection = new ListViewSection(MainSection.Element.PreviousElementSibling) { ParentElement = DataTable };
            AddChild(EmptySection);

            HeaderSection = new ListViewSection(EmptySection.Element.PreviousElementSibling) { ParentElement = DataTable };
            AddChild(HeaderSection);
            Html.Instance.EndOf(".table-wrapper");
            RenderPaginator();
        }

        public void SwapList(int oldIndex, int newIndex)
        {
            var item = Header[oldIndex];
            Header.RemoveAt(oldIndex);
            Header.Insert(newIndex, item);
        }

        public void SwapHeader(int oldIndex, int newIndex)
        {
            var item = Header[oldIndex];
            Header.RemoveAt(oldIndex);
            Header.Insert(newIndex, item);
        }

        protected void ClickHeader(Event e, Component header)
        {
            var index = LastNumClick;
            var table = DataTable;
            if (LastNumClick != null)
            {
                /*@
                table.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                    if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                       return;
                    }
                    const cells = [].slice.call(row.querySelectorAll('th, td'));
                    cells[index].style.removeProperty("background-color");
                    cells[index].style.removeProperty("color");
                });
                */
            }
            var th = (e.Target as HTMLElement).Closest("th");
            var tr = th.ParentElement.QuerySelectorAll("th");
            index = tr.FindItemAndIndex(x => x == th).Item2;
            if (index < 0)
            {
                return;
            }
            LastThClick = th;
            LastNumClick = index;
            /*@
                table.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                    if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                        return;
                    }
                    const cells = [].slice.call(row.querySelectorAll('th, td'));
                    cells[index].style.backgroundColor= "#cbdcc2";
                    cells[index].style.color = "#000";
                });
                */
        }

        protected void FocusOutHeader(Event e, Component header)
        {
            var index = LastNumClick;
            var table = DataTable;
            if (LastNumClick != null)
            {
                /*@
                table.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                    if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                       return;
                    }
                    const cells = [].slice.call(row.querySelectorAll('th, td'));
                    cells[index].style.removeProperty("background-color");
                    cells[index].style.removeProperty("color");
                });
                */
            }
        }

        protected void ThHotKeyHandler(Event e, Component header)
        {
            if (Meta.Focus)
            {
                return;
            }
            var keyCode = e.KeyCodeEnum();
            if (keyCode == KeyCodeEnum.RightArrow)
            {
                e.StopPropagation();
                var th = (e.Target as HTMLElement).Closest("th");
                var tr = th.ParentElement.QuerySelectorAll("th");
                var index = tr.FindItemAndIndex(x => x == th).Item2;
                /*@
                th.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                        if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                            return;
                        }
                        const cells = [].slice.call(row.querySelectorAll('th, td'));
                        if(cells[0].classList.contains('summary-header')){
                            return;
                        }
                        var draggingColumnIndex = index;
                        var endColumnIndex = index + 1;
                        draggingColumnIndex > endColumnIndex
                            ? cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                                  cells[draggingColumnIndex],
                                  cells[endColumnIndex]
                              )
                            : cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                                  cells[draggingColumnIndex],
                                  cells[endColumnIndex].nextSibling
                              );
                        cells[draggingColumnIndex].style.backgroundColor= "#cbdcc2";
                });
                */
                SwapList(index - 1, index);
                SwapHeader(index, index + 1);
                ResetOrder();
                UpdateHeader();
                th.Focus();
            }
            else if (keyCode == KeyCodeEnum.LeftArrow)
            {
                e.StopPropagation();
                var th1 = (e.Target as HTMLElement).Closest("th");
                var tr1 = th1.ParentElement.QuerySelectorAll("th");
                var index1 = tr1.FindItemAndIndex(x => x == th1).Item2;
                /*@
                th1.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                        if(row.hasAttribute('virtualrow') || row.classList.contains('group-row')){
                            return;
                        }
                        const cells = [].slice.call(row.querySelectorAll('th, td'));
                        if(cells[0].classList.contains('summary-header')){
                            return;
                        }
                        var draggingColumnIndex = index1;
                        var endColumnIndex = index1 - 1;
                        draggingColumnIndex > endColumnIndex
                            ? cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                                  cells[draggingColumnIndex],
                                  cells[endColumnIndex]
                              )
                            : cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                                  cells[draggingColumnIndex],
                                  cells[endColumnIndex].nextSibling
                              );
                        cells[draggingColumnIndex].style.backgroundColor= "#cbdcc2";
                });
                */
                SwapList(index1 - 1, index1 - 2);
                SwapHeader(index1, index1 - 1);
                ResetOrder();
                UpdateHeader();
                th1.Focus();
            }
        }

        public void FilterInSelected(object e)
        {
            var hotKeyModel = e.As<HotKeyModel>();
            if (_waitingLoad)
            {
                Window.ClearTimeout(_renderPrepareCacheAwaiter);
            }
            if (hotKeyModel.Operator is null)
            {
                return;
            }
            var header = Header.FirstOrDefault(x => x.FieldName == hotKeyModel.FieldName);
            var subFilter = string.Empty;
            var lastFilter = Window.LocalStorage.GetItem("LastSearch" + Meta.Id + header.Id);
            if (lastFilter != null)
            {
                subFilter = lastFilter.ToString();
            }
            var confirmDialog = new ConfirmDialog
            {
                Content = $"Nhập {header.ShortDesc} cần tìm " + hotKeyModel.OperatorText,
                NeedAnswer = true,
                MultipleLine = false,
                ComType = header.ComponentType == nameof(Datepicker) || header.ComponentType == nameof(Number) ? header.ComponentType : nameof(Textbox),
                Precision = header.Precision,
                PElement = MainSection.Element
            };
            confirmDialog.YesConfirmed += () =>
            {
                string value = null;
                string valueText = null;
                if (header.ComponentType == nameof(Datepicker))
                {
                    valueText = value = confirmDialog.Datepicker.Value.ToString();
                }
                else if (header.ComponentType == nameof(Number))
                {
                    valueText = confirmDialog.Number.GetValueText();
                    value = confirmDialog.Number.Value.ToString();
                }
                else
                {
                    valueText = confirmDialog.Textbox.Text.Trim().EncodeSpecialChar();
                    value = confirmDialog.Textbox.Text.Trim().EncodeSpecialChar();
                }
                Window.LocalStorage.SetItem("LastSearch" + Meta.Id + header.Id, value);
                if (CellSelected.Any(x => x.FieldName == hotKeyModel.FieldName && x.Operator == (int)OperatorEnum.In) && !hotKeyModel.Shift)
                {
                    CellSelected.FirstOrDefault(x => x.FieldName == hotKeyModel.FieldName && x.Operator == (int)OperatorEnum.In).Value = value;
                    CellSelected.FirstOrDefault(x => x.FieldName == hotKeyModel.FieldName && x.Operator == (int)OperatorEnum.In).ValueText = valueText;
                }
                else
                {
                    CellSelected.Add(new CellSelected
                    {
                        FieldName = hotKeyModel.FieldName,
                        FieldText = header.ShortDesc,
                        ComponentType = header.ComponentType,
                        Shift = hotKeyModel.Shift,
                        Value = value,
                        ValueText = valueText,
                        Operator = hotKeyModel.Operator,
                        OperatorText = hotKeyModel.OperatorText,
                    });
                }
                _summarys.Add(new HTMLElement());
                confirmDialog.Textbox.Text = null;
                ActionFilter();
            };
            confirmDialog.Entity = new { ReasonOfChange = string.Empty };
            confirmDialog.Render();
            if (!subFilter.IsNullOrWhiteSpace())
            {
                if (header.ComponentType == nameof(Datepicker))
                {
                    confirmDialog.Datepicker.Value = DateTime.Parse(subFilter);
                    var input = confirmDialog.Datepicker.Element as HTMLInputElement;
                    input.SelectionStart = 0;
                    input.SelectionEnd = subFilter.Length;
                }
                else if (header.ComponentType == nameof(Number))
                {
                    confirmDialog.Number.Value = Convert.ToDecimal(subFilter);
                    var input = confirmDialog.Number.Element as HTMLInputElement;
                    input.SelectionStart = 0;
                    input.SelectionEnd = subFilter.Length;
                }
                else
                {
                    confirmDialog.Textbox.Text = subFilter;
                    var input = confirmDialog.Textbox.Element as HTMLInputElement;
                    input.SelectionStart = 0;
                    input.SelectionEnd = subFilter.Length;
                }
            }
        }

        public override void ActionFilter()
        {
            if (CellSelected.Nothing())
            {
                NoCellSelected();
                return;
            }
            Spinner.AppendTo(DataTable);
            var dropdowns = CellSelected.Where(x => (!x.Value.IsNullOrWhiteSpace() || !x.ValueText.IsNullOrWhiteSpace()) && x.ComponentType == nameof(SearchEntry) || x.FieldName.Contains(".")).ToList();
            var groups = CellSelected.Where(x => x.FieldName.Contains(".")).ToList();
            FilterDropdownIds(dropdowns).Done(data =>
            {
                var index = 0;
                var lisToast = new List<string>();
                CellSelected.ForEach(cell =>
                {
                    index = BuildCondition(cell, data, index, lisToast);
                });
                Spinner.Hide();
                if (Meta.ComponentType == nameof(VirtualGrid) && Meta.CanSearch)
                {
                    HeaderSection.Element.Focus();
                }
                if (Meta.ComponentType == nameof(SearchEntry))
                {
                    var search = Parent as SearchEntry;
                    search?._input.Focus();
                }
                Toast.Success(lisToast.Combine("</br>"));
                ApplyFilter();
            });
        }

        private Task<string[][]> FilterDropdownIds(List<CellSelected> dropdowns)
        {
            var tcs = new TaskCompletionSource<string[][]>();
            var dataTask = dropdowns.Select(x =>
            {
                var header = Enumerable.FirstOrDefault(Header, y => y.FieldName == x.FieldName);
                if (!x.IsSearch)
                {
                    var filterOperation = AdvSearchOperation.Like;
                    if (x.Operator == (int)OperatorEnum.Lr)
                    {
                        filterOperation = AdvSearchOperation.StartWith;
                    }
                    else if (x.Operator == (int)OperatorEnum.Rl)
                    {
                        filterOperation = AdvSearchOperation.EndWidth;
                    }
                    else if (x.Operator == (int)OperatorEnum.NotIn)
                    {
                        filterOperation = AdvSearchOperation.NotLike;
                    }
                    var func = AdvOptionExt.OperationToSql[filterOperation];
                    var sqlFilter = string.Format(func, $"ds.[{header.FormatData}]", x.Value);
                    return Client.Instance.GetIds(new SqlViewModel
                    {
                        ComId = header.Id,
                        Where = sqlFilter,
                    });
                }
                else
                {
                    return Task.FromResult(new string[] { x.Value });
                }
            }).ToArray();
            Task.WhenAll(dataTask).Done(ds =>
            {
                tcs.TrySetResult(ds);
            });
            return tcs.Task;
        }

        private int BuildCondition(CellSelected cell, string[][] data, int index, List<string> lisToast)
        {
            var where = string.Empty;
            var hl = Header.FirstOrDefault(y => y.FieldName == cell.FieldName);
            string ids = null;
            var isNUll = cell.Value.IsNullOrWhiteSpace();
            AdvSearchOperation advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotIn : AdvSearchOperation.In;
            if (hl.FieldName == IdField)
            {
                where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} not in ({cell.Value})" : $"[ds].{cell.FieldName} in ({cell.Value})";
                lisToast.Add(cell.FieldText + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
            }
            else
            {
                if (hl.ComponentType == nameof(SearchEntry) && hl.FormatData.HasNonSpaceChar())
                {
                    if (isNUll)
                    {
                        advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} is not null" : $"[ds].{cell.FieldName} is null";
                    }
                    else
                    {
                        var idArr = data[index];
                        if (idArr.Any())
                        {
                            ids = idArr.Combine();
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} not in ({ids})" : $"[ds].{cell.FieldName} in ({ids})";
                        }
                        else
                        {
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} != {cell.Value}" : $"[ds].{cell.FieldName} = {cell.Value}";
                        }
                        index++;
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == "Input" || hl.ComponentType == nameof(Textbox) || hl.ComponentType == "Textarea")
                {
                    if (isNUll)
                    {
                        advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        where = cell.Operator == (int)OperatorEnum.NotIn ? $"([ds].{cell.FieldName} is not null and [ds].{cell.FieldName} != '')" : $"([ds].{cell.FieldName} is null or [ds].{cell.FieldName} = '')";
                    }
                    else
                    {
                        advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotLike : AdvSearchOperation.Like;
                        where = cell.Operator == (int)OperatorEnum.NotIn ? $"(CHARINDEX(N'{cell.Value}', [ds].{cell.FieldName}) = 0 or [ds].{cell.FieldName} is null)" : $"CHARINDEX(N'{cell.Value}', [ds].{cell.FieldName}) > 0";
                        if (cell.Operator == (int)OperatorEnum.Lr)
                        {
                            advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotStartWith : AdvSearchOperation.StartWith;
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $" [ds].{cell.FieldName} not like N'{cell.Value}%' or [ds].{cell.FieldName} is null)" : $" [ds].{cell.FieldName} like N'{cell.Value}%'";
                        }
                        if (cell.Operator == (int)OperatorEnum.Rl)
                        {
                            advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEndWidth : AdvSearchOperation.EndWidth;
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $" [ds].{cell.FieldName} not like N'%{cell.Value}' or [ds].{cell.FieldName} is null)" : $" [ds].{cell.FieldName} like N'%{cell.Value}'";
                        }
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == nameof(Number) || (hl.ComponentType == "Label" && hl.FieldName.Contains("Id")))
                {
                    if (cell.Operator == (int)OperatorEnum.NotIn || cell.Operator == (int)OperatorEnum.In)
                    {
                        if (isNUll)
                        {
                            advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} is not null" : $"[ds].{cell.FieldName} is null";
                        }
                        else
                        {
                            advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqual : AdvSearchOperation.Equal;
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} != {cell.Value.Replace(",", "")}" : $"[ds].{cell.FieldName} = {cell.Value.Replace(",", "")}";
                        }
                    }
                    else
                    {
                        if (cell.Operator == (int)OperatorEnum.Gt || cell.Operator == (int)OperatorEnum.Lt)
                        {
                            where = cell.Operator == (int)OperatorEnum.Gt ? $"[ds].{cell.FieldName} > {cell.Value}" : $"[ds].{cell.FieldName} < {cell.Value}";
                            advo = cell.Operator == (int)OperatorEnum.Gt ? AdvSearchOperation.GreaterThan : AdvSearchOperation.LessThan;
                        }
                        else if (cell.Operator == (int)OperatorEnum.Ge || cell.Operator == (int)OperatorEnum.Le)
                        {
                            where = cell.Operator == (int)OperatorEnum.Ge ? $"[ds].{cell.FieldName} >= {cell.Value}" : $"[ds].{cell.FieldName} <= {cell.Value}";
                            advo = cell.Operator == (int)OperatorEnum.Ge ? AdvSearchOperation.GreaterThanOrEqual : AdvSearchOperation.LessThanOrEqual;
                        }
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == nameof(Checkbox))
                {
                    if (isNUll)
                    {
                        advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} is not null" : $"[ds].{cell.FieldName} is null";
                    }
                    else
                    {
                        where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} != {(cell.Value == "true" ? "1" : "0")}" : $"[ds].{cell.FieldName} = {(cell.Value == "true" ? "1" : "0")}";
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == nameof(Datepicker))
                {
                    cell.Value = cell.Value.DecodeSpecialChar();
                    cell.ValueText = cell.Value.DecodeSpecialChar();
                    if (cell.Operator == (int)OperatorEnum.NotIn || cell.Operator == (int)OperatorEnum.In)
                    {
                        if (isNUll)
                        {
                            where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} is not null" : $"[ds].{cell.FieldName} is null";
                            advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        }
                        else
                        {
                            try
                            {
                                var va = DateTimeOffset.ParseExact(cell.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                if (cell.Operator == (int)OperatorEnum.NotIn || cell.Operator == (int)OperatorEnum.In)
                                {
                                    where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} != '{va:yyyy-MM-dd}'" : $"[ds].{cell.FieldName} = '{va:yyyy-MM-dd}'";
                                    advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualDatime : AdvSearchOperation.EqualDatime;
                                }
                            }
                            catch
                            {
                                var va = DateTimeOffset.ParseExact(cell.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                where = cell.Operator == (int)OperatorEnum.NotIn ? $"[ds].{cell.FieldName} != '{va:yyyy-MM-dd}'" : $"[ds].{cell.FieldName} = '{va:yyyy-MM-dd}'";
                                advo = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualDatime : AdvSearchOperation.EqualDatime;
                            }
                        }
                    }
                    else
                    {
                        if (!isNUll)
                        {
                            DateTime va;
                            try
                            {
                                va = DateTime.ParseExact(cell.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                va = DateTime.ParseExact(cell.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            }
                            if (cell.Operator == (int)OperatorEnum.Gt || cell.Operator == (int)OperatorEnum.Lt)
                            {
                                where = cell.Operator == (int)OperatorEnum.Gt ? $"[ds].{cell.FieldName} > '{va:yyyy-MM-dd HH:mm}'" : $"[ds].{cell.FieldName} < '{va:yyyy-MM-dd HH:mm}'";
                                advo = cell.Operator == (int)OperatorEnum.Gt ? AdvSearchOperation.GreaterThanDatime : AdvSearchOperation.LessThanDatime;
                            }
                            else if (cell.Operator == (int)OperatorEnum.Ge || cell.Operator == (int)OperatorEnum.Le)
                            {
                                where = cell.Operator == (int)OperatorEnum.Ge ? $"[ds].{cell.FieldName} >= '{va:yyyy-MM-dd HH:mm}'" : $"[ds].{cell.FieldName} <= '{va:yyyy-MM-dd HH:mm}'";
                                advo = cell.Operator == (int)OperatorEnum.Ge ? AdvSearchOperation.GreaterEqualDatime : AdvSearchOperation.LessEqualDatime;
                            }
                        }
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
            }
            var value = ids ?? cell.Value;
            if (AdvSearchVM.Conditions.Any(x => x.Field.FieldName == cell.FieldName
            && x.CompareOperatorId == advo
            && (x.CompareOperatorId == AdvSearchOperation.Like || x.CompareOperatorId == AdvSearchOperation.In || x.CompareOperatorId == AdvSearchOperation.EqualDatime)) && !cell.Shift && !cell.Group)
            {
                AdvSearchVM.Conditions.FirstOrDefault(x => x.Field.FieldName == cell.FieldName && x.CompareOperatorId == advo).Value = value.IsNullOrWhiteSpace() ? cell.ValueText : value;
                Wheres.FirstOrDefault(x => x.Condition.Contains($"[ds].{cell.FieldName}")).Condition = where;
            }
            else
            {
                if (!AdvSearchVM.Conditions.Any(x => x.Field.FieldName == cell.FieldName && x.CompareOperatorId == advo && x.Value == cell.Value))
                {
                    if (cell.ComponentType == "Input" && cell.Value.IsNullOrWhiteSpace())
                    {
                        AdvSearchVM.Conditions.Add(new FieldCondition
                        {
                            Field = hl,
                            CompareOperatorId = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull,
                            LogicOperatorId = cell.Operator == (int)OperatorEnum.NotIn ? LogicOperation.And : LogicOperation.Or,
                            Value = null,
                            Group = true
                        });
                        AdvSearchVM.Conditions.Add(new FieldCondition
                        {
                            Field = hl,
                            CompareOperatorId = cell.Operator == (int)OperatorEnum.NotIn ? AdvSearchOperation.NotEqual : AdvSearchOperation.Equal,
                            LogicOperatorId = cell.Operator == (int)OperatorEnum.NotIn ? LogicOperation.And : LogicOperation.Or,
                            Value = string.Empty,
                            Group = true
                        });
                    }
                    else
                    {
                        if (hl.FieldName.Contains("."))
                        {
                            var format = hl.FieldName.Split(".").FirstOrDefault() + "Id";
                            hl.FieldName = format;
                            AdvSearchVM.Conditions.Add(new FieldCondition
                            {
                                Field = hl,
                                CompareOperatorId = advo,
                                LogicOperatorId = cell.Logic ?? LogicOperation.And,
                                Value = value.IsNullOrWhiteSpace() ? cell.ValueText : value,
                                Group = cell.Group
                            });
                        }
                        else
                        {
                            AdvSearchVM.Conditions.Add(new FieldCondition
                            {
                                Field = hl,
                                CompareOperatorId = advo,
                                LogicOperatorId = cell.Logic ?? LogicOperation.And,
                                Value = value.IsNullOrWhiteSpace() ? cell.ValueText : value,
                                Group = cell.Group
                            });
                        }
                    }
                    Wheres.Add(new Where()
                    {
                        Condition = where,
                        Group = cell.Group
                    });
                }
            }

            return index;
        }

        private void NoCellSelected()
        {
            MainSection.DisposeChildren();
            ApplyFilter();
            if (Meta.ComponentType == nameof(VirtualGrid) && Meta.CanSearch)
            {
                HeaderSection.Element.Focus();
            }
            if (Meta.ComponentType == nameof(SearchEntry))
            {
                var search = Parent as SearchEntry;
                search?._input.Focus();
            }
        }

        private void ApplyLocal()
        {
            var tb = DataTable as HTMLTableElement;
            HTMLCollection rows;
            if (Meta.TopEmpty)
            {
                rows = tb.TBodies.LastOrDefault().Children;
            }
            else
            {
                rows = tb.TBodies.FirstOrDefault().Children;
            }
            if (CellSelected.Nothing())
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    rows[i].RemoveClass("d-none");
                }
                return;
            }
            LastElementFocus = null;
            var listNone = new List<HTMLElement>();
            var header = Header.IndexOf(y => y.FieldName == CellSelected.FirstOrDefault().FieldName);
            CellSelected.ForEach(cell =>
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    var cells = rows[i].Children;
                    if (cells[header] is null)
                    {
                        continue;
                    }
                    var cellText = cells[header].TextContent ?? string.Empty;
                    if (cell.Operator == (int)OperatorEnum.In)
                    {
                        if (!(cellText.ToLowerCase().IndexOf(cell.ValueText.ToLowerCase()) > -1))
                        {
                            if (!listNone.Any(x => x == rows[i]))
                            {
                                listNone.Add(rows[i]);
                            }
                        }
                    }
                    else
                    {
                        if (!(cellText.ToLowerCase().IndexOf(cell.ValueText.ToLowerCase()) <= -1))
                        {
                            if (!listNone.Any(x => x == rows[i]))
                            {
                                listNone.Add(rows[i]);
                            }
                        }
                    }
                }
            });
            for (var i = 0; i < rows.Length; i++)
            {
                var cells = rows[i].Children;
                if (listNone.Any(x => x == rows[i].Cast<HTMLElement>()))
                {
                    rows[i].AddClass("d-none");
                }
                else
                {
                    if (LastElementFocus is null)
                    {
                        LastElementFocus = cells[header];
                    }
                    rows[i].RemoveClass("d-none");
                }
            }
            if (LastElementFocus != null)
            {
                LastElementFocus.Focus();
                LastElementFocus = null;
            }
        }

        public void FilterSelected(HotKeyModel hotKeyModel)
        {
            if (hotKeyModel.Operator is null)
            {
                return;
            }
            if (!CellSelected.Any(x => x.FieldName == hotKeyModel.FieldName && x.Value == hotKeyModel.Value && x.ValueText == hotKeyModel.ValueText && x.Operator == hotKeyModel.Operator))
            {
                var header = Header.FirstOrDefault(x => x.FieldName == hotKeyModel.FieldName);
                CellSelected.Add(new CellSelected
                {
                    FieldName = hotKeyModel.FieldName,
                    FieldText = header.ShortDesc,
                    ComponentType = header.ComponentType,
                    Value = hotKeyModel.Value,
                    ValueText = hotKeyModel.ValueText,
                    Operator = hotKeyModel.Operator,
                    OperatorText = hotKeyModel.OperatorText,
                    IsSearch = hotKeyModel.ActValue,
                });
                _summarys.Add(new HTMLElement());
            }
            ActionFilter();
        }

        public virtual void DisposeSumary()
        {
            if (_summarys.Any())
            {
                _summarys.ElementAtOrDefault(_summarys.Count - 1).Remove();
                _summarys.RemoveAt(_summarys.Count - 1);
            }
            if (LastListViewItem != null && LastElementFocus != null)
            {
                LastListViewItem.Focused = true;
                LastElementFocus.Focus();
            }
        }

        private void HiddenSumary()
        {
            _summarys.ElementAtOrDefault(_summarys.Count - 1).Hide();
        }

        public void SearchDisplayRows()
        {
            var table = DataTable as HTMLTableElement;
            var rows = table.TBodies.LastOrDefault().Children;
            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i].HasClass("virtual-row"))
                {
                    continue;
                }
                var cells = rows[i].ChildNodes;
                var found = false;
                for (var j = 0; j < cells.Length; j++)
                {
                    var htmlElement = cells[j] as HTMLElement;
                    var input = htmlElement.QuerySelector("input:first-child");
                    string cellText;
                    if (input != null)
                    {
                        cellText = input.GetPropValue("value").ToString();
                    }
                    else
                    {
                        cellText = cells[j].TextContent is null ? "" : cells[j].TextContent;
                    }
                    if (cellText.DecodeSpecialChar().ToLowerCase().IndexOf(ListViewSearch.EntityVM.FullTextSearch?.ToLowerCase().DecodeSpecialChar()) > -1)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    rows[i].RemoveClass("d-none");
                }
                else
                {
                    rows[i].AddClass("d-none");
                }
            }
        }

        public virtual void FocusCell(Event e, Component header)
        {
            var td = e.Target as HTMLElement;

            /*@
             var $table = $(e.target).closest('table');
             $table.find("tbody tr").removeClass("focus");
             $table.find("tbody td").removeClass("cell-selected");
             */

            td.Closest(ElementType.tr.ToString()).AddClass("focus");
            td.Closest(ElementType.td.ToString()).AddClass("cell-selected");
        }

        public void ActionKeyHandler(Event e, Component header, ListViewItem focusedRow, EditableComponent com, HTMLElement el, KeyCodeEnum? keyCode)
        {
            var fieldName = "";
            var text = "";
            var value = "";
            if (keyCode == KeyCodeEnum.F4 || keyCode == KeyCodeEnum.F8 ||
               keyCode == KeyCodeEnum.F9 || keyCode == KeyCodeEnum.F10 ||
               keyCode == KeyCodeEnum.F11 ||
               keyCode == KeyCodeEnum.F2 || keyCode == KeyCodeEnum.UpArrow ||
               keyCode == KeyCodeEnum.DownArrow || keyCode == KeyCodeEnum.Home ||
               keyCode == KeyCodeEnum.End || keyCode == KeyCodeEnum.Insert ||
               (e.CtrlOrMetaKey() && keyCode == KeyCodeEnum.D))
            {
                e.PreventDefault();
                e.StopPropagation();
                if (com is null)
                {
                    return;
                }
                fieldName = com.FieldName;
                switch (com.Meta.ComponentType)
                {
                    case nameof(SearchEntry):
                        value = focusedRow.Entity.GetPropValue(header.FieldName)?.ToString().EncodeSpecialChar();
                        break;
                    case nameof(Number):
                        value = focusedRow.Entity.GetPropValue(header.FieldName)?.ToString()?.Replace(",", "");
                        break;
                    case nameof(Checkbox):
                        value = com.GetValue()?.ToString()?.ToLower();
                        break;
                    default:
                        value = com.GetValue()?.ToString().EncodeSpecialChar();
                        break;
                }
                if (value is null)
                {
                    text = null;
                }
                else
                {
                    if (!com.Meta.Editable)
                    {
                        text = com.GetValueTextAct() is null ? null : com.GetValueTextAct().ToString().DecodeSpecialChar();
                    }
                    else
                    {
                        text = com.GetValueText() is null ? null : com.GetValueText().ToString().DecodeSpecialChar();
                    }
                }
            }

            switch (keyCode)
            {
                case KeyCodeEnum.F2:
                    FilterSelected(new HotKeyModel { Operator = 2, OperatorText = "Loại trừ", Value = value, FieldName = fieldName, ValueText = text, ActValue = true });
                    break;
                case KeyCodeEnum.F4:
                    ProcessFilterDetail(e, com, el, fieldName, text, value);
                    break;
                case KeyCodeEnum.F8:
                    ProcessHardDelete();
                    break;
                case KeyCodeEnum.F9:
                    FilterSelected(new HotKeyModel { Operator = 1, OperatorText = "Chứa", Value = value, FieldName = fieldName, ValueText = text, ActValue = true });
                    com.Focus();
                    break;
                case KeyCodeEnum.F11:
                    ProcessSort(e, com);
                    break;
                case KeyCodeEnum.UpArrow:
                    var currentItemUp = GetItemFocus();
                    if (currentItemUp.RowNo == 0)
                    {
                        return;
                    }
                    var upItemUp = AllListViewItem.Where(x => !x.GroupRow).FirstOrDefault(x => x.RowNo == (currentItemUp.RowNo - 1));
                    if (upItemUp is null)
                    {
                        if (Meta.CanAdd)
                        {
                            upItemUp = EmptySection.FirstChild as ListViewItem;
                        }
                        else
                        {
                            return;
                        }
                    }
                    CoppyValue(e, com, fieldName, currentItemUp, upItemUp);
                    break;
                case KeyCodeEnum.DownArrow:
                    var currentItemDown = GetItemFocus();
                    if (currentItemDown is null)
                    {
                        return;
                    }
                    var upItemDown = AllListViewItem.Where(x => !x.GroupRow).FirstOrDefault(x => x.RowNo == (currentItemDown.RowNo + 1));
                    if (upItemDown is null)
                    {
                        if (Meta.CanAdd)
                        {
                            upItemDown = EmptySection.FirstChild as ListViewItem;
                        }
                        else
                        {
                            return;
                        }
                    }
                    CoppyValue(e, com, fieldName, currentItemDown, upItemDown);
                    break;
                case KeyCodeEnum.LeftArrow:
                    if (!Meta.IsRealtime)
                    {
                        return;
                    }
                    e.PreventDefault();
                    e.StopPropagation();
                    var currentItemLeft = LastListViewItem;
                    var upItemLeft = currentItemLeft.Children.FirstOrDefault(x => x.Element.Closest(ElementType.td.ToString()) == com.Element.Closest(ElementType.td.ToString()).PreviousElementSibling);
                    if (upItemLeft is null || currentItemLeft.Children.Nothing())
                    {
                        return;
                    }
                    upItemLeft.ParentElement?.Focus();
                    upItemLeft.Focus();
                    if (upItemLeft.Meta.Editable && !upItemLeft.Disabled)
                    {
                        if (upItemLeft.Element is HTMLInputElement html)
                        {
                            html.SelectionStart = 0;
                            html.SelectionEnd = upItemLeft.GetValueText().Length;
                        }
                    }
                    break;
                case KeyCodeEnum.RightArrow:
                    if (!Meta.IsRealtime)
                    {
                        return;
                    }
                    e.PreventDefault();
                    e.StopPropagation();
                    var currentItemRight = LastListViewItem;
                    if (currentItemRight is null || currentItemRight.Children.Nothing())
                    {
                        return;
                    }
                    var upItemRight = currentItemRight.Children.FirstOrDefault(x => x.Element.Closest(ElementType.td.ToString()) == com.Element.Closest(ElementType.td.ToString()).NextElementSibling);
                    if (upItemRight is null)
                    {
                        return;
                    }
                    upItemRight.ParentElement?.Focus();
                    upItemRight.Focus();
                    if (upItemRight.Meta.Editable && !upItemRight.Disabled)
                    {
                        if (upItemRight.Element is HTMLInputElement html)
                        {
                            html.SelectionStart = 0;
                            html.SelectionEnd = upItemRight.GetValueText().Length;
                        }
                    }
                    break;
                case KeyCodeEnum.Home:
                    var lastSelected = GetSelectedRows().LastOrDefault();
                    var currentItemHome = AllListViewItem.FirstOrDefault();
                    if (currentItemHome != null)
                    {
                        currentItemHome.Focused = false;
                    }
                    DataTable.ParentElement.ScrollTop = 0;
                    RenderViewPort();
                    var upItemHome = AllListViewItem.FirstOrDefault();
                    if (upItemHome != null)
                    {
                        upItemHome.Focused = true;
                        var upComponent = upItemHome.FirstOrDefault(x => x.FieldName == fieldName);
                        var tdup = upComponent.Element.Closest(ElementType.td.ToString());
                        upItemHome.Focus();
                        tdup.Focus();
                    }
                    break;
                case KeyCodeEnum.End:
                    var lastSelectedEnd = GetSelectedRows().LastOrDefault();
                    var currentItemEnd = AllListViewItem.FirstOrDefault(x => x.Entity == lastSelectedEnd);
                    if (currentItemEnd != null)
                    {
                        currentItemEnd.Focused = false;
                    }
                    DataTable.ParentElement.ScrollTop = DataTable.ParentElement.ScrollHeight;
                    RenderViewPort();
                    var upItemEnd = AllListViewItem.LastOrDefault();
                    if (upItemEnd != null)
                    {
                        upItemEnd.Focused = true;
                        var upComponent = upItemEnd.FirstOrDefault(x => x.FieldName == fieldName);
                        var tdup = upComponent.Element.Closest(ElementType.td.ToString());
                        upItemEnd.Focus();
                        tdup.Focus();
                    }
                    break;
                case KeyCodeEnum.Insert:
                    var currentItemInsert = GetItemFocus();
                    currentItemInsert.Selected = !currentItemInsert.Selected;
                    break;
                case KeyCodeEnum.D:
                    if (e.CtrlOrMetaKey())
                    {
                        e.StopPropagation();
                        e.PreventDefault();
                        var currentItemD = GetItemFocus();
                        if (currentItemD.RowNo == 0)
                        {
                            return;
                        }
                        var upItemD = AllListViewItem.FirstOrDefault(x => x.RowNo == (currentItemD.RowNo - 1));
                        currentItemD.Entity.SetComplexPropValue(fieldName, upItemD.Entity.GetPropValue(com.FieldName));
                        var updated = currentItemD.FilterChildren(x => x.FieldName == com.FieldName).FirstOrDefault();
                        updated.Dirty = true;
                        Task.Run(async () =>
                        {
                            if (updated.Meta.ComponentType == nameof(SearchEntry))
                            {
                                updated.UpdateView();
                                var dropdown = com as SearchEntry;
                                updated.PopulateFields(dropdown.Matched);
                                await updated.DispatchEvent(updated.Meta.Events, EventType.Change, currentItemD.Entity, dropdown.Matched);
                            }
                            else
                            {
                                updated.UpdateView();
                                updated.PopulateFields();
                                await updated.DispatchEvent(updated.Meta.Events, EventType.Change, currentItemD.Entity);
                            }
                            await currentItemD.ListViewSection.ListView.DispatchEvent(upItemD.ListViewSection.ListView.Meta.Events, EventType.Change, upItemD.Entity);
                            if (Meta.IsRealtime)
                            {
                                currentItemD.PatchUpdateOrCreate();
                            }
                        });
                    }
                    break;
                default:
                    break;
            }
        }

        private void ProcessHardDelete()
        {
            if (Disabled)
            {
                return;
            }
            var selectedRows = GetSelectedRows().ToList();
            if (selectedRows.Nothing())
            {
                Toast.Warning("Vui lòng chọn dòng cần xóa");
                return;
            }
            var isOwner = selectedRows.All(x => Utils.IsOwner(x, false));
            var canDelete = CanDo(x => x.CanDelete && isOwner || x.CanDeleteAll);
            if (canDelete)
            {
                HardDeleteSelected(null);
            }
        }

        private void ProcessFilterDetail(Event e, EditableComponent com, HTMLElement el, string fieldName, string text, string value)
        {
            var menu = ContextMenu.Instance;
            menu.PElement = MainSection.Element;
            menu.Top = el.GetBoundingClientRect().Top;
            menu.Left = el.GetBoundingClientRect().Left;
            menu.MenuItems = new List<ContextMenuItem>
            {
                new ContextMenuItem { Icon = "fal fa-angle-double-right", Text = "Chứa", Click = FilterInSelected,
                        Parameter = new HotKeyModel { Operator = (int)OperatorEnum.In, OperatorText = "Chứa", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey()  } },
                new ContextMenuItem { Icon = "fal fa-not-equal", Text = "Không chứa", Click = FilterInSelected,
                        Parameter = new HotKeyModel { Operator=(int)OperatorEnum.NotIn,OperatorText= "Không chứa", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                new ContextMenuItem { Icon = "fal fa-hourglass-start", Text = "Trái phải", Click = FilterInSelected,
                        Parameter = new HotKeyModel { Operator = (int)OperatorEnum.Lr, OperatorText = "Trái phải", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey()  } },
                new ContextMenuItem { Icon = "fal fa-hourglass-end", Text = "Phải trái", Click = FilterInSelected,
                        Parameter = new HotKeyModel { Operator=(int)OperatorEnum.Rl,OperatorText= "Phải trái", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }}
            };
            if (com.Meta.ComponentType == nameof(Number) || com.Meta.ComponentType == nameof(Datepicker))
            {
                menu.MenuItems.AddRange(new List<ContextMenuItem>
                {
                    new ContextMenuItem { Icon = "fal fa-greater-than", Text = "Lớn hơn", Click = FilterInSelected,
                            Parameter = new HotKeyModel { Operator=(int)OperatorEnum.Gt, OperatorText= "Lớn hơn", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                    new ContextMenuItem { Icon = "fal fa-less-than", Text = "Nhỏ hơn", Click = FilterInSelected,
                            Parameter = new HotKeyModel { Operator=(int)OperatorEnum.Lt, OperatorText= "Nhỏ hơn", Value = value, FieldName = fieldName, ValueText=text, Shift = e.ShiftKey() }},
                    new ContextMenuItem { Icon = "fal fa-greater-than-equal", Text = "Lớn hơn bằng", Click = FilterInSelected,
                            Parameter = new HotKeyModel { Operator=(int)OperatorEnum.Ge, OperatorText= "Lớn hơn bằng", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                    new ContextMenuItem { Icon = "fal fa-less-than-equal", Text = "Nhỏ hơn bằng", Click = FilterInSelected,
                            Parameter = new  { Operator= (int)OperatorEnum.Le, OperatorText= "Nhỏ hơn bằng", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                });
            }
            menu.Render();
        }

        private void ProcessSort(Event e, EditableComponent com)
        {
            if (com.Meta.ComponentType == "Button")
            {
                return;
            }
            var th = HeaderSection.Children.FirstOrDefault(x => x.Meta.Id == com.Meta.Id);
            th.Element.RemoveClass("desc");
            th.Element.RemoveClass("asc");
            var fieldName = com.ComponentType == nameof(SearchEntry) ? com.Meta.FieldText : com.FieldName;
            var sort = new OrderBy
            {
                FieldName = fieldName,
                OrderbyDirectionId = OrderbyDirection.ASC,
                ComId = com.Meta.Id,
            };
            if (AdvSearchVM.OrderBy.Nothing())
            {
                AdvSearchVM.OrderBy = new List<OrderBy>() { sort };
                th.Element.AddClass("desc");
            }
            else
            {
                var existSort = AdvSearchVM.OrderBy.FirstOrDefault(x => x.FieldName == fieldName);
                if (existSort != null)
                {
                    AlterExistSort(th, existSort);
                }
                else
                {
                    var shiftKey = e.ShiftKey();
                    RemoveOtherSorts(shiftKey);
                    th.Element.AddClass("desc");
                    AdvSearchVM.OrderBy.Add(sort);
                }
            }
            LocalStorage.SetItem("OrderBy" + Meta.Id, AdvSearchVM.OrderBy);
            ReloadData().Done();
        }

        private void AlterExistSort(EditableComponent th, OrderBy existSort)
        {
            if (existSort.OrderbyDirectionId == OrderbyDirection.ASC)
            {
                existSort.OrderbyDirectionId = OrderbyDirection.DESC;
                th.Element.ReplaceClass("asc", "desc");
            }
            else
            {
                AdvSearchVM.OrderBy.Remove(existSort);
            }
        }

        private void RemoveOtherSorts(bool shiftKey)
        {
            if (shiftKey) return;
            HeaderSection.Children.ForEach(x =>
            {
                x.Element.RemoveClass("desc");
                x.Element.RemoveClass("asc");
            });
            AdvSearchVM.OrderBy.Clear();
        }

        internal virtual void RenderViewPort(bool count = true, bool firstLoad = false, int? skip = null)
        {
            return;
        }

        public void HotKeyF6Handler(Event e, KeyCodeEnum? keyCode)
        {
            switch (keyCode)
            {
                case KeyCodeEnum.F6:
                    e.PreventDefault();
                    e.StopPropagation();
                    if (_summarys.Any())
                    {

                        var lastElement = _summarys.LastOrDefault();
                        if (Meta.FilterLocal)
                        {
                            if (lastElement.InnerHTML == string.Empty)
                            {
                                CellSelected.RemoveAt(CellSelected.Count - 1);
                                ActionFilter();
                                _summarys.RemoveAt(_summarys.Count - 1);
                            }
                            else
                            {
                                if (lastElement.Style.Display.ToString() == "none")
                                {
                                    CellSelected.RemoveAt(CellSelected.Count - 1);
                                    ActionFilter();
                                    lastElement.Show();
                                }
                                else
                                {
                                    _summarys.RemoveAt(_summarys.Count - 1);
                                    lastElement.Remove();
                                }
                            }
                            return;
                        }
                        if (lastElement.InnerHTML == string.Empty)
                        {
                            CellSelected.RemoveAt(CellSelected.Count - 1);
                            Wheres.RemoveAt(Wheres.Count - 1);
                            var last = AdvSearchVM.Conditions.LastOrDefault();
                            if (last != null && last.Field.ComponentType == "Input" && last.Value.IsNullOrWhiteSpace())
                            {
                                AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                                AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                            }
                            else
                            {

                                AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                            }
                            ActionFilter();
                            _summarys.RemoveAt(_summarys.Count - 1);
                        }
                        else
                        {
                            if (_waitingLoad)
                            {
                                Window.ClearTimeout(_renderPrepareCacheAwaiter);
                            }
                            if (lastElement.Style.Display.ToString() == "none")
                            {
                                CellSelected.RemoveAt(CellSelected.Count - 1);
                                Wheres.RemoveAt(Wheres.Count - 1);
                                var last = AdvSearchVM.Conditions.LastOrDefault();
                                if (last != null && last.Field.ComponentType == "Input" && last.Value.IsNullOrWhiteSpace())
                                {
                                    AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                                    AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                                }
                                else
                                {

                                    AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                                }
                                ActionFilter();
                                lastElement.Show();
                            }
                            else
                            {
                                _summarys.RemoveAt(_summarys.Count - 1);
                                lastElement.Remove();
                            }
                        }
                    }
                    break;
                case KeyCodeEnum.F3:
                    e.PreventDefault();
                    e.StopPropagation();
                    GetRealTimeSelectedRows().Done(selected =>
                    {
                        if (selected.Count == 0)
                        {
                            selected = RowData.Data.ToList();
                        }
                        var numbers = Header.Where(x => x.ComponentType == nameof(Number)).ToList();
                        if (numbers.Count == 0)
                        {
                            Toast.Warning("Vui lòng cấu hình");
                            return;
                        }
                        var listString = numbers.Select(x =>
                        {
                            var val = selected.Select(k => k[x.FieldName]).Where(k => k != null).Select(y => Convert.ToDecimal(y)).Sum();
                            return x.ShortDesc + " : " + (val % 2 > 0 ? val.ToString("N2") : val.ToString("N0"));
                        });
                        Toast.Success(listString.Combine("</br>"), 6000);
                    });
                    break;
                case KeyCodeEnum.F1:
                    e.PreventDefault();
                    e.StopPropagation();
                    ToggleAll();
                    break;
                case KeyCodeEnum.U:
                    if (e.CtrlOrMetaKey())
                    {
                        if (Disabled || !Meta.CanAdd)
                        {
                            return;
                        }
                        e.PreventDefault();
                        e.StopPropagation();
                        DuplicateSelected(e, true);
                    }
                    break;
                default:
                    break;
            }
            if (LastListViewItem is null)
            {
                return;
            }
            if (LastListViewItem.Children is null)
            {
                return;
            }
            var com = LastListViewItem.Children.FirstOrDefault(x => x.Meta.Id == LastComponentFocus?.Id);
            if (com is null)
            {
                return;
            }
            ActionKeyHandler(e, LastComponentFocus, LastListViewItem, com, com.Element.Closest(ElementType.td.ToString()), keyCode);
        }

        private void CoppyValue(Event e, EditableComponent com, string fieldName, ListViewItem currentItem, ListViewItem upItem)
        {
            LastListViewItem = upItem;
            currentItem.Focused = false;
            upItem.Focused = true;
            if (fieldName.IsNullOrWhiteSpace())
            {
                return;
            }
            var nextcom = upItem.FilterChildren(x => x.Meta.Id == com.Meta.Id).FirstOrDefault();
            if (nextcom != null)
            {
                LastComponentFocus = nextcom.Meta;
                nextcom.ParentElement?.Focus();
                nextcom.Focus();
                if (nextcom.Meta.Editable && !nextcom.Disabled)
                {
                    if (nextcom.Element is HTMLInputElement html)
                    {
                        html.SelectionStart = 0;
                        html.SelectionEnd = nextcom.GetValueText().Length;
                    }
                }
                LastElementFocus = nextcom.Element;
                if (e.ShiftKey())
                {
                    upItem.Entity.SetComplexPropValue(fieldName, com.GetValue());
                    var updated = upItem.FilterChildren(x => x.FieldName == nextcom.FieldName).FirstOrDefault();
                    if (updated.Disabled || !updated.Meta.Editable)
                    {
                        return;
                    }
                    updated.Dirty = true;
                    Task.Run(async () =>
                    {
                        if (updated.Meta.ComponentType == nameof(SearchEntry))
                        {
                            updated.UpdateView();
                            var dropdown = com as SearchEntry;
                            updated.PopulateFields(dropdown.Matched);
                            await updated.DispatchEvent(updated.Meta.Events, EventType.Change, upItem.Entity, dropdown.Matched);
                        }
                        else
                        {
                            updated.UpdateView();
                            updated.PopulateFields();
                            await updated.DispatchEvent(updated.Meta.Events, EventType.Change, upItem.Entity);
                        }
                        await upItem.ListViewSection.ListView.DispatchEvent(upItem.ListViewSection.ListView.Meta.Events, EventType.Change, upItem.Entity);
                        if (Meta.IsRealtime)
                        {
                            upItem.PatchUpdateOrCreate();
                        }
                    });
                }
            }
        }

        public override async Task<ListViewItem> AddRow(object rowData, int index = 0, bool singleAdd = true)
        {
            var rowSection = await base.AddRow(rowData, index, singleAdd);
            StickyColumn(rowSection);
            RenderIndex();
            return rowSection;
        }

        public override void AddNewEmptyRow()
        {
            if (Disabled || !Editable || EmptySection?.Children.HasElement() == true)
            {
                return;
            }
            var emptyRowData = new object();
            if (!Meta.DefaultVal.IsNullOrWhiteSpace() && Utils.IsFunction(Meta.DefaultVal, out var fn))
            {
                var dfObj = fn.Call(this, this);
                dfObj.ForEachProp(x =>
                {
                    emptyRowData[x] = dfObj[x];
                });
            }
            emptyRowData[IdField] = null;
            var rowSection = RenderRowData(Header, emptyRowData, EmptySection, null, true);
            emptyRowData.ForEachProp((field, value) =>
            {
                rowSection.PatchModel.Add(new PatchDetail
                {
                    Field = field,
                    Value = value?.ToString()
                });
            });
            if (!Meta.TopEmpty)
            {
                DataTable.InsertBefore(MainSection.Element, EmptySection.Element);
            }
            else
            {
                DataTable.InsertBefore(EmptySection.Element, MainSection.Element);
            }
            this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterEmptyRowCreated, emptyRow).Done();
        }

        protected override List<Component> FilterColumns(List<Component> Component)
        {
            if (Component.Nothing()) return Component;
            var specificComponent = Component.Any(x => x.ComponentId == Meta.Id);
            if (specificComponent)
            {
                Component = Component.Where(x => x.ComponentId == Meta.Id).ToList();
            }
            else
            {
                Component = Component.Where(x => x.ComponentId == null).ToList();
            }

            var permission = EditForm.GetGridPolicies(Component.Select(x => x.Id).ToArray(), Utils.ComponentId);
            var headers = Component.Where(x => !x.Hidden)
                .Where(header => !header.IsPrivate || permission.Where(x => x.RecordId == header.Id).HasElementAndAll(policy => policy.CanRead))
                .Select(CalcTextAlign).OrderByDescending(x => x.Frozen).ThenByDescending(header => header.ComponentType == "Button")
                .ThenBy(x => x.Order).ToList();
            OrderHeaderGroup(headers);
            Header.Clear();
            Header.Add(ToolbarColumn);
            Header.AddRange(headers);
            Header = Header.Where(x => x != null).ToList();
            return Header;
        }

        public override Task ApplyFilter()
        {
            DataTable.ParentElement.ScrollTop = 0;
            return ReloadData(cacheHeader: true);
        }

        private void ColumnResizeHandler()
        {
            var self = this;
            /*@
             const createResizableTable = function (table) {
                if (table == null) return;
                const cols = table.querySelectorAll('th');
                [].forEach.call(cols, function (col) {
                    // Add a resizer element to the column
                    const resizer = document.createElement('div');
                    resizer.classList.add('resizer');

                    col.appendChild(resizer);

                    createResizableColumn(col, resizer);
                });
            };

            const createResizableColumn = function (col, resizer) {
                let x = 0;
                let w = 0;

                const mouseDownHandler = function (e) {
                    e.preventDefault();
                    x = e.clientX;

                    const styles = window.getComputedStyle(col);
                    w = parseInt(styles.width, 10);

                    document.addEventListener('mousemove', mouseMoveHandler);
                    document.addEventListener('mouseup', mouseUpHandler);

                    resizer.classList.add('resizing');
                };

                const mouseMoveHandler = function (e) {
                    e.preventDefault();
                    const dx = e.clientX - x;
                    col.style.width = `${w + dx}px`;
                    col.style.minWidth = `${w + dx}px`;
                    col.style.maxWidth = `${w + dx}px`;
                };

                const mouseUpHandler = function () {
                    self.UpdateHeader();
                    resizer.classList.remove('resizing');
                    document.removeEventListener('mousemove', mouseMoveHandler);
                    document.removeEventListener('mouseup', mouseUpHandler);
                };

                resizer.addEventListener('mousedown', mouseDownHandler);
            };
            createResizableTable(this.DataTable);
             */
        }

        public override void RenderContent()
        {
            if (!LoadRerender)
            {
                Rerender();
            }
            AddSections();
            if (!_hasFirstLoad && VirtualScroll)
            {
                _hasFirstLoad = true;
                return;
            }
            var viewPort = GetViewPortItem();
            FormattedRowData = Meta.LocalRender ? Meta.LocalData : RowData.Data;
            if (FormattedRowData.Nothing())
            {
                MainSection.DisposeChildren();
                DomLoaded();
                return;
            }
            DisposeNoRecord();
            if (VirtualScroll && FormattedRowData.Count > viewPort)
            {
                FormattedRowData = FormattedRowData.Take(viewPort).ToList();
            }
            if (MainSection.Children.HasElement())
            {
                UpdateExistRowsWrapper(dirty: false, 0, viewPort);
                return;
            }
            MainSection.Show = false;
            FormattedRowData.ToList().ForEach((rowData) =>
            {
                Html.Take(MainSection.Element);
                RenderRowData(Header, rowData, MainSection);
            });
            MainSection.Show = true;
            ContentRendered();
            DomLoaded();
        }

        protected void SetFocusingCom()
        {
            if (AutoFocus)
            {
                return;
            }
            if (EntityFocusId != null && LastComponentFocus != null)
            {
                var element = MainSection.Children.Flattern(x => x.Children)
                    .FirstOrDefault(x => x.Entity[IdField].ToString() == EntityFocusId && x.Meta.Id == LastComponentFocus.Id);
                if (element != null)
                {
                    var lastListView = AllListViewItem.FirstOrDefault(x => x.Entity[IdField].ToString() == EntityFocusId);
                    lastListView.Focused = true;
                    element.ParentElement.AddClass("cell-selected");
                    LastListViewItem = lastListView;
                    LastComponentFocus = element.Meta;
                    LastElementFocus = element.Element;
                }
                else
                {
                    HeaderSection.Element.Focus();
                }
            }
            else
            {
                HeaderSection.Element.Focus();
            }
        }

        private bool _hasFirstLoad = false;
        protected void UpdateExistRowsWrapper(bool? dirty, int skip, int viewPort)
        {
            if (!_hasFirstLoad)
            {
                _hasFirstLoad = true;
                return;
            }
            UpdateExistRows(dirty);
            RenderIndex();
            DomLoaded();
        }

        protected void UpdateExistRows(bool? dirty)
        {
            var updatedData = FormattedRowData.ToArray();
            var dataSections = AllListViewItem.Take(updatedData.Length).ToArray();
            dataSections.SelectForEach((child, index) =>
            {
                child.Entity = updatedData[index];
                child.Children.Flattern(x => x.Children).SelectForEach(x =>
                {
                    x.Entity = updatedData[index];
                });
                child.UpdateView();
            });
            var shouldAddRow = AllListViewItem.Count() <= updatedData.Length;
            if (shouldAddRow)
            {
                updatedData.Skip(dataSections.Length).ForEach(newRow =>
                {
                    var rs = RenderRowData(Header, newRow, MainSection);
                    StickyColumn(rs);
                });
            }
            else
            {
                MainSection.Children.Skip(updatedData.Length).ToArray().ForEach(x => x.Dispose());
            }
            if (dirty.HasValue)
            {
                Dirty = dirty.Value;
            }
            RenderIndex();
        }

        public override ListViewItem RenderRowData(List<Component> headers, object row, Section section, int? index = null, bool emptyRow = false)
        {
            var tbody = section.Element;
            Html.Take(tbody);
            var rowSection = new GridViewItem(ElementType.tr)
            {
                EmptyRow = emptyRow,
                Entity = row,
                ParentElement = tbody,
                PreQueryFn = _preQueryFn,
                ListView = this,
                Meta = Meta
            };
            section.AddChild(rowSection, index);
            var tr = Html.Context as HTMLTableRowElement;
            tr.TabIndex = -1;
            if (index.HasValue)
            {
                if (index >= tr.ParentElement.Children.Count() || index < 0)
                {
                    index = 0;
                }

                tr.ParentElement.InsertBefore(tr, tr.ParentElement.Children[index.Value]);
            }
            rowSection.RenderRowData(headers, row, index, emptyRow);
            if (emptyRow)
            {
                Children.ForEach(x => x.AlwaysLogHistory = true);
            }
            if (Disabled)
            {
                rowSection.SetDisabled(false, "btnEdit");
            }
            if (Meta.ComponentType != nameof(FileUploadGrid))
            {
                if (row[Utils.IdField] != null)
                {
                    rowSection.Element.RemoveClass("new-row");
                }
                else
                {
                    rowSection.Element.AddClass("new-row");
                }
            }
            return rowSection;
        }

        private void AddSummaries()
        {
            if (Header.All(x => x.Summary.IsNullOrEmpty()))
            {
                return;
            }

            var sums = Header.Where(x => !x.Summary.IsNullOrWhiteSpace());
            MainSection.Element.As<HTMLTableElement>().Children.Where(x => x.HasClass(SummaryClass)).ToArray().ForEach(x => x.Remove());
            var count = sums.DistinctBy(x => x.Summary).Count();
            sums.ForEach(header =>
            {
                AddNewEmptyRow();
                RenderSummaryRow(header, Header, FooterSection.Element as HTMLTableSectionElement, count);
            });
        }

        public override void DuplicateSelected(Event ev, bool addRow = false)
        {
            var originalRows = GetSelectedRows();
            var copiedRows = ReflectionExt.CloneRows(originalRows).ToList();
            if (copiedRows.Nothing() || !CanWrite)
            {
                return;
            }

            Toast.Success("Đang Sao chép liệu !");
            this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforePasted, originalRows, copiedRows).Done(() =>
            {
                var index = GetStartIndex(ev, addRow);
                base.AddRowsNo(copiedRows, index).Done(list =>
                {
                    RowsAdded(list, originalRows, copiedRows);
                });
            });
        }

        private int GetStartIndex(Event ev, bool addRow)
        {
            var index = AllListViewItem.IndexOf(x => x.Selected);
            if (addRow)
            {
                if (ev.KeyCodeEnum() == KeyCodeEnum.U && ev.CtrlOrMetaKey())
                {
                    if (Meta.TopEmpty)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = AllListViewItem.LastOrDefault().RowNo;
                    }
                }
            }

            return index;
        }

        private void RowsAdded(ListViewItem[] list, List<object> originalRows, List<object> copiedRows)
        {
            var lastChild = list.FirstOrDefault().FilterChildren<EditableComponent>(x => x.Meta.Editable).FirstOrDefault();
            lastChild?.Focus();
            RenderIndex();
            if (Meta.IsSumary)
            {
                AddSummaries();
            }
            ClearSelected();
            foreach (var item in list)
            {
                item.Selected = true;
            }
            LastListViewItem = list.FirstOrDefault();
            if (Meta.IsRealtime)
            {
                foreach (var item in list)
                {
                    item.PatchUpdateOrCreate();
                }
                Toast.Success("Sao chép dữ liệu thành công !");
                base.Dirty = false;
            }
            else
            {
                Toast.Success("Sao chép dữ liệu thành công !");
            }
            this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterPasted, originalRows, copiedRows).Done();
        }

        private void RenderSummaryRow(Component sum, List<Component> headers, HTMLTableSectionElement footer, int count)
        {
            var tr = CreateSummaryTableRow(sum, footer, count);
            if (tr is null)
            {
                return;
            }

            var hasSummaryClass = tr.HasClass(SummaryClass);
            var colSpan = sum.SummaryColSpan ?? 0;
            tr.AddClass(SummaryClass);
            if (!hasSummaryClass && headers.Contains(sum))
            {
                ResetSummaryRow(tr, colSpan);
            }
            if (!headers.Contains(sum))
            {
                ClearSummaryContent(tr);
                return;
            }
            SetSummaryHeaderText(sum, tr);
            CalcSumCol(sum, headers, tr, colSpan);
        }

        protected override void SetRowData(List<object> listData)
        {
            if (RowData._data is null || RowData._data.GetType() == typeof(string))
            {
                RowData._data = listData;
            }
            else
            {
                RowData._data.Clear();
                var hasElement = listData.HasElement();
                if (hasElement)
                {
                    listData.ForEach(RowData._data.Add); // Not to use AddRange because the _data is not always List
                }
            }
            RenderContent();
            if (Entity != null && ShouldSetEntity)
            {
                Entity.SetComplexPropValue(FieldName, RowData.Data);
            }
        }

        private static void SetSummaryHeaderText(Component sum, HTMLTableRowElement tr)
        {
            if (sum.Summary.IsNullOrWhiteSpace())
            {
                return;
            }

            var cell = tr.Cells[0];
            cell.ColSpan = (uint)sum.SummaryColSpan;
            cell.TextContent = sum.Summary;
            cell.AddClass("summary-header");
        }

        private HTMLTableRowElement CreateSummaryTableRow(Component sum, HTMLTableSectionElement footer, int count)
        {
            var summaryText = sum.Summary;
            if (footer is null)
            {
                return null;
            }

            var summaryRowCount = footer.Rows.Count(x => x.HasClass(SummaryClass));
            var existSumRow = footer.Rows.Reverse()
                .FirstOrDefault(x => x.HasClass(SummaryClass) && x.Children.Any(y => y.TextContent == summaryText));
            if (existSumRow is null)
            {
                existSumRow = footer.Rows.LastOrDefault();
            }

            if (summaryRowCount >= count)
            {
                return existSumRow;
            }
            if (MainSection.FirstChild is null)
            {
                return null;
            }
            var result = MainSection.FirstChild.Element.CloneNode(true) as HTMLTableRowElement;
            footer.AppendChild(result);
            result.Children.SelectForEach(x => x.InnerHTML = null);
            return result;
        }

        private void CalcSumCol(Component header, List<Component> headers, HTMLTableRowElement tr, int colSpan)
        {
            var index = headers.IndexOf(header);
            var cellVal = tr.Cells[index - colSpan + 1];
            var format = header.FormatData.IsNullOrWhiteSpace() ? "{0:n0}" : header.FormatData;
            var isNumber = RowData.Data.Any(x => x.GetType().GetProperty(header.FieldName).PropertyType.IsNumber());
            var sum = RowData.Data.Sum(x =>
            {
                var val = x[header.FieldName];
                if (val is null)
                {
                    return 0;
                }

                return Convert.ToDecimal(val);
            });
            cellVal.TextContent = Utils.FormatEntity(format, isNumber ? sum : RowData.Data.Count());
        }

        private static void ResetSummaryRow(HTMLTableRowElement tr, int colSpan)
        {
            for (var i = 1; i < colSpan; i++)
            {
                tr.Cells[0]?.Remove();
            }
            ClearSummaryContent(tr);
        }

        private static void ClearSummaryContent(HTMLTableRowElement tr)
        {
            foreach (var c in tr.Cells)
            {
                c.InnerHTML = string.Empty;
            }
        }

        internal override async Task RowChangeHandler(object rowData, ListViewItem rowSection, ObservableArgs observableArgs, EditableComponent component = null)
        {
            await Task.Delay(50);
            var com = new List<string>() { nameof(SearchEntry) };
            if (rowSection.EmptyRow && observableArgs.EvType == EventType.Change)
            {
                await this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeCreated, rowData, this);
                object rs;
                if (Meta.IsRealtime)
                {
                    var entity = rowData;
                    rowSection.PatchUpdateOrCreate();
                    Dirty = false;
                }
                else
                {
                    rs = rowSection.Entity;
                    Dirty = true;
                }
                if (Meta.ComponentType != nameof(VirtualGrid))
                {
                    Entity.SetComplexPropValue(FieldName, RowData.Data);
                }
                if (rowSection.EmptyRow)
                {
                    MoveEmptyRow(rowSection);
                    EmptySection.Children.Clear();
                    AddNewEmptyRow();
                }
                if (!com.Contains(component?.Meta.ComponentType))
                {
                    ClearSelected();
                    rowSection.Selected = true;
                    rowSection.Focus();
                    LastListViewItem = rowSection;
                    LastElementFocus.Focus();
                }
                await this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCreated, rowData);
            }
            if (component != null && component.ComponentType == nameof(GridView))
            {
                await this.DispatchEvent(component.Meta.Events, observableArgs.EvType, rowData, rowSection);
            }
            await this.DispatchEvent(Meta.Events, observableArgs.EvType, rowData, rowSection);
            if (observableArgs.EvType == EventType.Change)
            {
                PopulateFields();
                RenderIndex();
                if (Meta.IsSumary)
                {
                    AddSummaries();
                }
                LastListViewItem = rowSection;
                var headers = Header.Where(y => y.Editable).ToList();
                var currentComponent = headers.FirstOrDefault(y => y.FieldName == component?.FieldName);
                if (com.Contains(currentComponent.ComponentType) && rowData[currentComponent.FieldName] != null)
                {
                    var index = headers.IndexOf(currentComponent);
                    if (headers.Count > index + 1)
                    {
                        var nextGrid = headers[index + 1];
                        var nextComponent = rowSection.Children.Where(y => y?.FieldName == nextGrid.FieldName).FirstOrDefault();
                        ClearSelected();
                        rowSection.Selected = true;
                        rowSection.Focus();
                        LastListViewItem = rowSection;
                        nextComponent.Focus();
                    }
                }
            }
        }

        private void MoveEmptyRow(ListViewItem rowSection)
        {
            if (RowData.Data.Contains(rowSection.Entity))
            {
                return;
            }
            if (Meta.TopEmpty)
            {
                RowData.Data.Insert(0, rowSection.Entity);
                if (!MainSection.Children.Contains(EmptySection.FirstChild))
                {
                    MainSection.Children.Insert(0, EmptySection.FirstChild);
                }
                MainSection.Element.Prepend(EmptySection.Element.FirstElementChild);
            }
            else
            {
                RowData.Data.Add(rowSection.Entity);
                MainSection.Element.AppendChild(EmptySection.Element.FirstElementChild);
                if (!MainSection.Children.Contains(EmptySection.FirstChild))
                {
                    MainSection.Children.Add(EmptySection.FirstChild);
                }
            }
            if (Meta.IsRealtime)
            {
                rowSection.Element.RemoveClass("new-row");
            }
            rowSection.Parent = MainSection;
            rowSection.ListViewSection = MainSection;
        }

        protected void ProcessMetaData(object[][] ds, int rowCount)
        {
            var total = ds.Length > 1 && ds[1].Length > 0 ? (int?)ds[1][0]["total"] : null;
            var headers = ds.Length > 2 ? ds[2].Select(x => x.CastProp<Component>()).ToList() : null;
            Settings = ds.Length > 3 && ds[3].Length > 0 ? ds[3][0].As<UserSetting>() : null;
            FilterColumns(MergeComponent(headers, Settings));
            RenderTableHeader(Header);
            if (Paginator != null)
            {
                Paginator.Options.Total = total ?? rowCount;
            }
        }

        protected virtual void RenderTableHeader(List<Component> headers)
        {
            if (headers.Nothing())
            {
                headers = Header;
            }
            if (HeaderSection.Element is null)
            {
                AddSections();
            }
            headers.SelectForEach((x, index) => x.PostOrder = index);
            HeaderSection.DisposeChildren();
            bool anyGroup = headers.Any(x => !string.IsNullOrEmpty(x.GroupName));
            Html.Take(HeaderSection.Element).Clear().TRow.ForEach(headers, (header, index) =>
            {
                if (anyGroup && !string.IsNullOrEmpty(header.GroupName))
                {
                    if (header != headers.FirstOrDefault(x => x.GroupName == header.GroupName))
                    {
                        return;
                    }

                    Html.Instance.Th.Render();
                    Html.Instance.ColSpan(headers.Count(x => x.GroupName == header.GroupName));
                    Html.Instance.IHtml(header.GroupName).Render();
                    return;
                }
                Html.Instance.Th
                    .TabIndex(-1)
                    .DataAttr("field", header.FieldName)
                    .DataAttr("id", header.Id).Width(header.AutoFit ? "auto" : header.Width)
                    .Style($"{header.Style};min-width: {header.MinWidth}; max-width: {header.MaxWidth}")
                    .TextAlign(TextAlign.center)
                    .Event(EventType.DblClick, EditForm.ComponentProperties, header)
                    .Event(EventType.ContextMenu, HeaderContextMenu, header)
                    .Event(EventType.FocusOut, (e) => FocusOutHeader(e, header))
                    .Event(EventType.KeyDown, (e) => ThHotKeyHandler(e, header));
                HeaderSection.AddChild(new Section(Html.Context) { Meta = header });
                if (anyGroup && string.IsNullOrEmpty(header.GroupName))
                {
                    Html.Instance.RowSpan(2);
                }
                if (!anyGroup && Header.Any(x => x.GroupName.HasAnyChar()))
                {
                    Html.Instance.ClassName("header-group");
                }
                if (header.StatusBar)
                {
                    Html.Instance.Icon("fa fa-edit").Event(EventType.Click, ToggleAll).End.Render();
                }
                var orderBy = AdvSearchVM.OrderBy?.FirstOrDefault(x => x.ComId == header.Id);
                if (orderBy != null)
                {
                    Html.Instance.ClassName(orderBy.OrderbyDirectionId == OrderbyDirection.ASC ? "asc" : "desc").Render();
                }
                if (!header.Icon.IsNullOrWhiteSpace())
                {
                    Html.Instance.Icon(header.Icon).Margin(Direction.right, 0).End.Render();
                }
                else if (!header.StatusBar)
                {
                    Html.Instance.Event(EventType.Click, (e) => ClickHeader(e, header)).IHtml(header.ShortDesc).Render();
                }
                if (header.ComponentType == nameof(Number))
                {
                    Html.Instance.Div.End.Render();
                    Html.Instance.Span.Style("display: block;").End.Render();
                }
                if (header.Description != null)
                {
                    Html.Instance.Attr("title", header.Description);
                }
                if (Client.SystemRole)
                {
                    Html.Instance.Attr("contenteditable", "true");
                    Html.Instance.Event(EventType.Input, (e) => ChangeHeader(e, header));
                }
                Html.Instance.EndOf(ElementType.th);
            }).EndOf(ElementType.tr).Render();

            if (anyGroup)
            {
                Html.Instance.TRow.ForEach(headers, (header, index) =>
                {
                    if (anyGroup && !string.IsNullOrEmpty(header.GroupName))
                    {
                        Html.Instance.Th
                            .DataAttr("field", header.FieldName).Width(header.Width)
                            .Style($"min-width: {header.MinWidth}; max-width: {header.MaxWidth}")
                            .TextAlign(header.TextAlignEnum)
                            .Event(EventType.ContextMenu, HeaderContextMenu, header)
                            .InnerHTML(header.ShortDesc);
                        HeaderSection.AddChild(new Section(Html.Context.ParentElement) { Meta = header });
                    }
                });
            }
            HeaderSection.Children = HeaderSection.Children.OrderBy(x => x.Meta.PostOrder).ToList();
            if (!Meta.Focus)
            {
                ColumnResizeHandler();
            }
        }
        private int _imeout;

        private void ChangeHeader(Event e, Component header)
        {
            Window.ClearTimeout(_imeout);
            _imeout = Window.SetTimeout(() =>
            {
                var html = e.Target as HTMLElement;
                var patchVM = new PatchVM
                {
                    Table = nameof(Component),
                    Changes = new List<PatchDetail>
                    {
                        new PatchDetail { Field = nameof(Component.Id), Value = header.Id, OldVal = header.Id },
                        new PatchDetail { Field = nameof(Component.ShortDesc), Value = html.TextContent.Trim(), OldVal = header.ShortDesc },
                    }
                };
                Client.Instance.PatchAsync(patchVM);
            }, 1000);
        }

        protected override Task<List<object>> CustomQuery(SqlViewModel vm)
        {
            var tcs = new TaskCompletionSource<List<object>>();
            var dsTask = Client.Instance.ComQuery(vm);
            dsTask.Done(ds =>
            {
                if (ds.Nothing())
                {
                    SetRowData(null);
                    tcs.TrySetResult(null);
                    return;
                }
                var total = ds.Length > 1 ? ds[1].ToDynamic()[0].total : ds[0].Length;
                if (ds.Length >= 3)
                {
                    ProcessMetaData(ds, total);
                }
                var rows = new List<object>(ds[0]);
                ClearRowData();
                SetRowData(rows);
                UpdatePagination(total, rows.Count);
                tcs.TrySetResult(rows);
            }).Catch(err => tcs.TrySetException(err));
            return tcs.Task;
        }

        private void MoveLeft(Component header, Event e)
        {
            var current = e.Target as HTMLElement;
            var th = current.ParentElement;
            var tr = th.ParentElement.QuerySelectorAll("th");
            var index = tr.FindItemAndIndex(x => x == th).Item2;
            /*@
            th.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                const cells = [].slice.call(row.querySelectorAll('th, td'));
                if(!cells[0].classList.contains('summary-header')){
                    var draggingColumnIndex = index;
                    var endColumnIndex = index - 1;
                    draggingColumnIndex > endColumnIndex
                        ? cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                              cells[draggingColumnIndex],
                              cells[endColumnIndex]
                          )
                        : cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                              cells[draggingColumnIndex],
                              cells[endColumnIndex].nextSibling
                          );
                }
            });
            */
            SwapList(index - 1, index - 2);
            SwapHeader(index, index - 1);
            ResetOrder();
            UpdateHeader();
        }

        private void MoveRight(Component header, Event e)
        {
            var current = e.Target as HTMLElement;
            var th = current.ParentElement;
            var tr = th.ParentElement.QuerySelectorAll("th");
            var index = tr.FindItemAndIndex(x => x == th).Item2;
            /*@
            th.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                const cells = [].slice.call(row.querySelectorAll('th, td'));
                if(!cells[0].classList.contains('summary-header')){
                    var draggingColumnIndex = index;
                    var endColumnIndex = index + 1;
                    draggingColumnIndex > endColumnIndex
                        ? cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                              cells[draggingColumnIndex],
                              cells[endColumnIndex]
                          )
                        : cells[endColumnIndex].parentNode && cells[endColumnIndex].parentNode.insertBefore(
                              cells[draggingColumnIndex],
                              cells[endColumnIndex].nextSibling
                          );
                }
            });
            */
            SwapList(index - 1, index);
            SwapHeader(index, index + 1);
            ResetOrder();
            UpdateHeader();
        }

        public virtual void ToggleAll()
        {
            var anySelected = AllListViewItem.Any(x => x.Selected);
            if (anySelected)
            {
                ClearSelected();
                return;
            }
            RowAction(x =>
            {
                if (x.EmptyRow) return;
                x.Selected = true;
            });
        }

        private void HeaderContextMenu(Event e, Component header)
        {
            e.PreventDefault();
            e.StopPropagation();
            var editForm = this.FindClosest<EditForm>();
            var section = this.FindClosest<Section>();
            var menu = ContextMenu.Instance;
            menu.Top = e.Top();
            menu.Left = e.Left();

            menu.MenuItems = new List<ContextMenuItem>
            {
                new ContextMenuItem { Icon = "fal fa-eye", Text = "Hiện tiêu đề", Click = ShowWidth, Parameter = new {header, events= e }},
                new ContextMenuItem { Icon = "fal fa-eye-slash", Text = "Ẩn tiêu đề", Click = HideWidth, Parameter = new {header, events= e }},
                new ContextMenuItem { Icon = header.Frozen ? "fal fa-snowflakes" : "fal fa-snowflake", Text = header.Frozen ? "Hủy định cột" : "Cố định cột", Click = FrozenColumn, Parameter = new {header= header, events= e }},
            };
            if (Client.SystemRole)
            {
                menu.MenuItems.AddRange(new List<ContextMenuItem>
                {
                    new ContextMenuItem { Icon = "fal fa-wrench", Text = "Tùy chọn cột dữ liệu", Click = editForm.ComponentProperties, Parameter = header },
                    new ContextMenuItem { Icon = "fal fa-clone", Text = "Clone cột", Click = CloneHeader, Parameter = header },
                    new ContextMenuItem { Icon = "fal fa-trash-alt", Text = "Xóa cột", Click = RemoveHeader, Parameter = header },
                    new ContextMenuItem { Icon = "fal fa-cog", Text = "Tùy chọn bảng dữ liệu", Click = editForm.ComponentProperties, Parameter = Meta },
                    new ContextMenuItem { Icon = "fal fa-cogs", Text = "Tùy chọn vùng dữ liệu", Click = editForm.SectionProperties, Parameter = section.ComponentGroup },
                    new ContextMenuItem { Icon = "fal fa-folder-open", Text = "Thiết lập chung", Click = editForm.FeatureProperties, Parameter = editForm.Feature },
                });
            }
            menu.Render();
        }

        private int awaiter1;
        public void UpdateHeader()
        {
            var isSave = Window.LocalStorage.GetItem("isSave");
            if (isSave is null)
            {
                Window.ClearTimeout(awaiter1);
                awaiter1 = Window.SetTimeout(() =>
                {
                    UpdateSetting(Settings, "ListView", GetHeaderSettings()).Done();
                }, 100);
            }
        }

        private void HideWidth(object arg)
        {
            var entity = arg["header"] as Component;
            var e = arg["events"] as Event;
            /*@
             e.target.firstChild.remove();
             e.target.style.minWidth = "";
             e.target.style.maxWidth = "";
             e.target.style.width = "";
             */
            UpdateSetting(Settings, "ListView", GetHeaderSettings()).Done();
        }

        private string GetHeaderSettings()
        {
            var headerElement = HeaderSection.Children
                .Where(x => x.Meta?.Id != null)
                .ToDictionary(x => x.Meta.Id);
            var columns = Header.Where(x => x.Id != null).Select(x =>
            {
                var match = headerElement.GetValueOrDefault(x.Id);
                if (match is null) return null;
                x.Width = match.Element.OffsetWidth + "px";
                x.MaxWidth = match.Element.OffsetWidth + "px";
                x.MinWidth = match.Element.OffsetWidth + "px";
                return x;
            }).Where(x => x != null).ToArray();
            var value = JsonConvert.SerializeObject(columns);
            return value;
        }

        private void ShowWidth(object arg)
        {
            var entity = arg["header"] as Component;
            var e = arg["events"] as Event;
            /*@
             if (!e.target.firstChild.length) {
                e.target.prepend(entity.ShortDesc)
             }
             e.target.style.minWidth = "";
             e.target.style.maxWidth = "";
             e.target.style.width = "";
             */
            UpdateSetting(Settings, "ListView", GetHeaderSettings()).Done();
        }

        private void FrozenColumn(object arg)
        {
            var entity = arg["header"] as Component;
            Header.FirstOrDefault(x => x.Id == entity.Id).Frozen = !entity.Frozen;
            UpdateSetting(Settings, "ListView", GetHeaderSettings()).Done(x => ReloadData(cacheHeader: false));
        }

        public void CloneHeader(object arg)
        {
            var entity = arg as Component;
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn clone cột này không?",
            };
            confirm.Render();
            confirm.YesConfirmed += () =>
            {
                var cloned = entity.Clone();
                cloned.Id = Uuid7.Id25();
                var patch = cloned.MapToPatch();
                Client.Instance.PatchAsync(patch).Done(success =>
                {
                    if (success == 0)
                    {
                        Toast.Warning("Clone error");
                        return;
                    }
                    Header.Add(cloned);
                    Header = Header.OrderByDescending(x => x.Frozen).ThenByDescending(header => header.ComponentType == "Button").ThenBy(x => x.Order).ToList();
                    Rerender();
                    Toast.Success("Clone success");
                }).Catch(e =>
                {
                    Toast.Warning("Clone header NOT success");
                });
            };
        }

        public void RemoveHeader(object arg)
        {
            var entity = arg as Component;
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn xóa cột này không?",
            };
            confirm.Render();
            confirm.YesConfirmed += () =>
            {
                var ids = new string[] { entity.Id };
                Client.Instance.HardDeleteAsync(ids, nameof(Component), ConnKey)
                .Done(success =>
                {
                    if (!success)
                    {
                        Toast.Warning("delete error");
                        return;
                    }
                    Toast.Success("Delete success");
                    Header.Remove(entity);
                    Rerender();
                });
            };
        }

        public override void RemoveRowById(string id)
        {
            base.RemoveRowById(id);
            RenderIndex();
        }

        public override void RemoveRow(object row)
        {
            base.RemoveRow(row);
            RenderIndex();
        }

        public override Task<List<object>> HardDeleteConfirmed(List<object> deleted)
        {
            var tcs = new TaskCompletionSource<List<object>>();
            base.HardDeleteConfirmed(deleted).Done((res) =>
            {
                RenderIndex();
                if (Meta.IsSumary)
                {
                    AddSummaries();
                }
                tcs.TrySetResult(res);
            });
            return tcs.Task;
        }

        protected int _renderIndexAwaiter;
        internal int _lastScrollTop;

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            if (!Editable && !Meta.CanCache)
            {
                if (force)
                {
                    DisposeNoRecord();
                    ListViewSearch.RefershListView();
                }
            }
            else
            {
                RowAction(row => !row.EmptyRow, row => row.UpdateView(force, dirty, componentNames));
            }
        }

        public async Task RowChangeHandlerGrid(object rowData, ListViewItem rowSection, ObservableArgs observableArgs, EditableComponent component = null)
        {
            await Task.Delay(CellCountNoSticky);
            if (rowSection.EmptyRow && observableArgs.EvType == EventType.Change)
            {
                await this.DispatchCustomEvent(Meta.Events, CustomEventType.BeforeCreated, rowData);
                rowSection.EmptyRow = false;
                MoveEmptyRow(rowSection);
                var headers = Header.Where(y => y.Editable).ToList();
                var currentComponent = headers.FirstOrDefault(y => y.FieldName == component.FieldName);
                var index = headers.IndexOf(currentComponent);
                if (headers.Count > index + 1)
                {
                    var nextGrid = headers[index + 1];
                    var nextComponent = rowSection.Children.Where(y => y.FieldName == nextGrid.FieldName).FirstOrDefault();
                    nextComponent.Focus();
                }
                EmptySection.Children.Clear();
                AddNewEmptyRow();
                Entity.SetComplexPropValue(FieldName, RowData.Data);
                await this.DispatchCustomEvent(Meta.Events, CustomEventType.AfterCreated, rowData);
            }
            AddSummaries();
            PopulateFields();
            RenderIndex();
            await this.DispatchEvent(Meta.Events, EventType.Change, rowData);
        }

        internal int GetViewPortItem()
        {
            if (Element is null || !Element.HasClass(Position.sticky.ToString()))
            {
                return RowData.Data.Count();
            }
            var mainSectionHeight = Element.ClientHeight
                - (HeaderSection.Element?.ClientHeight ?? 0)
                - Paginator.Element.ClientHeight
                - _theadTable;
            Header = Header.Where(x => x != null).ToList();
            if (!Header.All(x => x.Summary.IsNullOrEmpty()))
            {
                mainSectionHeight -= _tfooterTable;
            }
            if (Meta.CanAdd)
            {
                mainSectionHeight -= _rowHeight;
            }
            return GetRowCountByHeight(mainSectionHeight);
        }
    }
}