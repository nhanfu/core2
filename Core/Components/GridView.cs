using Bridge.Html5;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.MVVM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ElementType = Core.MVVM.ElementType;
using TextAlign = Core.Enums.TextAlign;

namespace Core.Components
{
    public class SortedField
    {
        public string Field { get; set; }
        public GridPolicy Com { get; set; }
        public bool Desc { get; set; }
    }
    public class GridView : ListView
    {
        public ListViewSection EmptyRowSection { get; set; }
        private const string SummaryClass = "summary";
        private const int CellCountNoSticky = 50;
        public List<HTMLElement> _summarys = new List<HTMLElement>();
        public bool _sum = false;
        public bool AutoFocus = false;
        public bool LoadRerender = false;
        public HTMLElement DataTable { get; set; }
        private UserSetting _settings { get; set; }
        public static GridPolicy ToolbarColumn = new GridPolicy
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
            if (GuiInfo.IsSumary)
            {
                AddSummaries();
            }
            PopulateFields();
            if (!_sum && (GuiInfo.ComponentType == nameof(GridView) || GuiInfo.ComponentType == nameof(VirtualGrid)))
            {
                Task.Run(async () =>
                {
                    await AddSubTotal();
                    _sum = true;
                });
            }
        }

        private async Task AddSubTotal()
        {
            if (BasicHeader.Nothing())
            {
                return;
            }
            var gridPolicy = BasicHeader.Where(x => x.FieldName != IdField && x.ComponentType == nameof(Number) && x.IsSumary == true).ToList();
            if (gridPolicy.Nothing())
            {
                return;
            }
            var sum = gridPolicy.Select(x => $"FORMAT(SUM(isnull([{GuiInfo.RefName}].{x.FieldName},0)),'#,#') as {x.FieldName}").ToList();
            var submitEntity = GuiInfo.PreQuery;
            if (_preQueryFn != null)
            {
                submitEntity = (string)_preQueryFn.Call(null, this);
            }
            var filter = Wheres.Where(x => !x.Group).Select(x => x.FieldName).Combine(" and ");
            var filter1 = Wheres.Where(x => x.Group).Select(x => x.FieldName).Combine(" or ");
            var wh = new List<string>();
            if (!filter.IsNullOrWhiteSpace())
            {
                wh.Add($"({filter})");
            }
            if (!filter1.IsNullOrWhiteSpace())
            {
                wh.Add($"({filter1})");
            }
            var stringWh = wh.Any() ? $"({wh.Combine(" and ")})" : "";
            var dataSet = await new Client(GuiInfo.RefName)
                .SubmitAsync<object[][]>(new XHRWrapper
                {
                    Value = sum.Combine(),
                    Url = $"SubTotal?group=" +
                    $"&tablename={GuiInfo.RefName}" +
                    $"&refname=" +
                    $"&formatsumary={GuiInfo.FormatSumaryField}" +
                    $"&dateTimeField={GuiInfo.DateTimeField}" +
                    $"&showNull={GuiInfo.ShowNull ?? false}" +
                    $"&sql={Sql}" +
                    $"&orderby={GuiInfo.OrderBySumary}" +
                    $"&where={stringWh} {(GuiInfo.PreQuery.IsNullOrWhiteSpace() ? "" : $"{(wh.Any() ? " and " : "")} {submitEntity}")}",
                    Method = HttpMethod.POST,
                    AllowNestedObject = true,
                    ErrorHandler = (x) => { }
                });
            var sumarys = dataSet[0][0];
            var headers = HeaderSection.Children.Where(x => x.GuiInfo.FieldName != IdField && x.GuiInfo.ComponentType == nameof(Number) && x.GuiInfo.IsSumary == true).ToList();
            headers.ForEach(x =>
            {
                x.Element.Children[1].InnerHTML = "(" + sumarys[x.GuiInfo.FieldName] + ")";
            });
        }

        private void PopulateFields()
        {
            if (!GuiInfo.PopulateField.IsNullOrWhiteSpace())
            {
                var fields = GuiInfo.PopulateField.Split(",");
                if (fields.Length > 0)
                {
                    EditForm.UpdateView(true, componentNames: fields);
                }
            }
        }

