using Bridge.Html5;
using Core.Models;
using Core.Clients;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Retyped;

namespace Core.Components
{
    public class ListViewSearchVM
    {
        public string SearchTerm { get; set; }
        public string FullTextSearch { get; set; }
        public string ScanTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public int? DateTimeField { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ListViewSearch : EditableComponent
    {
        private HTMLInputElement _uploader;
        private HTMLInputElement _fullTextSearch;
        public ListViewSearchVM EntityVM => Entity as ListViewSearchVM;
        public string DateTimeField { get; set; }
        private ListView ParentListView
        {
            get
            {
                if (parentListView != null)
                {
                    return parentListView;
                }
                return Parent as ListView;
            }
            set => parentListView = value;
        }

        private GridView ParentGridView
        {
            get
            {
                if (parentGridView != null)
                {
                    return parentGridView;
                }
                return Parent as GridView;
            }
            set => parentGridView = value;
        }
        public GridPolicy[] BasicSearch;
        private ListView parentListView;
        private GridView parentGridView;

        public ListViewSearch(Component ui) : base(ui)
        {
            PopulateDirty = false;
            AlwaysValid = true;
            GuiInfo = ui ?? throw new ArgumentNullException(nameof(ui));
            DateTimeField = ui.DateTimeField ?? nameof(Component.InsertedDate);
            Entity = new ListViewSearchVM();
        }

        private void ParentListView_HeaderLoaded(List<GridPolicy> basicSearchHeader)
        {
            BasicSearch = basicSearchHeader.Where(x => x.BasicSearch).OrderByDescending(x => x.Order).ToArray();
            if (BasicSearch.Nothing())
            {
                return;
            }
            Html.Take(Element);
            var components = BasicSearch.Select(header =>
            {
                var com = header.MapToComponent();
                var componentType = com.ComponentType.TryParse<ComponentTypeTypeEnum>();
                com.ShowLabel = false;
                com.PlainText = header.ShortDesc;
                com.Visibility = true;
                com.Column = 1;
                ParentListView.AdvSearchVM.Conditions.Add(new FieldCondition
                {
                    FieldId = com.Id,
                    CompareOperatorId = (AdvSearchOperation)Components.AdvancedSearch.OperatorFactory(componentType ?? ComponentTypeTypeEnum.Textbox).FirstOrDefault().Id,
                    LogicOperatorId = LogicOperation.And,
                    Field = header,
                });
                return com;
            }).ToArray();
            var sectionInfo = new ComponentGroup
            {
                Component = components,
                Responsive = true,
                Column = components.Length,
                ClassName = "wrapper"
            };
            var _basicSearchGroup = Section.RenderSection(this, sectionInfo);
            _basicSearchGroup.Children.ForEach(child =>
            {
                child.UserInput += (changes) =>
                {
                    var condition = ParentListView.AdvSearchVM.Conditions.FirstOrDefault(x => x.FieldId == child.GuiInfo.Id);
                    condition.Value = child.GetValue(simple: true)?.ToString();
                };
            });
            while (_basicSearchGroup.Element.Children.Length > 0)
            {
                Element.InsertBefore(_basicSearchGroup.Element.FirstChild, Element.FirstChild);
            }
        }

        public override void Render()
        {
            ParentListView = Parent as ListView;
            ParentListView.HeaderLoaded += ParentListView_HeaderLoaded;
            if (!GuiInfo.CanSearch)
            {
                return;
            }

            Html.Take(Parent.Element.FirstElementChild).TabIndex(-1).AsyncEvent(EventType.KeyPress, EnterSearch);
            Element = Html.Context;
            if (GuiInfo.ComponentType == nameof(GridView))
            {
                var txtSearch = new Textbox(new Component
                {
                    FieldName = nameof(ListViewSearchVM.SearchTerm),
                    Visibility = true,
                    Label = "Tìm kiếm",
                    PlainText = "Tìm kiếm",
                    ShowLabel = false,
                })
                {
                    ParentElement = Element
                };
                txtSearch.UserInput = null;
                AddChild(txtSearch);
            }
            var txtFullTextSearch = new Textbox(new Component
            {
                FieldName = nameof(ListViewSearchVM.FullTextSearch),
                Visibility = true,
                Label = "Lọc hiển thị",
                PlainText = "Lọc hiển thị",
                ShowLabel = false,
            })
            {
                ParentElement = Element
            };
            txtFullTextSearch.UserInput = null;
            AddChild(txtFullTextSearch);
            _fullTextSearch = txtFullTextSearch.Input;
            _fullTextSearch.AddEventListener(EventType.Input, ParentGridView.FullTextSearch);
            if (GuiInfo.UpperCase)
            {
                var txtScan = new Textbox(new Component
                {
                    FieldName = nameof(ListViewSearchVM.ScanTerm),
                    Visibility = true,
                    Label = "Scan",
                    PlainText = "Scan",
                    ShowLabel = false,
                    Focus = true,
                    Events = "{'input':'ScanGridView'}"
                })
                {
                    ParentElement = Element
                };
                txtScan.UserInput = null;
                AddChild(txtScan);
            }
            var startDate = new Datepicker(new Component
            {
                FieldName = nameof(ListViewSearchVM.StartDate),
                Visibility = true,
                Label = "Từ ngày",
                PlainText = "Từ ngày",
                ShowLabel = false,
            })
            {
                ParentElement = Element
            };
            startDate.UserInput = null;
            AddChild(startDate);
            var endDate = new Datepicker(new Component
            {
                FieldName = nameof(ListViewSearchVM.EndDate),
                Visibility = true,
                Label = "Đến ngày",
                PlainText = "Đến ngày",
                ShowLabel = false,
            })
            {
                ParentElement = Element
            };
            endDate.UserInput = null;
            AddChild(endDate);
            if (ParentListView.GuiInfo.ShowDatetimeField != null && (bool)ParentListView.GuiInfo.ShowDatetimeField)
            {
                var dateType = new SearchEntry(new Component
                {
                    FieldName = nameof(Component.DateTimeField),
                    PlainText = "Loại ngày",
                    FormatData = "{ShortDesc}",
                    DataSourceFilter = $"?$filter=Active eq true and ComponentType eq '{nameof(Datepicker)}' and FeatureId eq {EditForm.Feature.Id}",
                    ShowLabel = false,
                    ReferenceId = Utils.GetEntity(nameof(GridPolicy)).Id,
                    RefName = nameof(GridPolicy),
                    Reference = new Entity { Name = nameof(GridPolicy) },
                })
                {
                    ParentElement = Element
                };
                dateType.UserInput = null;
                AddChild(dateType);
            }
            Html.Take(Element).Div.ClassName("searching-block")
                .Button("Tìm kiếm", className: "button secondary small btn-toolbar", icon: "fa fa-search")
                    .Event(EventType.Click, async () =>
                    {
                        ParentListView.ClearSelected();
                        await ParentListView.ActionFilter();
                    }).End
                .Button(className: "button secondary small btn-toolbar right", icon: "fa fa-cog")
                    .Title("Nâng cao")
                    .Icon("fa fa-chevron-down").End
                    .Event(EventType.Click, AdvancedOptions).End
                .Button(className: "btnSearch button secondary small btn-toolbar right", icon: "fa fa-undo")
                    .Title("Làm mới")
                    .Event(EventType.Click, async () => await RefershListView()).End
                .Button(className: "btn btn-secondary btn-sm", icon: "fal fa-compress-wide")
                    .Title("Phóng to")
                    .Event(EventType.Click, FullScreen).End
                    .Render();
            if (GuiInfo.ShowHotKey)
            {
                Html.Take(Element).Div.ClassName("hotkey-block")
                .Button("F1", className: "btn btn-light btn-sm").Event(EventType.Click, ParentGridView.ToggleAll)
                    .Attr("title", "Bỏ chọn tất cả").End
                .Button("F2", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                {
                    var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                    ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F2);
                })
                    .Attr("title", "Lọc loại trừ").End
                .Button("F3", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                {
                    var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                    ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F3);
                })
                    .Attr("title", "Cộng tổng dòng được chọn").End
                .Button("F4", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                {
                    var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                    ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F4);
                })
                    .Attr("title", "Lọc tiếp theo các phép tính (Chứa: Bằng; Lớn hơn; Nhỏ hơn; Lớn hơn hoặc bằng;...)").End
                 .Button("F6", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                 {
                     ParentGridView.HotKeyF6Handler(e, KeyCodeEnum.F6);
                 })
                    .Attr("title", "Quay lại lần lọc trước").End
                .Button("F8", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                {
                    var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                    ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F8);
                })
                    .Attr("title", "Xóa/ Vô hiệu hóa dòng hiện thời hoặc các dòng đánh dấu").End
                .Button("F9", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                {
                    var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                    ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F9);
                })
                    .Attr("title", "Lọc tại chỗ theo giá trị ô hiện thời").End
                .Button("F10", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                {
                    var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                    ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F10);
                })
                    .Attr("title", "Gộp theo cột hiện thời(thống kê lại số nội dung trong cột)").End
                .Button("F11", className: "btn btn-light btn-sm").Event(EventType.Click, (e) =>
                    {
                        var com = ParentListView.LastListViewItem.Children.FirstOrDefault(x => x.GuiInfo.Id == ParentGridView.LastComponentFocus.Id);
                        ParentGridView.ActionKeyHandler(e, ParentGridView.LastComponentFocus, ParentGridView.LastListViewItem, com, com.Element.Closest(Core.MVVM.ElementType.td.ToString()), KeyCodeEnum.F11);
                    })
                    .Attr("title", "Sắp xếp thứ tự tăng dần, giảm dần").End.Render();
            }
        }

        private void FullScreen()
        {
            var elem = ParentListView.Element;
            /*@
             if (elem.requestFullscreen) {
                    elem.requestFullscreen();
                  } else if (elem.webkitRequestFullscreen) { 
                            elem.webkitRequestFullscreen();
                        } else if (elem.msRequestFullscreen) {
                    elem.msRequestFullscreen();
                  }
             */
        }

        private async Task EnterSearch(Event e)
        {
            if (e.KeyCode() != 13)
            {
                return;
            }

            await ParentListView.ApplyFilter();
        }

        private async Task UploadCsv(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var fileName = files.FirstOrDefault().Name;
            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            var parentForm = this.FindClosest<EditForm>();
            var response = await parentForm.Client.SubmitAsync<Blob>(new XHRWrapper
            {
                FormData = formData,
                Url = "importCsv",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("csv")
            });
            ComponentExt.DownloadFile(fileName, response);
            Toast.Success("Import excel thành công");
            _uploader.Value = string.Empty;
        }

        private void AdvancedOptions(Event e)
        {
            var buttonRect = e.Target.As<HTMLElement>().GetBoundingClientRect();
            var show = LocalStorage.GetItem<bool?>("Show" + GuiInfo.Id) is null ? false : LocalStorage.GetItem<bool?>("Show" + GuiInfo.Id);
            var ctxMenu = ContextMenu.Instance;
            ctxMenu.Top = buttonRect.Bottom;
            ctxMenu.Left = buttonRect.Left;
            ctxMenu.MenuItems = new List<ContextMenuItem>
            {
                    new ContextMenuItem { Icon = "fa fa-search-plus mr-1", Text = "Nâng cao", Click = AdvancedSearch },
                    new ContextMenuItem { Icon = "fa fa-search mr-1", Text = "Lọc dữ liệu đã chọn", Click = FilterSelected },
                    new ContextMenuItem { Icon = "fa fa-download mr-1", Text = "Excel toàn bộ", Click = ExportAllData },
                    new ContextMenuItem { Icon = "fa fal fa-ballot-check mr-1", Text = "Excel đã chọn", Click = ExportSelectedData },
                    new ContextMenuItem { Icon = "fa fa-download mr-1", Text = "Excel tùy chọn", Click = ExportCustomData },
            };
            ctxMenu.Render();
            Html.Take(Element).Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Type("file").Id($"id_{GetHashCode()}").Attr("name", "files").Attr("accept", ".csv");
            _uploader = Html.Context as HTMLInputElement;
            _uploader.AddEventListener(EventType.Change.ToString(), async (ev) => await UploadCsv(ev));
            ctxMenu.Element.Children.FirstOrDefault()?.AppendChild(_uploader.ParentElement);
        }

        private void FilterSelected(object arg)
        {
            var selectedIds = ParentListView.SelectedIds;
            if (selectedIds.Nothing())
            {
                Toast.Warning("Vui lòng chọn dòng cần lọc");
                return;
            }
            Task.Run(async () =>
            {
                if (ParentListView.CellSelected.Any(x => x.FieldName == IdField))
                {
                    ParentListView.CellSelected.FirstOrDefault(x => x.FieldName == IdField).Value = selectedIds.Combine();
                    ParentListView.CellSelected.FirstOrDefault(x => x.FieldName == IdField).ValueText = selectedIds.Combine();
                }
                else
                {
                    ParentListView.CellSelected.Add(new CellSelected()
                    {
                        FieldName = IdField,
                        FieldText = "Mã",
                        ComponentType = "Input",
                        Value = selectedIds.Combine(),
                        ValueText = selectedIds.Combine(),
                        Operator = "in",
                        OperatorText = "Chứa",
                        Logic = LogicOperation.And,
                    });
                    ParentGridView._summarys.Add(new HTMLElement());
                }
                await ParentListView.ActionFilter();
            });
        }

        private void LiteGridView(object arg)
        {
            var show = LocalStorage.GetItem<bool?>("Show" + GuiInfo.Id) is null ? false : LocalStorage.GetItem<bool?>("Show" + GuiInfo.Id);
            LocalStorage.SetItem<bool?>("Show" + GuiInfo.Id, !show.Value);
            Window.Location.Reload();
        }

        private void ExportCustomData(object arg)
        {
            Task.Run(async () =>
            {
                await TabEditor.OpenPopup("Export CustomData", () =>
                {
                    var editor = new ExportCustomData(ParentListView)
                    {
                        ParentListView = Parent as ListView,
                        ParentElement = TabEditor.Element
                    };
                    return editor;
                });
            });
        }

        private void ShowHidden(Event e)
        {
            var buttonRect = e.Target.As<HTMLElement>().GetBoundingClientRect();
            var ctxMenu = ContextMenu.Instance;
            ctxMenu.Top = buttonRect.Bottom;
            ctxMenu.Left = buttonRect.Left;
            ctxMenu.MenuItems = new List<ContextMenuItem>
            {
                    new ContextMenuItem { Icon = "fa fa-cloud-upload-alt", Text = "Nhập excel", Click = OpenExcelFileDialog },
                    new ContextMenuItem { Icon = "fa fa-download", Text = "Xuất hiển thị", Click = ExportDisplay },
                    new ContextMenuItem { Icon = "fa fa-download", Text = "Xuất toàn bộ", Click = ExportAllData },
                    new ContextMenuItem { Icon = "fa fa-download", Text = "Xuất tùy chọn", Click = ExportAllData },
            };
            ctxMenu.Render();
            Html.Take(Element).Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Type("file").Id($"id_{GetHashCode()}").Attr("name", "files").Attr("accept", ".csv");
            _uploader = Html.Context as HTMLInputElement;
            _uploader.AddEventListener(EventType.Change.ToString(), async (ev) => await UploadCsv(ev));
            ctxMenu.Element.Children.FirstOrDefault()?.AppendChild(_uploader.ParentElement);
        }

        public void AdvancedSearch(object arg)
        {
            Task.Run(async () =>
            {
                await TabEditor.OpenPopup("AdvancedSearch", () =>
                {
                    var editor = new AdvancedSearch(ParentListView)
                    {
                        ParentListView = Parent as ListView,
                        ParentElement = TabEditor.Element
                    };
                    return editor;
                });
            });
        }

        private void ExportAllData(object arg)
        {
            Task.Run(async () =>
            {
                Toast.Success("Đang xuất excel");
                var orderbyList = ParentListView.AdvSearchVM.OrderBy.Select(orderby => $"[{ParentListView.GuiInfo.RefName}].{orderby.Field.FieldName} {orderby.OrderbyOptionId.ToString().ToLowerCase()}");
                var finalFilter = string.Empty;
                if (orderbyList.HasElement())
                {
                    finalFilter = orderbyList.Combine();
                }
                if (finalFilter.IsNullOrWhiteSpace())
                {
                    finalFilter = OdataExt.GetClausePart(ParentListView.FormattedDataSource, OdataExt.OrderByKeyword);
                    if (finalFilter.Contains(","))
                    {
                        var k = finalFilter.Split(",").ToList();
                        finalFilter = k.Select(x => $"[{ParentListView.GuiInfo.RefName}].{x}").Combine();
                    }
                    else
                    {
                        finalFilter = $"[{ParentListView.GuiInfo.RefName}].{finalFilter}";
                    }
                }
                var filter = ParentListView.Wheres.Where(x => !x.Group).Select(x => x.FieldName).Combine(" and ");
                var filter1 = ParentListView.Wheres.Where(x => x.Group).Select(x => x.FieldName).Combine(" or ");
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
                var pre = ParentListView.GuiInfo.PreQuery;
                if (pre != null && Utils.IsFunction(pre, out Function fn))
                {
                    pre = fn.Call(this, this, EditForm).ToString();
                }
                var path = await new Client(GuiInfo.RefName).GetAsync<string>($"/ExportExcel?componentId={ParentListView.GuiInfo.Id}" +
                    $"&sql={ParentListView.Sql}" +
                    $"&showNull={GuiInfo.ShowNull ?? false}" +
                    $"&where={stringWh} {(pre.IsNullOrWhiteSpace() ? "" : $"{(wh.Any() ? " and " : "")} {pre}")}" +
                    $"&custom=false&featureId={EditForm.Feature.Id}&orderby={finalFilter}");
                Client.Download($"/excel/Download/{path}");
                Toast.Success("Xuất file thành công");
            });
        }

        private void ExportSelectedData(object arg)
        {
            var selectedIds = ParentListView.SelectedIds;
            if (selectedIds.Nothing())
            {
                Toast.Warning("Vui lòng chọn dòng muốn xuất");
                return;
            }

            Task.Run(async () =>
            {
                Toast.Success("Đang xuất excel");
                var orderbyList = ParentListView.AdvSearchVM.OrderBy.Select(orderby => $"[{ParentListView.GuiInfo.RefName}].{orderby.Field.FieldName} {orderby.OrderbyOptionId.ToString().ToLowerCase()}");
                var finalFilter = string.Empty;
                if (orderbyList.HasElement())
                {
                    finalFilter = orderbyList.Combine();
                }
                if (finalFilter.IsNullOrWhiteSpace())
                {
                    finalFilter = OdataExt.GetClausePart(ParentListView.FormattedDataSource, OdataExt.OrderByKeyword);
                    if (finalFilter.Contains(","))
                    {
                        var k = finalFilter.Split(",").ToList();
                        finalFilter = k.Select(x => $"[{ParentListView.GuiInfo.RefName}].{x}").Combine();
                    }
                    else
                    {
                        finalFilter = $"[{ParentListView.GuiInfo.RefName}].{finalFilter}";
                    }
                }
                var filter = ParentListView.Wheres.Where(x => !x.Group).Select(x => x.FieldName).Combine(" and ");
                var filter1 = ParentListView.Wheres.Where(x => x.Group).Select(x => x.FieldName).Combine(" or ");
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
                var pre = ParentListView.GuiInfo.PreQuery;
                if (pre != null && Utils.IsFunction(pre, out Function fn))
                {
                    pre = fn.Call(this, this, EditForm).ToString();
                }
                var path = await new Client(GuiInfo.RefName).GetAsync<string>($"/ExportExcel?componentId={ParentListView.GuiInfo.Id}" +
                    $"&sql={ParentListView.Sql}" +
                    $"&showNull={GuiInfo.ShowNull ?? false}" +
                    $"&where={stringWh} {(pre.IsNullOrWhiteSpace() ? "" : $"{(wh.Any() ? " and " : "")} {pre}")} {$" and [{ParentListView.GuiInfo.RefName}].Id in ({selectedIds.Combine()})"}" +
                    $"&custom=false&featureId={EditForm.Feature.Id}&orderby={finalFilter}");
                Client.Download($"/excel/Download/{path}");
                Toast.Success("Xuất file thành công");
            });
        }

        private void ExportDisplay(object arg)
        {
            Task.Run(async () => await ExportData(paging: true));
        }

        private async Task ExportData(bool paging)
        {
            var url = ParentListView.CalcFilterQuery(false);
            var options = ParentListView.Paginator.Options;
            if (paging)
            {
                url += $"&$skip={options.PageIndex * options.PageSize}&$top={options.PageSize}";
            }
            Spinner.AppendTo(ParentElement, autoHide: true);
            var localData = await new Client(GuiInfo.Reference.Name).GetList<object>(url);
            var copyHeaders = ParentListView.Header
                .Where(x => x.ComponentType != nameof(Button))
                .Select(x =>
                {
                    var header = new GridPolicy();
                    header.CopyPropFrom(x);
                    header.Editable = false;
                    if (x.ComponentType == nameof(Checkbox))
                    {
                        header.SimpleText = true;
                    }
                    header.ComponentType = "Label";
                    return header;
                }).ToList();
            var copyGui = new Component();
            copyGui.CopyPropFrom(ParentListView.GuiInfo);
            copyGui.CanSearch = false;
            copyGui.LocalData = localData.Value;
            copyGui.LocalHeader = copyHeaders;
            var copyListView = new GridView(copyGui) { ParentElement = ParentListView.ParentElement };
            await copyListView.LoadMasterData(copyGui.LocalData);
            ParentListView.Parent.AddChild(copyListView);
            copyListView.Element.Style.Display = Display.None;
            copyListView.DOMContentLoaded += () =>
            {
                copyListView.Element.QuerySelector("table").QuerySelectorAll("td").ForEach(x =>
                {
                    /*@
                     x.style="border:1px solid;white-space: nowrap;font-family: 'times new roman', times, serif;";
                     */
                });
                copyListView.Element.QuerySelector("table").QuerySelectorAll("th").ForEach(x =>
                {
                    /*@
                     x.style="border:1px solid;white-space: nowrap;font-family: 'times new roman', times, serif;";
                     */
                });
                ExcelExt.ExportTableToExcel(null, GuiInfo.FieldName ?? "Export", copyListView.Element.QuerySelector("table") as HTMLElement);
                copyListView.Dispose();
            };
        }

        private void OpenExcelFileDialog(object arg)
        {
            _uploader.Click();
        }

        public string CalcFilterQuery(string prefix)
        {
            if (EntityVM.DateTimeField != null)
            {
                DateTimeField = ParentListView.Header.FirstOrDefault(x => x.Id == EntityVM.DateTimeField).FieldName;
            }
            var headers = ParentListView.Header;
            var searchTerm = EntityVM.SearchTerm?.Trim().EncodeSpecialChar() ?? string.Empty;
            var finalFilter = ComponentExt.FilterById(searchTerm, headers);
            if (finalFilter.IsNullOrEmpty())
            {
                headers.ForEach(x => x.FilterEq = GuiInfo.FilterEq);
                var operators = headers.Select(x => x.MapToFilterOperator(searchTerm)).Where(x => x.HasAnyChar());
                finalFilter = string.Join(" or ", operators);
            }
            var basicsAddDate = ParentGridView.BasicHeader.Where(x => x.AddDate).Select(x => x.Id).ToArray();
            var parentGrid = basicsAddDate.Any() && ParentGridView.AdvSearchVM.Conditions.Any(x => basicsAddDate.Contains(x.FieldId) && !x.Value.IsNullOrWhiteSpace());
            if (!parentGrid && EntityVM.StartDate != null)
            {
                if (finalFilter.HasAnyChar())
                {
                    finalFilter += " and ";
                }
                var oldStartDate = ParentListView.Wheres.FirstOrDefault(x => x.FieldName.Contains($"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] >="));
                if (oldStartDate is null)
                {
                    ParentListView.Wheres.Add(new Where()
                    {
                        FieldName = $"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] >= '{EntityVM.StartDate.Value.ToString("yyyy-MM-dd")}'",
                        Group = false
                    });
                }
                else
                {
                    oldStartDate.FieldName = $"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] >= '{EntityVM.StartDate.Value.ToString("yyyy-MM-dd")}'";
                }
                EntityVM.StartDate = EntityVM.StartDate.Value.Date;
                finalFilter += $"cast({DateTimeField},Edm.DateTimeOffset) ge cast({EntityVM.StartDate.Value.ToISOFormat()},Edm.DateTimeOffset)";
                LocalStorage.SetItem("FromDate" + ParentListView.GuiInfo.Id, EntityVM.StartDate.Value.ToString("MM/dd/yyyy"));
            }
            else if (EntityVM.StartDate is null)
            {
                var check = ParentListView.Wheres.FirstOrDefault(x => x.FieldName.Contains($"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] >="));
                if (ParentListView.Wheres.Any() && check != null)
                {
                    ParentListView.Wheres.Remove(check);
                }
                LocalStorage.RemoveItem("FromDate" + ParentListView.GuiInfo.Id);
            }
            if (!parentGrid && EntityVM.EndDate != null)
            {
                if (finalFilter.HasAnyChar())
                {
                    finalFilter += " and ";
                }
                var endDate = EntityVM.EndDate.Value.Date.AddDays(1);
                var oldEndDate = ParentListView.Wheres.FirstOrDefault(x => x.FieldName.Contains($"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] <"));
                if (oldEndDate is null)
                {
                    ParentListView.Wheres.Add(new Where()
                    {
                        FieldName = $"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] < '{endDate.ToString("yyyy-MM-dd")}'",
                        Group = false
                    });
                }
                else
                {
                    oldEndDate.FieldName = $"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] < '{endDate.ToString("yyyy-MM-dd")}'";
                }
                finalFilter += $"cast({DateTimeField},Edm.DateTimeOffset) lt cast({endDate.ToISOFormat()},Edm.DateTimeOffset)";
                LocalStorage.SetItem("ToDate" + ParentListView.GuiInfo.Id, EntityVM.EndDate.Value.ToString("MM/dd/yyyy"));
            }
            else if (EntityVM.EndDate is null)
            {
                var check1 = ParentListView.Wheres.FirstOrDefault(x => x.FieldName.Contains($"[{ParentListView.GuiInfo.RefName}].[{DateTimeField}] <"));
                if (ParentListView.Wheres.Any() && check1 != null)
                {
                    ParentListView.Wheres.Remove(check1);
                }
                LocalStorage.RemoveItem("ToDate" + ParentListView.GuiInfo.Id);
            }
            if ((EntityVM.EndDate != null || EntityVM.StartDate != null) && ParentListView.GuiInfo.ShowNull != null && (bool)ParentListView.GuiInfo.ShowNull)
            {
                finalFilter += $" or {DateTimeField} eq null";
            }
            if (finalFilter.IsNullOrWhiteSpace())
            {
                return ApplyOrder(prefix);
            }
            var filterPart = OdataExt.GetClausePart(prefix, OdataExt.FilterKeyword);
            finalFilter = OdataExt.AppendClause(prefix, filterPart.IsNullOrWhiteSpace() ? finalFilter : $" and ({finalFilter})");
            finalFilter = ApplyOrder(finalFilter);
            return finalFilter;
        }

        private string ApplyOrder(string finalFilter)
        {
            var orderbyList = ParentListView.AdvSearchVM.OrderBy.Select(orderby => $"{orderby.Field.FieldName} {orderby.OrderbyOptionId.ToString().ToLowerCase()}");
            if (orderbyList.HasElement())
            {
                finalFilter = OdataExt.ApplyClause(finalFilter, orderbyList.Combine(), OdataExt.OrderByKeyword);
            }

            return finalFilter;
        }

        public async Task RefershListView()
        {
            EntityVM.SearchTerm = string.Empty;
            EntityVM.StartDate = null;
            EntityVM.EndDate = null;
            UpdateView();
            if (!(Parent is ListView listView))
            {
                return;
            }
            listView.DataSourceFilter = listView.GuiInfo.DataSourceFilter;
            listView.ClearSelected();
            listView.CellSelected.Clear();
            listView.AdvSearchVM.Conditions.Clear();
            listView.Wheres.Clear();
            await listView.ApplyFilter();
        }

        public override bool Disabled { get => false; set => _disabled = false; }
    }
}
