using Bridge.Utils;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Manage
{
    public class ReturnPlanListBL : TransportationListBL
    {
        public bool checkView = false;
        public ReturnPlanListBL()
        {
            Name = "ReturnPlan List";
        }

        public async Task CheckQuotationExpense(Transportation transportation, MasterData masterData)
        {
            if (transportation.ContainerTypeId is null || transportation.BrandShipId is null)
            {
                return;
            }
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Transportation));
            var listViewItem = gridView.GetListViewItems(transportation).FirstOrDefault();
            if (masterData is null)
            {
                var confirm = new ConfirmDialog
                {
                    Content = "Bạn có muốn xóa phí không?"
                };
                confirm.Render();
                confirm.YesConfirmed += () =>
                {
                    transportation.ReturnVs = 0;
                    listViewItem.UpdateView();
                    var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnVs)).ToList();
                    updated.ForEach(x => x.Dirty = true);
                };
            }
            else
            {
                var quotationExpense = await new Client(nameof(QuotationExpense)).FirstOrDefaultAsync<QuotationExpense>($"?$filter=Active eq true and BrandShipId eq {transportation.BrandShipId} and ExpenseTypeId eq {masterData.Id} and BranchId eq {transportation.ExportListId}");
                if (quotationExpense is null)
                {
                    transportation.ReturnVs = 0;
                }
                else
                {
                    if (transportation.Cont20 > 0)
                    {
                        transportation.ReturnVs = quotationExpense.VS20UnitPrice;
                    }
                    else if (transportation.Cont40 > 0)
                    {
                        transportation.ReturnVs = quotationExpense.VS40UnitPrice;
                    }
                }
                listViewItem.UpdateView();
                var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnVs)).ToList();
                updated.ForEach(x => x.Dirty = true);
            }
        }

        public override void UpdateQuotationRegion(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));
            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                var received = selected.Entity["Return"];
                if (coords.ReturnId is null || (received is null) || (received != null && received["RegionId"] is null))
                {
                    Toast.Warning("Vui lòng nhập nhà xe hoặc chọn khu vực cho địa chỉ");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7593 " +
                    $"and BossId eq null " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and RegionId eq {received["RegionId"]} " +
                    $"and LocationId eq null " +
                    $"and StartDate le {coords.ReturnDate.Value.ToOdataFormat()} " +
                    $"and PackingId eq {coords.ReturnVendorId}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7593,
                        BossId = null,
                        RegionId = int.Parse(received["RegionId"].ToString()),
                        ContainerTypeId = coords.ContainerTypeId,
                        LocationId = null,
                        StartDate = coords.ReturnDate,
                        PackingId = coords.ReturnVendorId
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Region Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationRegionEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá khu vực";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public override void UpdateQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật giá!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7593 " +
                    $"and BossId eq {coords.BossId} and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and LocationId eq {coords.ReturnId} and StartDate le {coords.ReturnDate.Value.ToOdataFormat()} and PackingId eq {coords.ReturnVendorId}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7593,
                        BossId = coords.BossId,
                        ContainerTypeId = coords.ContainerTypeId,
                        LocationId = coords.ReturnId,
                        StartDate = coords.ReturnDate,
                        PackingId = coords.ReturnVendorId
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá trả hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void CheckStatusQuotationReturn()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                var menus = new List<ContextMenuItem>();
                menus.Clear();
                menus.Add(new ContextMenuItem
                {
                    Icon = "fas fa-pen",
                    Text = "Ghi chú",
                    MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Text = "Yêu cầu trả vỏ", Click = NoteFreeText, Parameter = new { StatusId = "1", Header = gridView.GetItemFocus() },Style="background-color:#9E9E9E" },
                        new ContextMenuItem { Text = "Yêu cầu chụp hình", Click = NoteFreeText, Parameter = new { StatusId = "2", Header = gridView.GetItemFocus() },Style="background-color:#ffeb3b" },
                        new ContextMenuItem { Text = "Trễ", Click = NoteFreeText, Parameter = new { StatusId = "3", Header = gridView.GetItemFocus() },Style="background-color:#b1eaf2" },
                        new ContextMenuItem { Text = "Chuyển kho", Click = NoteFreeText, Parameter = new { StatusId = "4", Header = gridView.GetItemFocus() },Style="background-color:#5bcad3" },
                    }
                });
                menus.Add(new ContextMenuItem
                {
                    Icon = "fas fa-pen",
                    Text = "Cập nhật cước",
                    MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Text = "Cước khu vực", Click = UpdateQuotationRegion },
                        new ContextMenuItem { Text = "Cước chi tiết", Click = UpdateQuotation },
                    }
                });
                menus.Add(new ContextMenuItem
                {
                    Icon = "fas fa-pen",
                    Text = "Cập nhật phí",
                    MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Text = "Cập phí nâng", Click = UpdateLadingQuotation },
                        new ContextMenuItem { Text = "Cập phí hạ", Click =  UpdateLiftQuotation},
                    }
                });
                ContextMenu.Instance.MenuItems = menus;
            };
            ChangeBackgroudColorReturn(gridView);
        }

        private void NoteFreeText(object arg)
        {
            Task.Run(async () =>
            {
                var transportation = arg["Header"] as ListViewItem;
                var value = arg["StatusId"] == transportation.Entity["FreeText9"] ? null : arg["StatusId"].ToString();
                var path = new PatchUpdate
                {
                    Changes = new List<PatchUpdateDetail>()
                    {
                        new PatchUpdateDetail()
                        {
                            Field = IdField,
                            Value = transportation.Entity[IdField].ToString()
                        },
                        new PatchUpdateDetail()
                        {
                            Field = nameof(Transportation.FreeText9),
                            Value = value
                        }
                    }
                };
                var rs = await new Client(nameof(Transportation)).PatchAsync<Transportation>(path, ig: $"&disableTrigger=true");
                transportation.Entity.CopyPropFrom(rs);
                transportation.UpdateView(true);
                var containerCom = transportation.FilterChildren<EditableComponent>(y => y.GuiInfo.FieldName == nameof(Transportation.ContainerNo)).FirstOrDefault();
                var td = containerCom.Element.Closest("td");
                td.Style.BackgroundColor = "";
                if (value == "1")
                {
                    td.Style.BackgroundColor = "#9E9E9E";
                }
                else if (value == "2")
                {
                    td.Style.BackgroundColor = "#ffeb3b";
                }
                else if (value == "3")
                {
                    td.Style.BackgroundColor = "#b1eaf2";
                }
                else if (value == "4")
                {
                    td.Style.BackgroundColor = "#5bcad3";
                }
            });
        }

        public void ChangeBackgroudColorReturn(GridView gridView)
        {
            if (gridView is null)
            {
                return;
            }
            foreach (var listViewItem in gridView.MainSection.FilterChildren<ListViewItem>())
            {
                var item = listViewItem.Entity as Transportation;
                if (listViewItem is null || item is null)
                {
                    return;
                }
                var containerCom = listViewItem.FilterChildren<EditableComponent>(y => y.GuiInfo.FieldName == nameof(Transportation.ContainerNo)).FirstOrDefault();
                var returnDateCom = listViewItem.FilterChildren<EditableComponent>(y => y.GuiInfo.FieldName == nameof(Transportation.ReturnDate)).FirstOrDefault();
                var tdContainer = containerCom.Element.Closest("td");
                var tdReturnDate = returnDateCom.Element.Closest("td");
                tdContainer.Style.BackgroundColor = string.Empty;
                tdReturnDate.Style.BackgroundColor = string.Empty;
                if (item.DemDate != null && item.ReturnDate != null && Convert.ToDateTime(item.ReturnDate.Value).Date > Convert.ToDateTime(item.DemDate.Value).Date)
                {
                    tdReturnDate.Style.BackgroundColor = "#f26c6c";
                }
                if (item.FreeText9 == "1")
                {
                    tdContainer.Style.BackgroundColor = "#9E9E9E";
                }
                else if (item.FreeText9 == "2")
                {
                    tdContainer.Style.BackgroundColor = "#ffeb3b";
                }
                else if (item.FreeText9 == "3")
                {
                    tdContainer.Style.BackgroundColor = "#b1eaf2";
                }
                else if (item.FreeText9 == "4")
                {
                    tdContainer.Style.BackgroundColor = "#5bcad3";
                }
            }
        }

        public void AfterPatchUpdateTransportationReturn(Transportation transportation, PatchUpdate patchUpdate, ListViewItem listViewItem)
        {
            if (listViewItem is null)
            {
                return;
            }
            var returnDateCom = listViewItem.FilterChildren<EditableComponent>(y => y.GuiInfo.FieldName == nameof(Transportation.ReturnDate)).FirstOrDefault();
            var tdReturnDate = returnDateCom.Element.Closest("td");
            tdReturnDate.Style.BackgroundColor = string.Empty;
            if (transportation.DemDate != null && transportation.ReturnDate != null && Convert.ToDateTime(transportation.ReturnDate.Value).Date > Convert.ToDateTime(transportation.DemDate.Value).Date)
            {
                tdReturnDate.Style.BackgroundColor = "#f26c6c";
            }
        }

        public override async Task Allotment()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            var selected = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().Where(x => x.Id > 0).ToList();
            if (selected.Nothing())
            {
                Toast.Warning("Vui lòng chọn cont cần phân bổ");
                return;
            }
            var fees = selected.Select(x => new Expense
            {
                ExpenseTypeId = null,
                UnitPrice = 0,
                Quantity = 1,
                IsReturn = true,
                TotalPriceAfterTax = 0,
                TotalPriceBeforeTax = 0,
                Vat = 0,
                ContainerNo = x.ContainerNo,
                SealNo = x.SealNo,
                BossId = x.BossId,
                CommodityId = x.CommodityId,
                ClosingDate = x.ClosingDate,
                ReturnDate = x.ReturnDate,
                TransportationId = x.Id
            }).ToList();
            await this.OpenPopup(
                featureName: "Allotment Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.AllotmentEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Phân bổ chi phí trả hàng";
                    instance.Entity = new Allotment
                    {
                        Expense = fees
                    };
                    return instance;
                });
        }

        public override async Task CheckFee()
        {
            var routeIds = LocalStorage.GetItem<List<int>>("RouteCheckFeeClosing");
            await this.OpenPopup(
                featureName: "CheckFee Form",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeFormBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Kiểm tra bảng kê";
                    instance.Entity = new CheckFeeHistory()
                    {
                        RouteIds = routeIds,
                        FromDate = LocalStorage.GetItem<string>("FromDateCheckFeeClosing") is null ? default(DateTime) : DateTime.Parse(LocalStorage.GetItem<string>("FromDateCheckFeeClosing")),
                        ToDate = LocalStorage.GetItem<string>("ToDateCheckFeeClosing") is null ? default(DateTime) : DateTime.Parse(LocalStorage.GetItem<string>("ToDateCheckFeeClosing")),
                        TypeId = 2,
                    };
                    return instance;
                });
        }

        public override async Task ViewCheckFee(CheckFeeHistory entity)
        {
            await this.OpenTab(
                id: "CheckFee Return Editor" + entity.Id,
                featureName: "CheckFee Return Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeReturnEditorBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    instance.Title = "Kiểm tra phí trả hàng";
                    instance.Icon = "fal fa-sitemap mr-1";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public override async Task EditTransportation(Transportation entity)
        {
            selected = entity;
            var gridView1 = TabEditor.FindComponentByName<GridView>(nameof(Expense));
            if (_expensePopup != null && gridView1 != null)
            {
                return;
            }
            var gridView = this.FindActiveComponent<GridView>(x => x.GuiInfo.RefName == nameof(Transportation)).FirstOrDefault();
            _expensePopup = await gridView.OpenPopup(
                featureName: "Transportation Return Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationReturnEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Xem chi phí trả hàng";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public override async Task ReloadExpense(Transportation entity)
        {
            selected = entity;
            var gridView1 = TabEditor.FindComponentByName<GridView>(nameof(Expense));
            if (_expensePopup is null || gridView1 is null)
            {
                return;
            }
            _expensePopup.Dispose();
            var gridView = this.FindActiveComponent<GridView>(x => x.GuiInfo.RefName == nameof(Transportation)).FirstOrDefault();
            _expensePopup = await gridView.OpenPopup(
                featureName: "Transportation Return Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationReturnEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Xem chi phí";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public override void CheckReturnDate(Transportation Transportation)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(Transportation).FirstOrDefault();
            if (Transportation.ReturnDate != null && Transportation.ReturnDate.Value.Date < Transportation.ShipDate.Value.Date)
            {
                var confirmDialog = new ConfirmDialog
                {
                    Content = "Ngày trả hàng nhỏ hơn ngày tàu cập?"
                };
                confirmDialog.NoConfirmed += () =>
                {
                    Transportation.ReturnDate = null;
                    listViewItem.FilterChildren<EditableComponent>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnDate)).ForEach(x => x.Dirty = true);
                };
                AddChild(confirmDialog);
            }
        }

        public async Task RequestUnClosing(Transportation transportation, PatchUpdate patch)
        {
            var tran = new TransportationListAccountantBL();
            await tran.RequestUnClosing(transportation, patch, this);
        }

        public override async Task ProductionReport()
        {
            await this.OpenPopup(
                featureName: "Production Report",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.ProductionReportFormBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Báo cáo sản lượng";
                    instance.Entity = new ReportGroupVM()
                    {
                        Return = true
                    };
                    return instance;
                });
        }

        public override void UpdateLiftQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật phí hạ!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                if (coords.ReturnEmptyId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7594 " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and LocationId eq {coords.ReturnEmptyId} " +
                    $"and StartDate le {coords.ReturnDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7594,
                        LocationId = coords.ReturnEmptyId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ReturnDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa phí hạ trả hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public override void UpdateLadingQuotation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            Task.Run(async () =>
            {
                var selected = gridView.LastListViewItem;
                if (selected is null)
                {
                    Toast.Warning("Vui lòng chọn cont cần cập nhật phí nâng!");
                    return;
                }
                var coords = selected.Entity.As<Transportation>();
                if (coords.PortLiftId is null || coords.ContainerTypeId is null || coords.ReturnDate is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7596 " +
                   $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                   $"and LocationId eq {coords.PortLiftId} " +
                   $"and StartDate le {coords.ReturnDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7596,
                        LocationId = coords.PortLiftId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ReturnDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa phí nâng trả hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public async Task ViewRequestChange(TransportationRequest transportationRequest)
        {
            checkView = true;
            var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and Id eq {transportationRequest.TransportationId}");
            if (tran != null)
            {
                await TransportationRequestDetailsBL(tran, this);
            }
        }

        public async Task TransportationRequestDetailsBL(Transportation transportation, TabEditor tabEditor)
        {
            await tabEditor.OpenPopup(
               featureName: "Transportation Request Details",
               factory: () =>
               {
                   var type = Type.GetType("TMS.UI.Business.Manage.TransportationRequestDetailsBL");
                   var instance = Activator.CreateInstance(type) as PopupEditor;
                   instance.Title = "Yêu cầu thay đổi";
                   instance.Entity = transportation;
                   return instance;
               });
        }

        public bool getCheckView()
        {
            return checkView;
        }
    }
}