        protected override void Rerender()
        {
            LoadRerender = true;
            DisposeNoRecord();
            Editable = GuiInfo.CanAdd && Header.Any(x => !x.Hidden && x.Editable);
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
                return;
            }
            RenderIndex();
            if (MainSection.Element is HTMLTableSectionElement tableElement)
            {
                tableElement.AddEventListener(EventType.ContextMenu, BodyContextMenuHandler);
            }
        }

        private void StickyColumn(EditableComponent rows, string top = null)
        {
            var shouldStickEle = new string[] { "th", "td" };
            var frozen = rows.FilterChildren<EditableComponent>(predicate: x => x.GuiInfo != null && x.GuiInfo.Frozen, ignorePredicate: x => x is ListViewSearch).ToArray();
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
            var id = "collapse" + GuiInfo.Id;
            var idtb = "tb" + GuiInfo.Id;
            if (GuiInfo.IsCollapsible)
            {
                html.Div.ClassName("card mb-0")
                    .Div.ClassName("card-header")
                    .H5.ClassName("mb-0")
                    .A
                    .ClassName("btn btn-primary")
                    .DataAttr("toggle", "collapse").Href("#" + id)
                    .Attr("aria-expanded", "false")
                    .Attr("aria-controls", id).Text(GuiInfo.Label).EndOf(".card");
            }
            html.Div.ClassName("grid-wrapper " + (GuiInfo.IsCollapsible ? "collapse multi-collapse" : "")).Id(id).Event(EventType.KeyDown, (e) => HotKeyF6Handler(e, e.KeyCodeEnum()))
            .ClassName(Editable ? "editable" : string.Empty);
            Element = Html.Context;
            if (GuiInfo.CanSearch)
            {
                Html.Instance.Div.ClassName("grid-toolbar search").End.Render();
            }
            ListViewSearch = new ListViewSearch(GuiInfo)
            {
                Entity = new ListViewSearchVM()
            };
            if (GuiInfo.DefaultAddStart.HasValue)
            {
                var pre = Convert.ToDouble(GuiInfo.DefaultAddStart.Value);
                ListViewSearch.EntityVM.StartDate = DateTime.Now.AddDays(pre);
            }
            var lFrom = Window.LocalStorage.GetItem("FromDate" + GuiInfo.Id);
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
                if (GuiInfo.ComponentType == nameof(VirtualGrid) && GuiInfo.AddDate != null && (bool)GuiInfo.AddDate)
                {
                    ListViewSearch.EntityVM.StartDate = DateTime.Now.AddMonths(-2);
                }
            }
            if (GuiInfo.DefaultAddEnd.HasValue)
            {
                var pre = Convert.ToDouble(GuiInfo.DefaultAddEnd.Value);
                ListViewSearch.EntityVM.EndDate = DateTime.Now.AddDays(pre);
            }
            var lTo = Window.LocalStorage.GetItem("ToDate" + GuiInfo.Id);
            if (lTo != null)
            {
                ListViewSearch.EntityVM.EndDate = DateTime.Parse(lTo.ToString());
            }
            AddChild(ListViewSearch);
            DataTable = Html.Take(Element).Div.ClassName("table-wrapper").Table.ClassName("table").Id(idtb).GetContext();
            Html.Instance.Thead.End.TBody.ClassName("empty").End.TBody.End.TFooter.Render();

            FooterSection = new ListViewSection(Html.Context) { ParentElement = DataTable };
            AddChild(FooterSection);

            MainSection = new ListViewSection(FooterSection.Element.PreviousElementSibling) { ParentElement = DataTable };
            AddChild(MainSection);

            EmptyRowSection = new ListViewSection(MainSection.Element.PreviousElementSibling) { ParentElement = DataTable };
            AddChild(EmptyRowSection);

            HeaderSection = new ListViewSection(EmptyRowSection.Element.PreviousElementSibling) { ParentElement = DataTable };
            AddChild(HeaderSection);
            Html.Instance.EndOf(".table-wrapper");
            RenderPaginator();
        }

        public void SwapList(int oldIndex, int newIndex)
        {
            var item = BasicHeader[oldIndex];
            BasicHeader.RemoveAt(oldIndex);
            BasicHeader.Insert(newIndex, item);
        }

        public void SwapHeader(int oldIndex, int newIndex)
        {
            var item = Header[oldIndex];
            Header.RemoveAt(oldIndex);
            Header.Insert(newIndex, item);
        }

        protected void ClickHeader(Event e, GridPolicy header)
        {
            var th = e.Target as HTMLElement;
            var tr = th.ParentElement.QuerySelectorAll("th");
            var index = tr.FindItemAndIndex(x => x == th).Item2;
            if (index < 0)
            {
                return;
            }
            /*@
                th.parentElement.parentElement.parentElement.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                    if(row.hasAttribute('virtualrow')){
                        return;
                    }
                    const cells = [].slice.call(row.querySelectorAll('th, td'));
                    cells[index].style.backgroundColor= "#cbdcc2";
                    cells[index].style.color = "#000";
                });
                */
        }

        protected void FocusOutHeader(Event e, GridPolicy header)
        {
            var th = e.Target as HTMLElement;
            var tr = th.ParentElement.QuerySelectorAll("th");
            var index = tr.FindItemAndIndex(x => x == th).Item2;
            if (index < 0)
            {
                return;
            }
            /*@
                th.parentElement.parentElement.parentElement.querySelectorAll('tr:not(.summary)').forEach(function(row) {
                    if(row.hasAttribute('virtualrow')){
                       return;
                    }
                    const cells = [].slice.call(row.querySelectorAll('th, td'));
                    cells[index].style.removeProperty("background-color");
                    cells[index].style.removeProperty("color");
                });
                */
        }

        protected void ThHotKeyHandler(Event e, GridPolicy header)
        {
            if (GuiInfo.Focus)
            {
                return;
            }
            var keyCode = e.KeyCodeEnum();
            if (keyCode == KeyCodeEnum.RightArrow)
            {
                e.StopPropagation();
                var th = e.Target as HTMLElement;
                var tr = th.ParentElement.QuerySelectorAll("th");
                var index = tr.FindItemAndIndex(x => x == th).Item2;
                /*@
                th.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                        if(row.hasAttribute('virtualrow')){
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
                var th1 = e.Target as HTMLElement;
                var tr1 = th1.ParentElement.QuerySelectorAll("th");
                var index1 = tr1.FindItemAndIndex(x => x == th1).Item2;
                /*@
                th1.parentElement.parentElement.parentElement.querySelectorAll('tr').forEach(function(row) {
                        if(row.hasAttribute('virtualrow')){
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

        public virtual void FilterInSelected(object ev)
        {
            if (ev["Operator"] is null)
            {
                return;
            }
            var header = Header.FirstOrDefault(x => x.FieldName == ev["FieldName"].ToString());
            var subFilter = string.Empty;
            var lastFilter = Window.LocalStorage.GetItem("LastSearch" + GuiInfo.Id + header.Id);
            if (lastFilter != null)
            {
                subFilter = lastFilter.ToString();
            }
            var confirmDialog = new ConfirmDialog
            {
                Content = $"Nhập {header.ShortDesc} cần tìm" + ev["Text"],
                NeedAnswer = true,
                MultipleLine = false,
                ComType = header.ComponentType == nameof(Datepicker) || header.ComponentType == nameof(Number) ? header.ComponentType : nameof(Textbox),
                Precision = header.Precision,
            };
            confirmDialog.YesConfirmed += async () =>
            {
                string value = null;
                string valueText = null;
                if (header.ComponentType == nameof(Datepicker))
                {
                    valueText = confirmDialog.Datepicker.OriginalText;
                    value = confirmDialog.Datepicker.Value.ToString();
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
                Window.LocalStorage.SetItem("LastSearch" + GuiInfo.Id + header.Id, value);
                if (CellSelected.Any(x => x.FieldName == ev["FieldName"].ToString() && x.Operator == "in") && !(bool)ev["Shift"])
                {
                    CellSelected.FirstOrDefault(x => x.FieldName == ev["FieldName"].ToString() && x.Operator == "in").Value = value;
                    CellSelected.FirstOrDefault(x => x.FieldName == ev["FieldName"].ToString() && x.Operator == "in").ValueText = valueText;
                }
                else
                {
                    CellSelected.Add(new CellSelected
                    {
                        FieldName = ev["FieldName"].ToString(),
                        FieldText = header.ShortDesc,
                        ComponentType = header.ComponentType,
                        Shift = (bool)ev["Shift"],
                        Value = value,
                        ValueText = valueText,
                        Operator = ev["Operator"].ToString(),
                        OperatorText = ev["OperatorText"].ToString(),
                    });
                }
                _summarys.Add(new HTMLElement());
                await ActionFilter();
                confirmDialog.Textbox.Text = null;
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

        public override async Task ActionFilter()
        {
            if (CellSelected.Nothing())
            {
                var dataSourceFilter = GuiInfo.DataSourceFilter;
                MainSection.DisposeChildren();
                await ApplyFilter(true);
                return;
            }
            Spinner.AppendTo(DataTable);
            var dropdowns = CellSelected.Where(x => (!x.Value.IsNullOrWhiteSpace() || !x.ValueText.IsNullOrWhiteSpace()) && (x.ComponentType == "Dropdown" || x.ComponentType == nameof(SearchEntry))).ToList();
            var data = dropdowns.Select(x =>
            {
                var header = Header.FirstOrDefault(y => y.FieldName == x.FieldName);
                if (!x.IsSearch)
                {
                    var format = header.FormatCell.Split("}")[0].Replace("{", "");
                    return new Client(header.RefName).GetRawList<dynamic>($"?$select=Id&$filter=contains({format},'" + x.ValueText.EncodeSpecialChar() + "')", entityName: header.RefName);
                }
                else
                {
                    return new Client(header.RefName).GetRawList<dynamic>($"?$select=Id&$filter=Id eq " + x.Value.EncodeSpecialChar(), entityName: header.RefName);
                }
            }).ToList();
            await Task.WhenAll(data);
            var index = 0;
            var lisToast = new List<string>();
            CellSelected.ForEach(cell =>
            {
                var where = string.Empty;
                var hl = Header.FirstOrDefault(y => y.FieldName == cell.FieldName);
                string ids = null;
                var isNUll = cell.Value.IsNullOrWhiteSpace() && cell.ValueText.IsNullOrWhiteSpace();
                AdvSearchOperation advo = cell.Operator == "not in" ? AdvSearchOperation.NotIn : AdvSearchOperation.In;
                if ((hl.ComponentType == "Dropdown" || hl.ComponentType == nameof(SearchEntry)) && !hl.FormatCell.IsNullOrWhiteSpace())
                {
                    if (isNUll)
                    {
                        advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} is not null" : $"[{GuiInfo.RefName}].{cell.FieldName} is null";
                    }
                    else
                    {
                        var rsdynamic = data[index].Result;
                        if (rsdynamic.Any())
                        {
                            ids = rsdynamic.Select(x => x.Id).Cast<int>().Combine();
                            where = cell.Operator == "not in" ? $"([{GuiInfo.RefName}].{cell.FieldName} not in ({ids}) {(!cell.Value.IsNullOrWhiteSpace() ? $" or [{GuiInfo.RefName}].{cell.FieldName} is null" : "")})" : $"[{GuiInfo.RefName}].{cell.FieldName} in ({ids})";
                        }
                        else
                        {
                            where = cell.Operator == "not in" ? $"([{GuiInfo.RefName}].{cell.FieldName} != {cell.Value} {(!cell.Value.IsNullOrWhiteSpace() ? $" or [{GuiInfo.RefName}].{cell.FieldName} is null" : "")})" : $"[{GuiInfo.RefName}].{cell.FieldName} = {cell.Value}";
                        }
                        index++;
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == "Input" || hl.ComponentType == nameof(Textbox) || hl.ComponentType == "Textarea")
                {
                    if (isNUll)
                    {
                        advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        where = cell.Operator == "not in" ? $"([{GuiInfo.RefName}].{cell.FieldName} is not null and [{GuiInfo.RefName}].{cell.FieldName} != '')" : $"([{GuiInfo.RefName}].{cell.FieldName} is null or [{GuiInfo.RefName}].{cell.FieldName} = '')";
                    }
                    else
                    {
                        advo = cell.Operator == "not in" ? AdvSearchOperation.NotLike : AdvSearchOperation.Like;
                        where = cell.Operator == "not in" ? $"(CHARINDEX(N'{cell.Value}', [{GuiInfo.RefName}].{cell.FieldName}) = 0 or [{GuiInfo.RefName}].{cell.FieldName} is null)" : $"CHARINDEX(N'{cell.Value}', [{GuiInfo.RefName}].{cell.FieldName}) > 0";
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == nameof(Number) || (hl.ComponentType == "Label" && hl.FieldName.Contains("Id")))
                {
                    if (cell.Operator == "not in" || cell.Operator == "in")
                    {
                        if (isNUll)
                        {
                            advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                            where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} is not null" : $"[{GuiInfo.RefName}].{cell.FieldName} is null";
                        }
                        else
                        {
                            advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqual : AdvSearchOperation.Equal;
                            where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} != {cell.Value.Replace(",", "")}" : $"[{GuiInfo.RefName}].{cell.FieldName} = {cell.Value.Replace(",", "")}";
                        }
                    }
                    else
                    {
                        if (cell.Operator == "gt" || cell.Operator == "lt")
                        {
                            where = cell.Operator == "gt" ? $"[{GuiInfo.RefName}].{cell.FieldName} > {cell.Value}" : $"[{GuiInfo.RefName}].{cell.FieldName} < {cell.Value}";
                            advo = cell.Operator == "gt" ? AdvSearchOperation.GreaterThan : AdvSearchOperation.LessThan;
                        }
                        else if (cell.Operator == "ge" || cell.Operator == "le")
                        {
                            where = cell.Operator == "ge" ? $"[{GuiInfo.RefName}].{cell.FieldName} >= {cell.Value}" : $"[{GuiInfo.RefName}].{cell.FieldName} <= {cell.Value}";
                            advo = cell.Operator == "ge" ? AdvSearchOperation.GreaterThanOrEqual : AdvSearchOperation.LessThanOrEqual;
                        }
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == nameof(Checkbox))
                {
                    if (isNUll)
                    {
                        advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} is not null" : $"[{GuiInfo.RefName}].{cell.FieldName} is null";
                    }
                    else
                    {
                        where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} != {(cell.Value == "true" ? "1" : "0")}" : $"[{GuiInfo.RefName}].{cell.FieldName} = {(cell.Value == "true" ? "1" : "0")}";
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                else if (hl.ComponentType == nameof(Datepicker))
                {
                    cell.Value = cell.Value.DecodeSpecialChar();
                    cell.ValueText = cell.Value.DecodeSpecialChar();
                    if (cell.Operator == "not in" || cell.Operator == "in")
                    {
                        if (isNUll)
                        {
                            where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} is not null" : $"[{GuiInfo.RefName}].{cell.FieldName} is null";
                            advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualNull : AdvSearchOperation.EqualNull;
                        }
                        else
                        {
                            try
                            {
                                var va = DateTime.ParseExact(cell.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                if (cell.Operator == "not in" || cell.Operator == "in")
                                {
                                    where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} != '{va:yyyy-MM-dd}'" : $"[{GuiInfo.RefName}].{cell.FieldName} = '{va:yyyy-MM-dd}'";
                                    advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualDatime : AdvSearchOperation.EqualDatime;
                                }
                            }
                            catch
                            {
                                var va = DateTime.ParseExact(cell.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                where = cell.Operator == "not in" ? $"[{GuiInfo.RefName}].{cell.FieldName} != '{va:yyyy-MM-dd}'" : $"[{GuiInfo.RefName}].{cell.FieldName} = '{va:yyyy-MM-dd}'";
                                advo = cell.Operator == "not in" ? AdvSearchOperation.NotEqualDatime : AdvSearchOperation.EqualDatime;
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
                            if (cell.Operator == "gt" || cell.Operator == "lt")
                            {
                                where = cell.Operator == "gt" ? $"[{GuiInfo.RefName}].{cell.FieldName} > '{va:yyyy-MM-dd HH:mm}'" : $"[{GuiInfo.RefName}].{cell.FieldName} < '{va:yyyy-MM-dd HH:mm}'";
                                advo = cell.Operator == "gt" ? AdvSearchOperation.GreaterThanDatime : AdvSearchOperation.LessThanDatime;
                            }
                            else if (cell.Operator == "ge" || cell.Operator == "le")
                            {
                                where = cell.Operator == "ge" ? $"[{GuiInfo.RefName}].{cell.FieldName} >= '{va:yyyy-MM-dd HH:mm}'" : $"[{GuiInfo.RefName}].{cell.FieldName} <= '{va:yyyy-MM-dd HH:mm}'";
                                advo = cell.Operator == "ge" ? AdvSearchOperation.GreaterEqualDatime : AdvSearchOperation.LessEqualDatime;
                            }
                        }
                    }
                    lisToast.Add(hl.ShortDesc + " <span class='text-danger'>" + cell.OperatorText + "</span> " + cell.ValueText);
                }
                var value = ids ?? cell.Value;
                if (AdvSearchVM.Conditions.Any(x => x.Field.FieldName == cell.FieldName
                && cell.FieldName != IdField
                && x.CompareOperatorId == advo
                && (x.CompareOperatorId == AdvSearchOperation.Like || x.CompareOperatorId == AdvSearchOperation.In || x.CompareOperatorId == AdvSearchOperation.EqualDatime)) && !cell.Shift)
                {
                    AdvSearchVM.Conditions.FirstOrDefault(x => x.Field.FieldName == cell.FieldName && x.CompareOperatorId == advo).Value = value.IsNullOrWhiteSpace() ? cell.ValueText : value;
                    Wheres.FirstOrDefault(x => x.FieldName.Contains($"[{GuiInfo.RefName}].{cell.FieldName}")).FieldName = where;
                }
                else
                {
                    if (!AdvSearchVM.Conditions.Any(x => x.Field.FieldName == cell.FieldName && x.CompareOperatorId == advo && x.Value == cell.Value))
                    {
                        AdvSearchVM.Conditions.Add(new FieldCondition
                        {
                            Field = hl,
                            CompareOperatorId = advo,
                            LogicOperatorId = cell.Logic ?? LogicOperation.And,
                            Value = value.IsNullOrWhiteSpace() ? cell.ValueText : value,
                            Group = cell.Group
                        });
                        Wheres.Add(new Where()
                        {
                            FieldName = where,

                            Group = cell.Group
                        });
                    }
                }
            });
            await ApplyFilter(true);
            Spinner.Hide();
            if (GuiInfo.ComponentType == nameof(VirtualGrid) && GuiInfo.CanSearch)
            {
                ListViewSearch.Focus();
            }
            Toast.Success(lisToast.Combine("</br>"));
        }

        public void FilterSelected(object ev)
        {
            if (ev["Operator"] is null)
            {
                return;
            }
            var value = ev["Value"] is null ? null : ev["Value"].ToString();
            var valueText = ev["ValueText"] is null ? null : ev["ValueText"].ToString();
            var fileName = ev["FieldName"] is null ? null : ev["FieldName"].ToString();
            var op = ev["Operator"] is null ? null : ev["Operator"].ToString();
            var opText = ev["OperatorText"] is null ? null : ev["OperatorText"].ToString();
            var isSearch = ev["IsSearch"] is null ? true : false;
            if (!CellSelected.Any(x => x.FieldName == fileName && x.Value == value && x.ValueText == valueText && x.Operator == op))
            {
                var header = Header.FirstOrDefault(x => x.FieldName == fileName);
                CellSelected.Add(new CellSelected
                {
                    FieldName = fileName,
                    FieldText = header.ShortDesc,
                    ComponentType = header.ComponentType,
                    Value = value,
                    ValueText = valueText,
                    Operator = op,
                    OperatorText = opText,
                    IsSearch = isSearch
                });
            }
            _summarys.Add(new HTMLElement());
            Task.Run(async () =>
            {
                await ActionFilter();
            });
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

        public virtual void ViewSumary(object ev, GridPolicy header)
        {
            Html.Take(Element).Div.ClassName("backdrop")
            .Style("align-items: center;").Escape((e) => DisposeSumary());
            _summarys.Add(Html.Context);
            Html.Instance.Div.ClassName("popup-content confirm-dialog").Style("top: 0;min-width: 90%")
                .Div.ClassName("popup-title").InnerHTML("Gộp theo cột hiện thời")
                .Div.ClassName("icon-box").Span.ClassName("fa fa-times")
                    .Event(EventType.Click, DisposeSumary)
                .EndOf(".popup-title")
                .Div.ClassName("popup-body scroll-content");
            Html.Instance.Div.ClassName("container-rpt");
            Html.Instance.Div.ClassName("menuBar");
            Html.Instance.EndOf(".menuBar");
            Html.Instance.Div.ClassName("printable");
            var body = Html.Context;
            Task.Run(async () =>
            {
                var gridPolicy = BasicHeader.Where(x => x.ComponentType == nameof(Number) && x.FieldName != header.FieldName).ToList();
                var sum = gridPolicy.Select(x => $"FORMAT(SUM(isnull([{GuiInfo.RefName}].{x.FieldName},0)),'#,#') as {x.FieldName}").ToList();

                var filter = Wheres.Where(x => !x.Group).Select(x => x.FieldName).Combine(" and ");
                var filter1 = Wheres.Where(x => x.Group).Select(x => x.FieldName).Combine(" or ");
                var wh = new List<string>();
                if (!filter.IsNullOrWhiteSpace())
                {
                    wh.Add($"({filter})");
                }
                if (!filter1.IsNullOrWhiteSpace())
                {
                    wh.Add($"({filter1})");
                }
                var stringWh = wh.Any() ? $"({wh.Combine(" and ")})" : "";
                var submitEntity = GuiInfo.PreQuery;
                if (_preQueryFn != null)
                {
                    submitEntity = (string)_preQueryFn.Call(null, this);
                }
                var dataSet = await new Client(GuiInfo.RefName).PostAsync<object[][]>(sum.Combine(), $"ViewSumary?group={header.FieldName}" +
                    $"&tablename={GuiInfo.RefName}" +
                    $"&refname={header.RefName}" +
                    $"&formatsumary={GuiInfo.FormatSumaryField}" +
                    $"&sql={Sql}&orderby={GuiInfo.OrderBySumary}" +
                    $"&where={stringWh} {(submitEntity.IsNullOrWhiteSpace() ? "" : $"{(Wheres.Any() ? " and " : "")} {submitEntity}")}");
                var sumarys = dataSet[0];
                object[] refn = null;
                if (dataSet.Length > 1)
                {
                    refn = dataSet[1];
                }
                var id = "sumary" + (new Random(10)).GetHashCode();
                var dir = refn?.ToDictionary(x => x[IdField]);
                Html.Instance.Div.ClassName("grid-wrapper sticky").Div.ClassName("table-wrapper printable").Table.Id(id).Width("100%").ClassName("table")
                .Thead
                    .TRow.Render();
                Html.Instance.Th.Style("max-width: 100%;").IText(header.ShortDesc).End.Render();
                Html.Instance.Th.Style("max-width: 100%;").IText("Tổng dữ liệu").End.Render();
                foreach (var item in gridPolicy)
                {
                    Html.Instance.Th.Style("max-width: 100%;").IHtml(item.ShortDesc).End.Render();
                }
                Html.Instance.EndOf(ElementType.thead);
                Html.Instance.TBody.Render();
                var ttCount = sumarys.Sum(x => Convert.ToDecimal(x["TotalRecord"].ToString().Replace(",", "") == "" ? "0" : x["TotalRecord"].ToString().Replace(",", "")));
                foreach (var item in sumarys)
                {
                    item[header.FieldName] = item[header.FieldName] ?? "";
                    var dataHeader = item[header.FieldName].ToString();
                    var value = string.Empty;
                    var valueText = string.Empty;
                    if (header.ComponentType == "Dropdown")
                    {
                        var ob = dir.GetValueOrDefault(item[header.FieldName]);
                        if (ob is null)
                        {
                            dataHeader = "";
                        }
                        else
                        {
                            dataHeader = ob[header.FormatCell.Split("}")[0].Replace("{", "")].ToString();
                            value = ob["Id"].ToString();
                            valueText = dataHeader;
                        }
                    }
                    else if (header.ComponentType == nameof(Datepicker))
                    {
                        var datetime = DateTimeExt.TryParseDateTime(item[header.FieldName].ToString());
                        dataHeader = datetime?.ToString("dd/MM/yyyy");
                        value = datetime?.ToString("dd/MM/yyyy");
                        valueText = datetime?.ToString("dd/MM/yyyy");
                    }
                    else if (header.ComponentType == nameof(Number))
                    {
                        var datetime = (item[header.FieldName] is null || item[header.FieldName].ToString() == "") ? default(decimal) : decimal.Parse(item[header.FieldName].ToString());
                        dataHeader = datetime == default(decimal) ? "" : datetime.ToString("N0");
                        value = item[header.FieldName].ToString();
                        valueText = dataHeader;
                    }
                    else
                    {
                        value = item[header.FieldName].ToString();
                        valueText = item[header.FieldName].ToString();
                    }
                    Html.Instance.TRow.Event(EventType.DblClick, () => FilterSumary(header, value, valueText)).Event(EventType.Click, (e) => FocusCell(e, this.HeaderComponentMap[header.GetHashCode()])).Render();
                    Html.Instance.TData.Style("max-width: 100%;").ClassName(header.ComponentType == nameof(Number) ? "text-right" : "text-left").IText(dataHeader.DecodeSpecialChar()).End.Render();
                    Html.Instance.TData.Style("max-width: 100%;").ClassName("text-right").IText(item["TotalRecord"].ToString()).End.Render();
                    foreach (var itemDetail in gridPolicy)
                    {
                        Html.Instance.TData.Style("max-width: 100%;").ClassName("text-right").IText(item[itemDetail.FieldName].ToString()).End.Render();
                    }
                    Html.Instance.EndOf(ElementType.tr);
                }
                Html.Instance.EndOf(ElementType.tbody);
                Html.Instance.TFooter.TRow.ClassName("summary").Render();
                Html.Instance.TData.Style("max-width: 100%;").IText("Tổng cộng").End.Render();
                Html.Instance.TData.ClassName("text-right").Style("max-width: 100%;").IText(ttCount.ToString("N0")).End.Render();
                foreach (var item in gridPolicy)
                {
                    var de = sumarys.Select(x => x[item.FieldName].ToString().Replace(",", "")).ToList();
                    var ttCount1 = de.Where(x => !x.IsNullOrWhiteSpace()).Sum(x => decimal.Parse(x));
                    Html.Instance.TData.ClassName("text-right").Style("max-width: 100%;").IHtml(ttCount1.ToString("N0")).End.Render();
                }
                await Client.LoadScript("//cdn.datatables.net/1.13.2/js/jquery.dataTables.min.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/dataTables.buttons.min.js");
                await Client.LoadScript("//cdnjs.cloudflare.com/ajax/libs/jszip/3.1.3/jszip.min.js");
                await Client.LoadScript("//cdnjs.cloudflare.com/ajax/libs/pdfmake/0.1.53/pdfmake.min.js");
                await Client.LoadScript("//cdnjs.cloudflare.com/ajax/libs/pdfmake/0.1.53/vfs_fonts.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/buttons.html5.min.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/buttons.html5.min.js");
                await Client.LoadScript("//cdn.datatables.net/buttons/2.3.4/js/buttons.print.min.js");
                /*@
                if (!$.fn.DataTable.isDataTable('#'+id)){
                  $('#'+id).DataTable({
                    paging: false,
                    info: false,
                    dom: 'Bfrtip',
                    buttons: [
                        'copy', 'csv', 'excel', 'pdf', 'print'
                    ]
                });
                }
                */
            });
        }

        private void FocusCell(Event e, Component header)
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

        public void FilterSumary(GridPolicy gridPolicy, string value, string valueText)
        {
            var dateTime = CellSelected.FirstOrDefault(x => gridPolicy.ComponentType == nameof(Datepicker) && x.FieldName == gridPolicy.FieldName && x.Operator == "in");
            if (dateTime is null)
            {
                if (!CellSelected.Any(x => x.FieldName == gridPolicy.FieldName && x.Value == value && x.Operator == "in"))
                {
                    var header = Header.FirstOrDefault(x => x.FieldName == gridPolicy.FieldName);
                    CellSelected.Add(new CellSelected
                    {
                        FieldName = gridPolicy.FieldName,
                        FieldText = header.ShortDesc,
                        ComponentType = header.ComponentType,
                        Value = value,
                        ValueText = valueText,
                        Operator = "in",
                        OperatorText = "chứa",
                        IsSearch = true
                    });
                }
            }
            else
            {
                dateTime.Value = value;
                dateTime.ValueText = valueText;
            }
            HiddenSumary();
            Task.Run(async () =>
            {
                await ActionFilter();
            });
        }

        internal void HotKeyHandler(Event e, Component header, ListViewItem focusedRow)
        {
            EditableComponent com = focusedRow.Children.FirstOrDefault(x => x.GuiInfo.Id == LastComponentFocus.Id);
            var el = e.Target as HTMLElement;
            el = el.Closest(ElementType.td.ToString());
            var keyCode = e.KeyCodeEnum();
            ActionKeyHandler(e, header, focusedRow, com, el, keyCode);
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
                fieldName = com.GuiInfo.FieldName;
                switch (com.GuiInfo.ComponentType)
                {
                    case "Dropdown":
                        value = focusedRow.Entity.GetPropValue(header.FieldName) is null ? null : focusedRow.Entity.GetPropValue(header.FieldName).ToString().EncodeSpecialChar();
                        break;
                    case nameof(Number):
                        value = focusedRow.Entity.GetPropValue(header.FieldName) is null ? null : focusedRow.Entity.GetPropValue(header.FieldName).ToString().Replace(",", "");
                        break;
                    case nameof(Checkbox):
                        value = com.GetValue() is null ? null : com.GetValue().ToString().EncodeSpecialChar().ToLower();
                        break;
                    default:
                        value = com.GetValue() is null ? null : com.GetValue().ToString().EncodeSpecialChar();
                        break;
                }
                if (value is null)
                {
                    text = null;
                }
                else
                {
                    text = com.GetValue() is null ? null : com.GetValue().ToString().DecodeSpecialChar();
                }
            }

            switch (keyCode)
            {
                case KeyCodeEnum.F11:
                    Task.Run(async () =>
                    {
                        var th = HeaderSection.Children.FirstOrDefault(x => x.GuiInfo.Id == com.GuiInfo.Id);
                        th.Element.RemoveClass("desc");
                        th.Element.RemoveClass("asc");
                        if (SortedField is null)
                        {
                            SortedField = new List<SortedField>()
                            {
                                new SortedField
                                {
                                    Field = com.GuiInfo.FieldName,
                                    Desc = true,
                                    Com = com.GuiInfo.CastProp<GridPolicy>(),
                                }
                            };
                            th.Element.AddClass("desc");
                        }
                        else
                        {
                            var check = SortedField.FirstOrDefault(x => x.Field == com.GuiInfo.FieldName);
                            if (check != null)
                            {
                                if ((!check.Desc) == false)
                                {
                                    SortedField.FirstOrDefault(x => x.Field == com.GuiInfo.FieldName).Desc = !check.Desc;
                                    th.Element.AddClass(!check.Desc ? "asc" : "desc");
                                }
                                else
                                {
                                    SortedField.Remove(SortedField.FirstOrDefault(x => x.Field == com.GuiInfo.FieldName));
                                }
                            }
                            else
                            {
                                if (!e.ShiftKey())
                                {
                                    HeaderSection.Children.ForEach(x =>
                                    {
                                        x.Element.RemoveClass("desc");
                                        x.Element.RemoveClass("asc");
                                    });
                                    SortedField.Clear();
                                }
                                th.Element.AddClass("desc");
                                SortedField.Add(new SortedField
                                {
                                    Field = com.GuiInfo.FieldName,
                                    Com = com.GuiInfo.CastProp<GridPolicy>(),
                                    Desc = true
                                });
                            }
                        }
                        AdvSearchVM.OrderBy.Clear();
                        AdvSearchVM.OrderBy.AddRange(SortedField.Select(x => new OrderBy
                        {
                            Field = x.Com,
                            FieldId = x.Com.Id,
                            OrderbyOptionId = x.Desc ? OrderbyOption.DESC : OrderbyOption.ASC
                        }).ToList());
                        LocalStorage.SetItem("OrderBy" + GuiInfo.Id, AdvSearchVM.OrderBy);
                        await ActionFilter();
                    });
                    break;
                case KeyCodeEnum.F4:
                    if (focusedRow is null)
                    {
                        return;
                    }
                    var menu = ContextMenu.Instance;
                    menu.Top = el.GetBoundingClientRect().Top;
                    menu.Left = el.GetBoundingClientRect().Left;
                    menu.MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Icon = "fal fa-angle-double-right", Text = "Chứa", Click = FilterInSelected,
                                Parameter = new { Operator = "in", OperatorText = "Chứa", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey()  } },
                        new ContextMenuItem { Icon = "fal fa-not-equal", Text = "Không chứa", Click = FilterInSelected,
                                Parameter = new { Operator="not in",OperatorText= "Không chứa", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }}
                    };
                    if (com.GuiInfo.ComponentType == nameof(Number) || com.GuiInfo.ComponentType == nameof(Datepicker))
                    {
                        menu.MenuItems.AddRange(new List<ContextMenuItem>
                        {
                            new ContextMenuItem { Icon = "fal fa-greater-than", Text = "Lớn hơn", Click = FilterInSelected,
                                    Parameter = new { Operator="gt", OperatorText= "Lớn hơn", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                            new ContextMenuItem { Icon = "fal fa-less-than", Text = "Nhỏ hơn", Click = FilterInSelected,
                                    Parameter = new { Operator="lt", OperatorText= "Nhỏ hơn", Value = value, FieldName = fieldName, ValueText=text, Shift = e.ShiftKey() }},
                            new ContextMenuItem { Icon = "fal fa-greater-than-equal", Text = "Lớn hơn bằng", Click = FilterInSelected,
                                    Parameter = new { Operator="ge", OperatorText= "Lớn hơn bằng", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                            new ContextMenuItem { Icon = "fal fa-less-than-equal", Text = "Nhỏ hơn bằng", Click = FilterInSelected,
                                    Parameter = new { Operator="le", OperatorText= "Nhỏ hơn bằng", Value = value, FieldName = fieldName, ValueText = text, Shift = e.ShiftKey() }},
                        });
                    }
                    menu.Render();
                    break;
                case KeyCodeEnum.F8:
                    var selectedRows = GetSelectedRows().ToList();
                    if (selectedRows.Nothing())
                    {
                        Toast.Warning("Vui lòng chọn dòng cần xóa");
                        return;
                    }
                    var gridPolicies = EditForm.GetElementPolicies(GuiInfo.Id, Utils.ComponentId);
                    var isOwner = selectedRows.All(x => Utils.IsOwner(x, false));
                    var canDelete = CanDo(gridPolicies, x => x.CanDelete && isOwner || x.CanDeleteAll);
                    if (canDelete)
                    {
                        HardDeleteSelected();
                    }
                    break;
                case KeyCodeEnum.F9:
                    if (value.IsNullOrWhiteSpace())
                    {
                        FilterSelected(new { Operator = "in", OperatorText = "Chứa", Value = value, FieldName = fieldName, ValueText = text, IsSearch = false });
                    }
                    else
                    {
                        FilterSelected(new { Operator = "in", OperatorText = "Chứa", Value = value, FieldName = fieldName, ValueText = text });
                    }
                    com.Focus();
                    break;
                case KeyCodeEnum.F10:
                    var header1 = Header.FirstOrDefault(x => x.Id == com.GuiInfo.Id);
                    ViewSumary(e, header1);
                    break;
                case KeyCodeEnum.F2:
                    if (value.IsNullOrWhiteSpace())
                    {
                        FilterSelected(new { Operator = "not in", OperatorText = "Loại trừ", Value = value, FieldName = fieldName, ValueText = text, IsSearch = false });
                    }
                    else
                    {
                        FilterSelected(new { Operator = "not in", OperatorText = "Loại trừ", Value = value, FieldName = fieldName, ValueText = text });
                    }
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
                        currentItemUp.Focus();
                        return;
                    }
                    if (upItemUp.EmptyRow)
                    {
                        return;
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
                        currentItemDown.Focus();
                        return;
                    }
                    if (upItemDown.EmptyRow)
                    {
                        return;
                    }
                    CoppyValue(e, com, fieldName, currentItemDown, upItemDown);
                    break;
                case KeyCodeEnum.LeftArrow:
                    if (!GuiInfo.IsRealtime)
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
                    if (upItemLeft.GuiInfo.Editable && !upItemLeft.Disabled)
                    {
                        if (upItemLeft.Element is HTMLInputElement html)
                        {
                            html.SelectionStart = 0;
                            html.SelectionEnd = upItemLeft.GetValueText().Length;
                        }
                    }
                    break;
                case KeyCodeEnum.RightArrow:
                    if (!GuiInfo.IsRealtime)
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
                    if (upItemRight.GuiInfo.Editable && !upItemRight.Disabled)
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
                    Task.Run(async () =>
                    {
                        await RenderViewPort();
                        var upItemHome = AllListViewItem.FirstOrDefault();
                        if (upItemHome != null)
                        {
                            upItemHome.Focused = true;
                            var upComponent = upItemHome.FirstOrDefault(x => x.GuiInfo.FieldName == fieldName);
                            var tdup = upComponent.Element.Closest(ElementType.td.ToString());
                            upItemHome.Focus();
                            tdup.Focus();
                        }
                    });
                    break;
                case KeyCodeEnum.End:
                    var lastSelectedEnd = GetSelectedRows().LastOrDefault();
                    var currentItemEnd = AllListViewItem.FirstOrDefault(x => x.Entity == lastSelectedEnd);
                    if (currentItemEnd != null)
                    {
                        currentItemEnd.Focused = false;
                    }
                    DataTable.ParentElement.ScrollTop = DataTable.ParentElement.ScrollHeight;
                    Task.Run(async () =>
                    {
                        await RenderViewPort();
                        var upItemEnd = AllListViewItem.LastOrDefault();
                        if (upItemEnd != null)
                        {
                            upItemEnd.Focused = true;
                            var upComponent = upItemEnd.FirstOrDefault(x => x.GuiInfo.FieldName == fieldName);
                            var tdup = upComponent.Element.Closest(ElementType.td.ToString());
                            upItemEnd.Focus();
                            tdup.Focus();
                        }
                    });
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
                        currentItemD.Entity.SetComplexPropValue(fieldName, upItemD.Entity.GetPropValue(com.GuiInfo.FieldName));
                        var updated = currentItemD.FilterChildren(x => x.GuiInfo.FieldName == com.GuiInfo.FieldName).FirstOrDefault();
                        updated.Dirty = true;
                        Task.Run(async () =>
                        {
                            if (updated.GuiInfo.ComponentType == nameof(SearchEntry) || updated.GuiInfo.ComponentType == "Dropdown")
                            {
                                updated.UpdateView();
                                var dropdown = com as SearchEntry;
                                updated.PopulateFields(dropdown.Matched);
                                await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, currentItemD.Entity, dropdown.Matched);
                            }
                            else
                            {
                                updated.UpdateView();
                                updated.PopulateFields();
                                await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, currentItemD.Entity);
                            }
                            await currentItemD.ListViewSection.ListView.DispatchEventToHandlerAsync(upItemD.ListViewSection.ListView.GuiInfo.Events, EventType.Change, upItemD.Entity);
                            if (GuiInfo.IsRealtime)
                            {
                                await currentItemD.PatchUpdate();
                            }
                        });
                    }
                    break;
                default:
                    break;
            }
        }

        internal virtual async Task RenderViewPort(bool count = true, bool firstLoad = false)
        {
            // not to do anything
        }

        public virtual void HotKeyF6Handler(Event e, KeyCodeEnum? keyCode)
        {
            if (keyCode == KeyCodeEnum.F6)
            {
                e.PreventDefault();
                e.StopPropagation();
                if (_summarys.Any())
                {
                    var lastElement = _summarys.LastOrDefault();
                    if (lastElement.InnerHTML == string.Empty)
                    {
                        _summarys.RemoveAt(_summarys.Count - 1);
                        lastElement.Remove();
                        if (CellSelected.Count > 0)
                        {
                            CellSelected.RemoveAt(CellSelected.Count - 1);
                            if (Wheres.Count - 1 >= 0)
                            {
                                Wheres.RemoveAt(Wheres.Count - 1);
                            }
                            AdvSearchVM.Conditions.RemoveAt(AdvSearchVM.Conditions.Count - 1);
                            Task.Run(async () =>
                            {
                                await ActionFilter();
                            });
                        }
                    }
                    else
                    {
                        lastElement.Show();
                    }
                }
            }
            else if (keyCode == KeyCodeEnum.F3)
            {
                e.PreventDefault();
                e.StopPropagation();
                Task.Run(async () =>
                {
                    var selected = await GetRealTimeSelectedRows();
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
            }
            else if (keyCode == KeyCodeEnum.F1)
            {
                e.PreventDefault();
                e.StopPropagation();
                ToggleAll();
            }
            if (LastListViewItem is null)
            {
                return;
            }
            var com = LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == LastComponentFocus.Id);
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
            var nextcom = upItem.FilterChildren(x => x.GuiInfo.Id == com.GuiInfo.Id).FirstOrDefault();
            if (nextcom != null)
            {
                LastComponentFocus = nextcom.GuiInfo;
                nextcom.ParentElement?.Focus();
                nextcom.Focus();
                if (nextcom.GuiInfo.Editable && !nextcom.Disabled)
                {
                    if (nextcom.Element is HTMLInputElement html)
                    {
                        html.SelectionStart = 0;
                        html.SelectionEnd = nextcom.GetValueText().Length;
                    }
                }
                if (!GuiInfo.VirtualScroll)
                {
                    if (SelectedIds.Count == 1)
                    {
                        ClearSelected();
                    }
                    upItem.Selected = true;
                }
                if (e.ShiftKey())
                {
                    upItem.Entity.SetComplexPropValue(fieldName, com.GetValue());
                    var updated = upItem.FilterChildren(x => x.GuiInfo.FieldName == nextcom.GuiInfo.FieldName).FirstOrDefault();
                    if (updated.Disabled || !updated.GuiInfo.Editable)
                    {
                        return;
                    }
                    updated.Dirty = true;
                    Task.Run(async () =>
                    {
                        if (updated.GuiInfo.ComponentType == nameof(SearchEntry) || updated.GuiInfo.ComponentType == "Dropdown")
                        {
                            updated.UpdateView();
                            var dropdown = com as SearchEntry;
                            updated.PopulateFields(dropdown.Matched);
                            await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, upItem.Entity, dropdown.Matched);
                        }
                        else
                        {
                            updated.UpdateView();
                            updated.PopulateFields();
                            await updated.DispatchEventToHandlerAsync(updated.GuiInfo.Events, EventType.Change, upItem.Entity);
                        }
                        await upItem.ListViewSection.ListView.DispatchEventToHandlerAsync(upItem.ListViewSection.ListView.GuiInfo.Events, EventType.Change, upItem.Entity);
                        if (GuiInfo.IsRealtime)
                        {
                            if (int.Parse(upItem.Entity[IdField].ToString()) > 0)
                            {
                                await upItem.PatchUpdate();
                            }
                            else
                            {
                                await upItem.CreateUpdate();
                            }
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
            if (Disabled || !Editable || EmptyRowSection?.Children.HasElement() == true)
            {
                return;
            }
            var emptyRowData = EmptyRowData();
            if (!GuiInfo.DefaultVal.IsNullOrWhiteSpace())
            {
                var json = string.Empty;
                if (Utils.IsFunction(GuiInfo.DefaultVal, out var fn))
                {
                    json = fn.Call(this, emptyRowData, Entity).ToString();
                }
                var entity = JsonConvert.DeserializeObject<object>(json);
                emptyRowData.CopyPropFromAct(entity);
            }
            emptyRowData[IdField] = -Math.Abs(emptyRowData.GetHashCode()); // Not to add this row into the submitted list
            var rowSection = RenderRowData(Header, emptyRowData, EmptyRowSection, null, true);
            if (!GuiInfo.TopEmpty)
            {
                DataTable.InsertBefore(MainSection.Element, EmptyRowSection.Element);
            }
            else
            {
                DataTable.InsertBefore(EmptyRowSection.Element, MainSection.Element);
            }
            Task.Run(async () =>
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterEmptyRowCreated, emptyRow);
            });
        }

        private object EmptyRowData()
        {
            var type = Type.GetType((GuiInfo.Reference.Namespace ?? Client.ModelNamespace) + GuiInfo.RefName) ?? typeof(object);
            return Activator.CreateInstance(type);
        }

        protected override List<GridPolicy> FilterColumns(List<GridPolicy> gridPolicy)
        {
            var specificComponent = gridPolicy.Any(x => x.ComponentId == GuiInfo.Id);
            if (specificComponent)
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == GuiInfo.Id).ToList();
            }
            else
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == null).ToList();
            }

            var permission = EditForm.GetElementPolicies(gridPolicy.Select(x => x.Id).ToArray(), Utils.GridPolicyId);
            var headers = gridPolicy.Where(x => !x.Hidden)
                .Where(header => !header.IsPrivate || permission.Where(x => x.RecordId == header.Id).HasElementAndAll(policy => policy.CanRead))
                .Select(CalcTextAlign).OrderByDescending(x => x.Frozen).ThenByDescending(header => header.ComponentType == "Button")
                .ThenBy(x => x.Order).ToList();
            OrderHeaderGroup(headers);
            Header.Clear();
            Header.Add(ToolbarColumn);
            Header.AddRange(headers);
            HeaderComponentMap = Header.DistinctBy(x => x.GetHashCode()).ToDictionary(x => x.GetHashCode(), x => x.MapToComponent());
            return headers;
        }

        public override async Task ApplyFilter(bool searching = true)
        {
            _sum = false;
            var calcFilter = CalcFilterQuery(searching);
            DataTable.ParentElement.ScrollTop = 0;
            await ReloadData(calcFilter, cache: false, search: searching);
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
            FormattedRowData = GuiInfo.LocalRender ? GuiInfo.LocalData : RowData.Data;
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
                Task.Run(async () =>
                {
                    await UpdateExistRowsWrapper(loadMasterData: true, dirty: false, 0, viewPort);
                });
                return;
            }
            MainSection.Show = false;
            FormattedRowData.ToList().ForEach((rowData) =>
            {
                Html.Take(MainSection.Element);
                RenderRowData(Header, rowData, MainSection);
            });
            MainSection.Show = true;
            RenderIndex();
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
                    .FirstOrDefault(x => x.Entity[IdField].As<int>() == EntityFocusId && x.GuiInfo.Id == LastComponentFocus.Id);
                if (element != null)
                {
                    var lastListView = AllListViewItem.FirstOrDefault(x => x.Entity[IdField].As<int>() == EntityFocusId);
                    lastListView.Focused = true;
                    element.ParentElement.AddClass("cell-selected");
                    LastListViewItem = lastListView;
                    LastComponentFocus = element.GuiInfo;
                    LastElementFocus = element.Element;
                }
                else
                {
                    ListViewSearch.Element.Focus();
                }
            }
            else
            {
                ListViewSearch.Element.Focus();
            }
        }

        private bool _hasFirstLoad = false;
        protected async Task UpdateExistRowsWrapper(bool loadMasterData, bool? dirty, int skip, int viewPort)
        {
            if (!_hasFirstLoad)
            {
                _hasFirstLoad = true;
                return;
            }
            if (loadMasterData)
            {
                await LoadMasterData(FormattedRowData);
            }
            UpdateExistRows(dirty);
            RenderIndex();
            DomLoaded();
        }

        protected void UpdateExistRows(bool? dirty)
        {
            var updatedData = FormattedRowData.ToArray();
            var dataSections = AllListViewItem.Take(updatedData.Length).ToArray();
            dataSections.ForEach((child, index) =>
            {
                child.Entity = updatedData[index];
                child.Children.Flattern(x => x.Children).ForEach(x =>
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

        public override ListViewItem RenderRowData(List<GridPolicy> headers, object row, Section section, int? index = null, bool emptyRow = false)
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
                GuiInfo = GuiInfo
            };
            section.AddChild(rowSection, index);
            var id = row[IdField].As<int>();
            if (id <= 0 && !emptyRow)
            {
                rowSection.Dirty = true;
            }
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
            if (headers.HasElement())
            {
                headers.ForEach(header => rowSection.RenderTableCell(row, HeaderComponentMap[header.GetHashCode()]));
            }
            if (emptyRow)
            {
                Children.ForEach(x => x.AlwaysLogHistory = true);
            }
            var isApproved = row["StatusId"].As<int?>() == (int)ApprovalStatusEnum.Approved || row["StatusId"].As<int?>() == (int)ReceiptStatusEnum.Finished;
            if (isApproved)
            {
                rowSection.Disabled = true;
                rowSection.SetDisabled(false, "btnEdit");
            }
            if (Disabled)
            {
                rowSection.SetDisabled(false, "btnEdit");
            }
            var owed = row["InsertedBy"].As<int?>() == Client.Token.UserId;
            if (isApproved)
            {
                rowSection.Disabled = true;
                rowSection.SetDisabled(false, "btnEdit");
            }
            if (Disabled)
            {
                rowSection.SetDisabled(false, "btnEdit");
            }
            if (row["Id"].As<int?>() > 0)
            {
                rowSection.Element.RemoveClass("new-row");
            }
            else
            {
                rowSection.Element.AddClass("new-row");
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

        private void RenderSummaryRow(GridPolicy sum, List<GridPolicy> headers, HTMLTableSectionElement footer, int count)
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
            RowData._data.Clear();
            var hasElement = listData.HasElement();
            if (hasElement)
            {
                listData.ForEach(RowData._data.Add); // Not to use AddRange because the _data is not always List
            }
            RenderContent();
            if (Entity != null && ShouldSetEntity)
            {
                Entity.SetComplexPropValue(GuiInfo.FieldName, RowData.Data);
            }
        }

        private static void SetSummaryHeaderText(GridPolicy sum, HTMLTableRowElement tr)
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

        private HTMLTableRowElement CreateSummaryTableRow(GridPolicy sum, HTMLTableSectionElement footer, int count)
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
            result.Children.ForEach(x => x.InnerHTML = null);
            return result;
        }

        private void CalcSumCol(GridPolicy header, List<GridPolicy> headers, HTMLTableRowElement tr, int colSpan)
        {
            var index = headers.IndexOf(header);
            var cellVal = tr.Cells[index - colSpan + 1];
            var format = header.FormatCell.IsNullOrWhiteSpace() ? "{0:n0}" : header.FormatCell;
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
            if (rowSection.EmptyRow && observableArgs.EvType == EventType.Change)
            {
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreated, rowData, this);
                object rs;
                if (GuiInfo.IsRealtime)
                {
                    var entity = rowData;
                    entity.ClearReferences();
                    rs = await new Client(GuiInfo.Reference.Name).CreateAsync<object>(entity);
                    rowSection.Entity.CopyPropFrom(rs);
                    if (GuiInfo.ComponentType == nameof(VirtualGrid))
                    {
                        CacheData.Add(rs);
                    }
                    Dirty = false;
                }
                else
                {
                    rs = rowSection.Entity;
                    Dirty = true;
                }
                if (GuiInfo.ComponentType != nameof(VirtualGrid))
                {
                    Entity.SetComplexPropValue(GuiInfo.FieldName, RowData.Data);
                }
                await LoadMasterData(new object[] { rs });
                rowSection.UpdateView(true);
                MoveEmptyRow(rowSection);
                EmptyRowSection.Children.Clear();
                AddNewEmptyRow();
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, rowData);
            }
            PopulateFields();
            RenderIndex();
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, rowData, rowSection);
            if (GuiInfo.IsSumary)
            {
                AddSummaries();
            }
            LastListViewItem = rowSection;
            var headers = Header.Where(y => y.Editable).ToList();
            var currentComponent = headers.FirstOrDefault(y => y.FieldName == component?.GuiInfo.FieldName);
            if (currentComponent.ComponentType == nameof(SearchEntry) || currentComponent.ComponentType == "Dropdown")
            {
                var index = headers.IndexOf(currentComponent);
                if (headers.Count > index + 1)
                {
                    var nextGrid = headers[index + 1];
                    var nextComponent = rowSection.Children.Where(y => y?.GuiInfo.FieldName == nextGrid.FieldName).FirstOrDefault();
                    rowSection.Focused = true;
                    nextComponent.Focus();
                }
            }
        }

        private void MoveEmptyRow(ListViewItem rowSection)
        {
            if (RowData.Data.Contains(rowSection.Entity))
            {
                return;
            }
            if (GuiInfo.TopEmpty)
            {
                RowData.Data.Insert(0, rowSection.Entity);
                if (!MainSection.Children.Contains(EmptyRowSection.FirstChild))
                {
                    MainSection.Children.Insert(0, EmptyRowSection.FirstChild);
                }
                MainSection.Element.Prepend(EmptyRowSection.Element.FirstElementChild);
            }
            else
            {
                RowData.Data.Add(rowSection.Entity);
                MainSection.Element.AppendChild(EmptyRowSection.Element.FirstElementChild);
                if (!MainSection.Children.Contains(EmptyRowSection.FirstChild))
                {
                    MainSection.Children.Add(EmptyRowSection.FirstChild);
                }
            }
            if (GuiInfo.IsRealtime)
            {
                rowSection.Element.RemoveClass("new-row");
            }
            rowSection.Parent = MainSection;
            rowSection.ListViewSection = MainSection;
        }

        protected virtual void RenderTableHeader(List<GridPolicy> headers)
        {
            if (headers.Nothing())
            {
                headers = Header;
            }
            if (HeaderSection.Element is null)
            {
                AddSections();
            }
            headers.ForEach((x, index) => x.PostOrder = index);
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
                    .Style($"min-width: {header.MinWidth}; max-width: {header.MaxWidth}")
                    .TextAlign(TextAlign.center)
                    .Event(EventType.ContextMenu, HeaderContextMenu, header)
                    .Event(EventType.FocusOut, (e) => FocusOutHeader(e, header))
                    .Event(EventType.KeyDown, (e) => ThHotKeyHandler(e, header));
                HeaderSection.AddChild(new Section(Html.Context) { GuiInfo = header.MapToComponent() });
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
                var orderBy = AdvSearchVM.OrderBy.FirstOrDefault(x => x.FieldId == header.Id);
                if (orderBy != null)
                {
                    Html.Instance.ClassName(orderBy.OrderbyOptionId == OrderbyOption.ASC ? "asc" : "desc").Render();
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
                if (Client.CheckHasRole(RoleEnum.System))
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
                        HeaderSection.AddChild(new Section(Html.Context.ParentElement) { GuiInfo = header.MapToComponent() });
                    }
                });
            }
            HeaderSection.Children = HeaderSection.Children.OrderBy(x => x.GuiInfo.PostOrder).ToList();
            if (!GuiInfo.Focus)
            {
                ColumnResizeHandler();
            }
        }
        private int _imeout;

        private void ChangeHeader(Event e, GridPolicy header)
        {
            Window.ClearTimeout(_imeout);
            _imeout = Window.SetTimeout(async () =>
            {
                var headerDB = await new Client(nameof(GridPolicy)).GetAsync<GridPolicy>(header.Id);
                var html = e.Target as HTMLElement;
                headerDB.ShortDesc = html.TextContent.Trim();
                await new Client(nameof(GridPolicy)).UpdateAsync<GridPolicy>(headerDB);
            }, 1000);
        }

        protected override async Task<List<object>> CustomQuery(object submitEntity)
        {
            var ds = await new Client(nameof(User)).PostAsync<object[][]>(submitEntity, "Cmd");
            if (ds.Nothing())
            {
                SetRowData(null);
                return null;
            }
            var total = ds.Length > 1 ? ds[1].ToDynamic()[0].total : ds[0].Length;
            if (ds.Length > 3)
            {
                var customHeaders = ds[2].Select(x => x.CastProp<GridPolicy>()).ToList();
                FilterColumns(customHeaders);
                RenderTableHeader(Header);
            }
            var rows = new List<object>(ds[0]);
            if (ds.Length > 4)
            {
                var master = ds[3].Cast<dynamic>().GroupBy(x => x.RefName);
                foreach (var remoteData in master)
                {
                    SetRemoteSource(remoteData.ToList(), remoteData.Key, Header.FirstOrDefault(x => x.RefName == remoteData.Key));
                }
                SyncMasterData(ds[3], Header);
            }
            else
            {
                await LoadMasterData(rows);
            }
            SetRowData(rows);
            UpdatePagination(total, rows.Count);
            return rows;
        }

        private void MoveLeft(GridPolicy header, Event e)
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

        private void MoveRight(GridPolicy header, Event e)
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


        protected virtual void ChangeFieldOrder(GridPolicy header, Event e)
        {
            if (GuiInfo.CanCache || header.ShortDesc.IsNullOrWhiteSpace())
            {
                return;
            }

            var sortFields = GetSortedFields();
            if (!sortFields.ContainsKey(header.FieldName))
            {
                sortFields.Add(header.FieldName, false);
            }
            var target = e.Target.As<HTMLElement>();
            var th = Html.Take(target).Closest(ElementType.th).GetContext();
            var sortEle = th.QuerySelector(".fa");
            if (sortEle is null)
            {
                Html.Take(target).Icon("fa fa-sort-amount-up-alt").End.Render();
            }
            else if (sortEle.HasClass("fa-sort-amount-down-alt"))
            {
                sortFields.Remove(header.FieldName);
                sortEle.RemoveClass("fa-sort-amount-down-alt");
            }
            else if (sortEle.HasClass("fa-sort-amount-up-alt"))
            {
                sortFields[header.FieldName] = true;
                sortEle.ReplaceClass("fa-sort-amount-up-alt", "fa-sort-amount-down-alt");
            }
            var orderPart = string.Join(",", sortFields.Select(x => x.Key + (x.Value ? " desc" : "")));
            DataSourceFilter = OdataExt.ApplyClause(DataSourceFilter, orderPart, OdataExt.OrderByKeyword);
            UpdateView();
        }

        private Dictionary<string, bool> GetSortedFields()
        {
            Dictionary<string, bool> fieldSorts = new Dictionary<string, bool> { };
            var dataSource = DataSourceFilter;
            if (dataSource.IsNullOrWhiteSpace())
            {
                return fieldSorts;
            }

            dataSource = dataSource.Replace(new RegExp(@"\s+"), " ");
            var orderClause = OdataExt.GetClausePart(dataSource, OdataExt.OrderByKeyword);
            if (orderClause.IsNullOrWhiteSpace())
            {
                return fieldSorts;
            }

            fieldSorts = orderClause.Split(",").Select(x =>
            {
                if (x.IsNullOrWhiteSpace())
                {
                    return null;
                }

                var sortedField = x.Split(" ");
                if (sortedField.Length < 1)
                {
                    return null;
                }

                return new { Field = sortedField[0], Desc = sortedField.Length == 2 && sortedField[1].ToLower() == "desc" };
            }).Where(x => x != null).DistinctBy(x => x.Field).ToDictionary(x => x.Field, x => x.Desc);

            return fieldSorts;
        }

        public virtual void ToggleAll()
        {
            var anySelected = AllListViewItem.Any(x => x.Selected);
            if (anySelected)
            {
                ClearSelected();
            }
            else
            {
                RowAction(x =>
                {
                    if (x.EmptyRow)
                    {
                        return;
                    }

                    x.Selected = true;
                });
            }
            if (VirtualScroll)
            {
                Task.Run(async () =>
                {
                    if (!anySelected)
                    {
                        var data = CalcDatasourse(Paginator.Options.Total, 0, "false");
                        var selectedOdataIds = await new Client(GuiInfo.RefName, GuiInfo.Reference?.Namespace).GetList<object>($"{data}&$select=Id", true);
                        var selectedIds = selectedOdataIds.Value.Select(x => x[IdField]).Cast<int>().ToList();
                        SelectedIds = selectedIds.As<HashSet<int>>();
                    }
                });
            }
        }

        private void HeaderContextMenu(Event e, GridPolicy header)
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
                new ContextMenuItem { Icon = "fal fa-eye", Text = "Hiện tiêu đề", Click = ShowWidth, Parameter = new {header= header, events= e }},
                new ContextMenuItem { Icon = "fal fa-eye-slash", Text = "Ẩn tiêu đề", Click = HideWidth, Parameter = new {header= header, events= e }},
                new ContextMenuItem { Icon = header.Frozen ? "fal fa-snowflakes" : "fal fa-snowflake", Text = header.Frozen ? "Hủy định cột" : "Cố định cột", Click = FrozenColumn, Parameter = new {header= header, events= e }},
            };
            if (Client.SystemRole)
            {
                menu.MenuItems.AddRange(new List<ContextMenuItem>
                {
                    new ContextMenuItem { Icon = "fal fa-wrench", Text = "Tùy chọn cột dữ liệu", Click = editForm.HeaderProperties, Parameter = header },
                    new ContextMenuItem { Icon = "fal fa-clone", Text = "Clone cột", Click = CloneHeader, Parameter = header },
                    new ContextMenuItem { Icon = "fal fa-trash-alt", Text = "Xóa cột", Click = RemoveHeader, Parameter = header },
                    new ContextMenuItem { Icon = "fal fa-cog", Text = "Tùy chọn bảng dữ liệu", Click = editForm.ComponentProperties, Parameter = GuiInfo },
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
                awaiter1 = Window.SetTimeout(async () =>
                {
                    await UpdateUserSetting();
                }, 100);
            }
        }

        private void HideWidth(object arg)
        {
            var entity = arg["header"] as GridPolicy;
            var e = arg["events"] as Event;
            /*@
             e.target.firstChild.remove();
             e.target.style.minWidth = "";
             e.target.style.maxWidth = "";
             e.target.style.width = "";
             */
            Task.Run(async () => await UpdateUserSetting());
        }

        private void HideColumn(object arg)
        {
            var entity = arg["header"] as GridPolicy;
            var e = arg["events"] as Event;
            /*@
             var $table = $(e.target).closest('table');
             var $cell = $(e.target).closest('th,td');
             var cellIndex = $cell[0].cellIndex + 1;
             $table.find("tbody tr, thead tr").children(":nth-child("+cellIndex+")").hide();
             console.log($(e.target).attr("data-field"));
             */
            Task.Run(async () => await UpdateUserSettingColumn());
        }

        private async Task UpdateUserSetting()
        {
            _settings = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>(
                $"?$filter=UserId eq {Client.Token.UserId} and Name eq 'ListView-{GuiInfo.Id}'");
            var headerElement = HeaderSection.Children.Where(x => x.GuiInfo != null).ToList().ToDictionary(x => x.GuiInfo.Id);
            BasicHeader.ForEach(x =>
            {
                var match = headerElement.GetValueOrDefault(x.Id);
                if (match != null)
                {
                    x.Width = match.Element.OffsetWidth + "px";
                    x.MaxWidth = match.Element.OffsetWidth + "px";
                    x.MinWidth = match.Element.OffsetWidth + "px";
                }
            });
            var column = BasicHeader;
            var value = JsonConvert.SerializeObject(column);
            if (_settings is null)
            {
                _settings = new UserSetting
                {
                    UserId = Client.Token.UserId,
                    Name = "ListView-" + GuiInfo.Id,
                    Value = value
                };
                _settings = await new Client(nameof(UserSetting)).CreateAsync<UserSetting>(_settings);
            }
            else
            {
                _settings.Value = value;
                _settings.Name = "ListView-" + GuiInfo.Id;
                _settings = await new Client(nameof(UserSetting)).UpdateAsync<UserSetting>(_settings);
            }
        }

        private async Task UpdateUserSettingColumn()
        {
            _settings = await new Client(nameof(UserSetting)).FirstOrDefaultAsync<UserSetting>(
                $"?$filter=UserId eq {Client.Token.UserId} and Name eq 'ListView-{GuiInfo.Id}'");
            BasicHeader.ForEach(x =>
            {
                x.Active = false;
            });
            var column = BasicHeader;
            var value = JsonConvert.SerializeObject(column);
            if (_settings is null)
            {
                _settings = new UserSetting
                {
                    UserId = Client.Token.UserId,
                    Name = "ListView-" + GuiInfo.Id,
                    Value = value
                };
                _settings = await new Client(nameof(UserSetting)).CreateAsync<UserSetting>(_settings);
            }
            else
            {
                _settings.Value = value;
                _settings.Name = "ListView-" + GuiInfo.Id;
                _settings = await new Client(nameof(UserSetting)).UpdateAsync<UserSetting>(_settings);
            }
        }

        private void ShowWidth(object arg)
        {
            var entity = arg["header"] as GridPolicy;
            var e = arg["events"] as Event;
            /*@
             if (!e.target.firstChild.length) {
                e.target.prepend(entity.ShortDesc)
             }
             e.target.style.minWidth = "";
             e.target.style.maxWidth = "";
             e.target.style.width = "";
             */
            Task.Run(async () => await UpdateUserSetting());
        }

        private void FrozenColumn(object arg)
        {
            var entity = arg["header"] as GridPolicy;
            BasicHeader.FirstOrDefault(x => x.Id == entity.Id).Frozen = !entity.Frozen;
            Task.Run(async () =>
            {
                await UpdateUserSetting();
                Window.Location.Reload();
            });
        }

        public void CloneHeader(object arg)
        {
            var entity = arg as GridPolicy;
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn clone cột này không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var ids = new List<int> { entity.Id };
                var client = new Client(nameof(GridPolicy));
                entity.Id = 0;
                var success = await client.CreateAsync<GridPolicy>(entity);
                if (success != null)
                {
                    Header.Add(success);
                    Header = Header.OrderByDescending(x => x.Frozen).ThenByDescending(header => header.ComponentType == "Button").ThenBy(x => x.Order).ToList();
                    Rerender();
                    Toast.Success("Clone thàng công");
                }
                else
                {
                    Toast.Warning("Clone error");
                }
            };
        }

        public void RemoveHeader(object arg)
        {
            var entity = arg as GridPolicy;
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn xóa cột này không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var ids = new List<int> { entity.Id };
                var success = await new Client(nameof(GridPolicy)).HardDeleteAsync(ids);
                if (success)
                {
                    Header.Remove(entity);
                    Rerender();
                    Toast.Success("delete success");
                }
                else
                {
                    Toast.Warning("delete error");
                }
            };
        }

        public override async Task AddOrUpdateRows(IEnumerable<object> rows)
        {
            await base.AddOrUpdateRows(rows);
        }

        public override void RemoveRowById(int id)
        {
            base.RemoveRowById(id);
            RenderIndex();
        }

        public override void RemoveRow(object row)
        {
            base.RemoveRow(row);
            RenderIndex();
        }

        public override async Task<IEnumerable<object>> HardDeleteConfirmed()
        {
            var res = await base.HardDeleteConfirmed();
            if (GuiInfo.ComponentType == nameof(VirtualGrid) && res != null)
            {
                var ids = res.Select(x => int.Parse(x[IdField].ToString())).ToList();
                CacheData.RemoveAll(x => ids.Contains(int.Parse(x[IdField].ToString())));
            }
            RenderIndex();
            if (GuiInfo.IsSumary)
            {
                AddSummaries();
            }
            return res;
        }

        protected int _renderIndexAwaiter;
        internal int _lastScrollTop;

        protected virtual void RenderIndex(int? skip = null)
        {
            if (skip is null)
            {
                skip = Paginator?.Options?.StartIndex ?? 0;
            }
            if (MainSection.Children.Nothing())
            {
                return;
            }
            MainSection.Children.Cast<ListViewItem>().ForEach((row, rowIndex) =>
            {
                if (row.Children.Nothing() || row.FirstChild is null || row.FirstChild.Element is null)
                {
                    return;
                }
                var previous = row.FirstChild.Element.Closest("td").PreviousElementSibling;
                if (previous is null)
                {
                    return;
                }
                var index = skip + rowIndex;
                previous.InnerHTML = index.ToString();
                row.Selected = SelectedIds.Contains(row.Entity[IdField].As<int>());
                row.RowNo = index.Value;
            });
        }

        public override void UpdateView(bool force = false, bool? dirty = null, params string[] componentNames)
        {
            if (!Editable && !GuiInfo.CanCache)
            {
                if (force)
                {
                    DisposeNoRecord();
                    Task.Run(async () => await ListViewSearch.RefershListView());
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
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.BeforeCreated, rowData);
                rowSection.EmptyRow = false;
                MoveEmptyRow(rowSection);
                var headers = Header.Where(y => y.Editable).ToList();
                var currentComponent = headers.FirstOrDefault(y => y.FieldName == component?.GuiInfo.FieldName);
                var index = headers.IndexOf(currentComponent);
                if (headers.Count > index + 1)
                {
                    var nextGrid = headers[index + 1];
                    var nextComponent = rowSection.Children.Where(y => y?.GuiInfo.FieldName == nextGrid.FieldName).FirstOrDefault();
                    nextComponent.Focus();
                }
                EmptyRowSection.Children.Clear();
                AddNewEmptyRow();
                Entity.SetComplexPropValue(GuiInfo.FieldName, RowData.Data);
                await this.DispatchCustomEventAsync(GuiInfo.Events, CustomEventType.AfterCreated, rowData);
            }
            AddSummaries();
            PopulateFields();
            RenderIndex();
            await this.DispatchEventToHandlerAsync(GuiInfo.Events, EventType.Change, rowData);
        }
    }
}