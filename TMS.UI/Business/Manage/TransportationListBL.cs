using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TransportationListBL : TabEditor
    {
        public bool openPopup;
        public GridView gridViewExpense;
        public Transportation selected;
        public TabEditor _expensePopup;

        public TransportationListBL() : base(nameof(Transportation))
        {
            Name = "Transportation List";
        }

        public virtual async Task ViewCheckFee(CheckFeeHistory entity)
        {
            await this.OpenTab(
                id: "CheckFee Editor" + entity.Id,
                featureName: "CheckFee Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeEditorBL");
                    var instance = Activator.CreateInstance(type) as TabEditor;
                    instance.Title = "Kiểm tra phí đóng hàng";
                    instance.Icon = "fal fa-sitemap mr-1";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task ExportCheckFeeSelected()
        {
            await this.OpenPopup(
                featureName: "CheckFee Form",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.CheckFeeFormBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Xuất bảng kê";
                    instance.Entity = new CheckFeeHistory();
                    return instance;
                });
        }

        public virtual async Task CheckFee()
        {
            var routeIds = LocalStorage.GetItem<List<int>>("RouteCheckFeeClosing");
            var closingId = LocalStorage.GetItem<int>("ClosingIdCheckFeeClosing");
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
                        ClosingId = closingId,
                        TypeId = 1,
                    };
                    return instance;
                });
        }

        public virtual async Task ProductionReport()
        {
            await this.OpenPopup(
                featureName: "Production Report",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.ProductionReportFormBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Báo cáo sản lượng";
                    return instance;
                });
        }

        public virtual async Task EditTransportation(Transportation entity)
        {
            selected = entity;
            var gridView = this.FindActiveComponent<GridView>(x => x.GuiInfo.RefName == nameof(Transportation)).FirstOrDefault();
            var gridView1 = TabEditor.FindComponentByName<GridView>(nameof(Expense));
            if (_expensePopup != null && gridView1 != null)
            {
                return;
            }
            _expensePopup?.Dispose();
            _expensePopup = await gridView.OpenPopup(
                featureName: "Transportation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Xem chi phí";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public virtual async Task ReloadExpense(Transportation entity)
        {
            selected = entity;
            var gridView1 = TabEditor.FindComponentByName<GridView>(nameof(Expense));
            if (_expensePopup is null || gridView1 is null)
            {
                return;
            }
            _expensePopup?.Dispose();
            var gridView = this.FindActiveComponent<GridView>(x => x.GuiInfo.RefName == nameof(Transportation)).FirstOrDefault();
            _expensePopup = await gridView.OpenPopup(
                featureName: "Transportation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Xem chi phí";
                    instance.Entity = entity;
                    return instance;    
                });
        }

        public async Task ViewAllotment(Allotment allotment)
        {
            await this.OpenPopup(
                featureName: "Allotment Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.AllotmentEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chi tiết";
                    instance.Entity = allotment;
                    return instance;
                });
        }

        public virtual async Task Allotment()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));
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
                    instance.Title = "Phân bổ chi phí đóng hàng";
                    instance.Entity = new Allotment
                    {
                        UnitPrice = 0,
                        Expense = fees
                    };
                    return instance;
                });
        }

        public virtual void CheckQuotationTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            gridView.BodyContextMenuShow += () =>
            {
                var menus = new List<ContextMenuItem>();
                menus.Clear();
                menus.Add(new ContextMenuItem { Icon = "fas fa-pen", Text = "Cập nhật giá", Click = UpdateQuotation });
                menus.Add(new ContextMenuItem { Icon = "fal fa-binoculars", Text = "Xem booking", Click = ViewBooking });
                menus.Add(new ContextMenuItem { Icon = Icon = "fal fa-download", Text = "Tải đính kèm", Click = DownLoadPackingList });
                menus.Add(new ContextMenuItem
                {
                    Icon = "fas fa-pen",
                    Text = "Cập nhật phí",
                    MenuItems = new List<ContextMenuItem>
                    {
                        new ContextMenuItem { Text = "Cập cước tàu", Click = UpdateShipQuotation },
                        new ContextMenuItem { Text = "Cập phí nâng", Click = UpdateLiftQuotation },
                        new ContextMenuItem { Text = "Cập phí hạ", Click = UpdateLadingQuotation },
                    }
                });
                ContextMenu.Instance.MenuItems = menus;
            };
            var listViewItems = gridView.RowData.Data.Cast<Transportation>().ToList();
            listViewItems.ForEach(x =>
            {
                var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                listViewItem.Element.RemoveClass("bg-host");
                var bookingId = listViewItem.FilterChildren<EditableComponent>(y => y.GuiInfo.FieldName == nameof(Transportation.BookingId)).FirstOrDefault();
                bookingId.Disabled = false;
                if (!x.IsHost)
                {
                    listViewItem.Element.AddClass("bg-host");
                }
                if (!x.IsBooking)
                {
                    bookingId.Disabled = true;
                }
            });
        }

        private void DownLoadPackingList(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            Task.Run(async () =>
            {
                var selected = (await gridView.GetRealTimeSelectedRows()).FirstOrDefault();
                var coord = selected.Cast<Transportation>();
                var booking = await new Client(nameof(Booking)).FirstOrDefaultAsync<Booking>($"?$filter=Active eq true and Id eq {coord.BookingId}");
                if (booking is null || booking.Files.IsNullOrWhiteSpace())
                {
                    booking = await new Client(nameof(Booking)).FirstOrDefaultAsync<Booking>($"?$filter=Active eq true and BrandShipId eq {coord.BrandShipId} and ShipId eq {coord.ShipId} and Trip eq '{coord.Trip}' and (Files ne null or Files ne '') and contains(Files,'    ')");
                    if (booking is null || booking.Files.IsNullOrWhiteSpace())
                    {
                        booking = await new Client(nameof(Booking)).FirstOrDefaultAsync<Booking>($"?$filter=Active eq true and BrandShipId eq {coord.BrandShipId} and ShipId eq {coord.ShipId} and Trip eq '{coord.Trip}' and Files ne null or Files ne ''");
                    }
                }
                else
                {
                    if (!booking.Files.Contains("    "))
                    {
                        var booking1 = await new Client(nameof(Booking)).FirstOrDefaultAsync<Booking>($"?$filter=Active eq true and BrandShipId eq {coord.BrandShipId} and ShipId eq {coord.ShipId} and Trip eq '{coord.Trip}' and (Files ne null or Files ne '') and contains(Files,'    ')");
                        if (booking1 != null && booking1.Files.Contains("    "))
                        {
                            booking = booking1;
                        }
                    }
                }
                if (booking.Files.IsNullOrWhiteSpace())
                {
                    return;
                }
                var newPath = booking.Files.Split("    ").Where(x => x.HasAnyChar()).Distinct().ToList();
                foreach (var path in newPath)
                {
                    Client.Download(path.EncodeSpecialChar());
                }
            });
        }

        public void ViewBooking(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = await gridView.GetRealTimeSelectedRows();
                if (selected.Nothing())
                {
                    selected = gridView.RowData.Data;
                }
                var brandShipIds = selected.Cast<Transportation>().ToList().Where(x => x.BrandShipId != null).Select(x => x.BrandShipId).Distinct().ToList();
                var shipIds = selected.Cast<Transportation>().ToList().Where(x => x.ShipId != null).Select(x => x.ShipId).Distinct().ToList();
                var startShips = selected.Cast<Transportation>().ToList().Where(x => x.StartShip != null).Select(x => x.StartShip).Distinct().ToList();
                var fe = Tabs.Where(x => x.Name == "Booking List").FirstOrDefault();
                if (fe is null)
                {
                    Toast.Warning("Vui lòng mở màn hình booking");
                    return;
                }
                var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == 15759);
                if (gridView1 is null)
                {
                    return;
                }
                gridView1.CellSelected.Clear();
                gridView1.AdvSearchVM.Conditions.Clear();
                gridView1.ListViewSearch.EntityVM.StartDate = null;
                gridView1.ListViewSearch.EntityVM.EndDate = null;
                brandShipIds.ForEach(x =>
                {
                    gridView1.CellSelected.Add(new Core.Models.CellSelected
                    {
                        FieldName = "BrandShipId",
                        FieldText = "Hãng tàu",
                        ComponentType = "Dropdown",
                        Value = x.ToString(),
                        ValueText = x.ToString(),
                        Operator = "in",
                        OperatorText = "Chứa",
                        IsSearch = false,
                        Logic = LogicOperation.And
                    });
                });
                shipIds.ForEach(x =>
                {
                    gridView1.CellSelected.Add(new Core.Models.CellSelected
                    {
                        FieldName = "ShipId",
                        FieldText = "Tàu",
                        ComponentType = "Dropdown",
                        Value = x.ToString(),
                        ValueText = x.ToString(),
                        Operator = "in",
                        OperatorText = "Chứa",
                        IsSearch = false,
                        Logic = LogicOperation.And
                    });
                });
                startShips.ForEach(x =>
                {
                    gridView1.CellSelected.Add(new Core.Models.CellSelected
                    {
                        FieldName = "StartShip",
                        FieldText = "Tàu",
                        ComponentType = "Datepicker",
                        Value = x.ToString(),
                        ValueText = x.ToString(),
                        Operator = "in",
                        OperatorText = "Chứa",
                        IsSearch = false,
                        Logic = LogicOperation.And
                    });
                });
                fe.Focus();
                await gridView1.ActionFilter();
            });
        }

        public void ViewTransportationPlan(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));

            Task.Run(async () =>
            {
                var selected = await gridView.GetRealTimeSelectedRows();
                if (selected.Nothing())
                {
                    selected = gridView.RowData.Data;
                }
                var coords = selected.Cast<Transportation>().ToList().Select(x => x.TransportationPlanId).Distinct().ToList();
                var fe = Tabs.Where(x => x.Name == "Transportation Plan List").FirstOrDefault();
                if (fe is null)
                {
                    Toast.Warning("Vui lòng mở màn hình kế hoạch vận chuyên");
                    return;
                }
                var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == 15768);
                if (gridView1 is null)
                {
                    return;
                }
                gridView1.CellSelected.Clear();
                gridView1.AdvSearchVM.Conditions.Clear();
                coords.ForEach(x =>
                {
                    gridView1.CellSelected.Add(new Core.Models.CellSelected
                    {
                        FieldName = "Id",
                        FieldText = "Mã số",
                        ComponentType = "Number",
                        Value = x.ToString(),
                        ValueText = x.ToString(),
                        Operator = "in",
                        OperatorText = "Chứa",
                        IsSearch = false,
                        Logic = LogicOperation.Or
                    });
                });
                fe.Focus();
                await gridView1.ActionFilter();
            });
        }

        public virtual void UpdateQuotation(object arg)
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
                if (coords.ClosingId is null)
                {
                    Toast.Warning("Vui lòng nhập nhà xe");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7592 " +
                    $"and BossId eq {coords.BossId} " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and LocationId eq {coords.ReceivedId} " +
                    $"and StartDate le {coords.ClosingDate.Value.ToOdataFormat()} " +
                    $"and PackingId eq {coords.ClosingId}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7592,
                        BossId = coords.BossId,
                        ContainerTypeId = coords.ContainerTypeId,
                        LocationId = coords.ReceivedId,
                        StartDate = coords.ClosingDate,
                        PackingId = coords.ClosingId
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá đóng hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void UpdateShipQuotation(object arg)
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
                if (coords.BrandShipId is null || coords.RouteId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7598 " +
                    $"and RouteId eq {coords.RouteId} " +
                    $"and PackingId eq {coords.BrandShipId} " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and cast(StartDate,Edm.DateTimeOffset) le {coords.ClosingDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7598,
                        RouteId = coords.RouteId,
                        PackingId = coords.BrandShipId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ClosingDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá cước tàu";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void UpdateLiftQuotation(object arg)
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
                if (coords.PickupEmptyId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7594 " +
                    $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                    $"and LocationId eq {coords.PickupEmptyId} " +
                    $"and StartDate le {coords.ClosingDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7594,
                        LocationId = coords.PickupEmptyId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ClosingDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá nâng đóng hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void UpdateLadingQuotation(object arg)
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
                if (coords.PortLoadingId is null || coords.ContainerTypeId is null)
                {
                    Toast.Warning("Vui lòng nhập đầy đủ thông tin");
                    return;
                }
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=TypeId eq 7596 " +
                   $"and ContainerTypeId eq {coords.ContainerTypeId} " +
                   $"and LocationId eq {coords.PortLoadingId} " +
                   $"and StartDate le {coords.ClosingDate.Value.ToOdataFormat()}&$orderby=StartDate desc");
                if (quotation is null)
                {
                    quotation = new Quotation()
                    {
                        TypeId = 7596,
                        LocationId = coords.PortLoadingId,
                        ContainerTypeId = coords.ContainerTypeId,
                        StartDate = coords.ClosingDate,
                    };
                }
                await this.OpenPopup(
                featureName: "Quotation Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Settings.QuotationEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa bảng giá hạ đóng hàng";
                    instance.Entity = quotation;
                    return instance;
                });
            });
        }

        public void ChangeBackgroudColor(List<Transportation> listViewItems)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            listViewItems.ForEach(x =>
            {
                var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                listViewItem.Element.RemoveClass("bg-red");
                listViewItem.Element.RemoveClass("bg-red1");
                if (x.DemDate != null && x.ReturnDate != null && Convert.ToDateTime(x.ReturnDate.Value).Date > Convert.ToDateTime(x.DemDate.Value).Date)
                {
                    if (listViewItem != null && !listViewItem.Element.HasClass("bg-red1"))
                    {
                        listViewItem.Element.RemoveClass("bg-red");
                        listViewItem.Element.AddClass("bg-red1");
                    }
                }
                else if (x.DemDate != null && x.ReturnDate != null && Convert.ToDateTime(x.ReturnDate.Value).Date < Convert.ToDateTime(x.DemDate.Value).Date)
                {
                    if (listViewItem != null && listViewItem.Element.HasClass("bg-red1"))
                    {
                        listViewItem.Element.RemoveClass("bg-red1");
                    }
                }
                else
                {
                    listViewItem.Element.RemoveClass("bg-red");
                    listViewItem.Element.RemoveClass("bg-red1");
                }
            });
        }

        public virtual void CheckReturnDate(Transportation transportationPlan)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (transportationPlan.ReturnDate != null && transportationPlan.ShipDate != null && transportationPlan.ReturnDate.Value.Date < transportationPlan.ShipDate.Value.Date)
            {
                var confirmDialog = new ConfirmDialog
                {
                    Content = "Ngày trả hàng phải bằng hoặc lớn hơn ngày tàu cập?"
                };
                confirmDialog.NoConfirmed += async () =>
                {
                    transportationPlan.ReturnDate = transportationPlan.ShipDate;
                    var listViewItem = gridView.GetListViewItems(transportationPlan).FirstOrDefault();
                    listViewItem.UpdateView();
                    var updated = listViewItem.FilterChildren<Datepicker>(x => x.GuiInfo.FieldName == nameof(Transportation.ReturnDate) || x.GuiInfo.FieldName == nameof(Transportation.ShipDate)).ToList();
                    updated.ForEach(x => x.Dirty = true);
                    await listViewItem.PatchUpdate();
                };
                AddChild(confirmDialog);
            }
        }

        public async Task ReturnGoods()
        {
            await this.OpenPopup(
                featureName: "Return TransportationPlan Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.ReturnTransportationPlanEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Trả hàng";
                    instance.Entity = new Transportation();
                    return instance;
                });
        }

        private PatchUpdate GetPathEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsReturn), Value = true.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public async Task SetStartShip()
        {
            await this.OpenPopup(
                featureName: "Set Start Ship",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.SetStartShipBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Cập nhật ngày tàu cập";
                    instance.Entity = new Transportation()
                    {
                        ShipId = selected is null ? null : selected.ShipId,
                        BrandShipId = selected is null ? null : selected.BrandShipId,
                        Trip = selected is null ? null : selected.Trip,
                        RouteIds = selected is null ? null : new List<int>() { selected.RouteId.Value },
                    };
                    return instance;
                });
        }

        public async Task UpdateStartShip()
        {
            await this.OpenPopup(
                featureName: "Update Start Ship",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.UpdateStartShipBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa ngày tàu cập";
                    instance.Entity = new Transportation()
                    {
                        ShipId = selected is null ? null : selected.ShipId,
                        Trip = selected is null ? null : selected.Trip,
                        PortLiftId = selected is null ? null : selected.PortLiftId,
                        ShipDate = selected is null ? null : selected.ShipDate,
                        RouteIds = selected is null ? null : new List<int>() { selected.RouteId.Value },
                    };
                    return instance;
                });
        }

        public async Task<Transportation> CheckAndReturnTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var selecteds = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().ToList();
            var selected = selecteds.FirstOrDefault();
            if (selecteds.Count() > 1)
            {
                Toast.Warning("Chỉ được chọn một danh sách vận chuyển");
                return null;
            }
            if (selected is null)
            {
                Toast.Warning("Vui lòng chọn danh sách vận chuyển");
                return null;
            }
            return selected;
        }

        private int containerId = 0;

        public async Task<int> CheckContainerType(Expense expense)
        {
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)expense.ContainerTypeId);
            var containers = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC') or contains(Name, '45HC') or contains(Name, '50DC'))");
            if (containerTypeName.Description.Contains("Cont 20"))
            {
                containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 40"))
            {
                containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 45"))
            {
                containerId = containers.Find(x => x.Name.Contains("45HC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 50"))
            {
                containerId = containers.Find(x => x.Name.Contains("50DC")).Id;
            }
            return containerId;
        }

        private CommodityValue CreateCommodityValue(Expense expense, decimal totalPrice)
        {
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
            var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var newCommodityValue = new CommodityValue();
            newCommodityValue.CopyPropFrom(expense);
            newCommodityValue.Id = 0;
            newCommodityValue.ContainerId = containerId;
            newCommodityValue.TotalPrice = totalPrice;
            newCommodityValue.Notes = expense.CommodityValueNotes;
            newCommodityValue.Active = true;
            newCommodityValue.InsertedDate = DateTime.Now.Date;
            newCommodityValue.StartDate = DateTime.Now.Date;
            newCommodityValue.CreatedBy = Client.Token.UserId;
            if (DateTime.Now.Date >= startDate1 && DateTime.Now.Date <= endDate1)
            {
                newCommodityValue.EndDate = endDate1;
            }
            if (DateTime.Now.Date >= startDate2 && DateTime.Now.Date <= endDate2)
            {
                newCommodityValue.EndDate = endDate2;
            }
            return newCommodityValue;
        }

        public void CalcTax(Expense expense)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Expense));
            var listViewItem = grid.GetListViewItems(expense).FirstOrDefault();
            expense.TotalPriceBeforeTax = expense.UnitPrice * expense.Quantity;
            expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + expense.TotalPriceBeforeTax * expense.Vat / 100;
            if (listViewItem != null)
            {
                listViewItem.UpdateView();
                var updated = listViewItem.FilterChildren(x => x.GuiInfo.FieldName == nameof(Expense.TotalPriceBeforeTax) || x.GuiInfo.FieldName == nameof(Expense.TotalPriceAfterTax));
                updated.ForEach(x => x.Dirty = true);
            }
        }

        private int commodityAwaiter;

        public void UpdateCommodityValue(Expense expense)
        {
            Window.ClearTimeout(commodityAwaiter);
            commodityAwaiter = Window.SetTimeout(async () =>
            {
                await UpdateCommodityAsync(expense);
            }, 500);
            CalcTax(expense);
        }

        private async Task UpdateCommodityAsync(Expense expense)
        {
            var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and Id eq {expense.ExpenseTypeId}");
            if (expenseType.Name.Contains("Bảo hiểm") == false && expenseType.Name.Contains("BH SOC") == false)
            {
                return;
            }
            if (expense.BossId != null && expense.BossId > 0 && expense.CommodityId != null && expense.CommodityId > 0 && expense.ContainerTypeId != null && expense.ContainerTypeId > 0)
            {
                var containerId = await CheckContainerType(expense);
                var commodityValueDB = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {expense.BossId} and CommodityId eq {expense.CommodityId} and ContainerId eq {containerId}");
                var boss = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and Id eq {expense.BossId}");
                if (commodityValueDB is null)
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có muốn lưu giá trị này vào bảng GTHH không?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        var newCommodityValue = CreateCommodityValue(expense, (decimal)expense.CommodityValue);
                        newCommodityValue.SaleId = boss.UserId;
                        newCommodityValue.CreatedBy = Client.Token.UserId;
                        await new Client(nameof(CommodityValue)).CreateAsync(newCommodityValue);
                    };
                }
                else
                {
                    if (expense.CommodityValue != commodityValueDB.TotalPrice)
                    {
                        var confirm = new ConfirmDialog
                        {
                            Content = "Bạn có muốn lưu giá trị này vào bảng GTHH không?",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            commodityValueDB.EndDate = DateTime.Now.Date;
                            commodityValueDB.Active = false;
                            await new Client(nameof(CommodityValue)).PatchAsync<object>(GetPatchEntity(commodityValueDB));
                            var newCommodityValue = CreateCommodityValue(expense, (decimal)expense.CommodityValue);
                            newCommodityValue.StartDate = DateTime.Now.Date;
                            await new Client(nameof(CommodityValue)).CreateAsync(newCommodityValue);
                        };
                    }
                    GridView gridView = this.FindComponentByName<GridView>(nameof(Expense));
                    var masterDataDB = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 11685");
                    var vat = decimal.Parse(masterDataDB.Name);
                    if (expenseType.Name.Contains("Bảo hiểm"))
                    {
                        await CalcInsuranceFees(expense, false);
                    }
                    else if (expenseType.Name.Contains("BH SOC"))
                    {
                        await CalcInsuranceFees(expense, true);
                    }
                    await new Client(nameof(Expense)).PatchAsync<object>(GetPatchEntity(expense));
                    var listViewItem = gridView.GetListViewItems(expense).FirstOrDefault();
                    listViewItem.UpdateView();
                    var updated = listViewItem.FilterChildren<Number>(x => x.GuiInfo.FieldName == nameof(Expense.CommodityValue) || x.GuiInfo.FieldName == nameof(Expense.TotalPriceBeforeTax) || x.GuiInfo.FieldName == nameof(Expense.TotalPriceAfterTax)).ToList();
                    updated.ForEach(x => x.Dirty = true);
                    await listViewItem.PatchUpdate();
                }
            }
        }

        public async Task UpdateCombinationFee(Transportation transportation)
        {
            if (transportation.BrandShipId is null)
            {
                return;
            }
            var gridView = this.FindComponentByName<GridView>(nameof(Transportation));
            var listViewItem = gridView.GetListViewItems(transportation).FirstOrDefault();
            if (transportation.IsEmptyCombination || transportation.IsClosingCustomer)
            {
                var quotation = await new Client(nameof(Quotation)).FirstOrDefaultAsync<Quotation>($"?$filter=Active eq true and TypeId eq 12071 and PackingId eq {transportation.BrandShipId}");
                transportation.CombinationFee = quotation is null ? default(decimal) : quotation.UnitPrice;
            }
            else
            {
                transportation.CombinationFee = 0;
            }
            listViewItem.UpdateView();
            listViewItem.FilterChildren(x => x.GuiInfo.FieldName == nameof(Transportation.CombinationFee)).ForEach(x => x.Dirty = true);
        }

        public void BookingChange(Transportation transportation, Booking booking)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            var listViewItem = gridView.GetListViewItems(transportation).FirstOrDefault();
            transportation.ShipId = booking is null ? null : booking.ShipId;
            transportation.BrandShipId = booking is null ? null : booking.BrandShipId;
            transportation.Trip = booking is null ? null : booking.Trip;
            transportation.StartShip = booking is null ? null : booking.StartShip;
            transportation.PickupEmptyId = booking is null ? null : booking.PickupEmptyId;
            transportation.PortLoadingId = booking is null ? null : booking.PortLoadingId;
            transportation.LineId = booking is null ? null : booking.LineId;
            transportation.PolicyId = booking is null ? null : transportation.PolicyId;
            transportation.ShipPolicyPrice = booking is null ? 0 : transportation.ShipPolicyPrice;
            transportation.ShipUnitPrice = booking is null ? null : transportation.ShipUnitPrice;
            listViewItem.UpdateView(true);
            listViewItem.FilterChildren(x =>
            x.GuiInfo.FieldName == nameof(Transportation.ShipId)
            || x.GuiInfo.FieldName == nameof(Transportation.PolicyId)
            || x.GuiInfo.FieldName == nameof(Transportation.BrandShipId)
            || x.GuiInfo.FieldName == nameof(Transportation.Trip)
            || x.GuiInfo.FieldName == nameof(Transportation.LineId)
            || x.GuiInfo.FieldName == nameof(Transportation.StartShip)
            || x.GuiInfo.FieldName == nameof(Transportation.PortLoadingId)
            || x.GuiInfo.FieldName == nameof(Transportation.PickupEmptyId)
            || x.GuiInfo.FieldName == nameof(Transportation.ShipUnitPrice)
            || x.GuiInfo.FieldName == nameof(Transportation.ShipPrice)
            || x.GuiInfo.FieldName == nameof(Transportation.ShipPolicyPrice)
            ).ForEach(x => x.Dirty = true);
        }

        public async Task AfterPatchUpdateTransportation(Transportation transportation, PatchUpdate patchUpdate, ListViewItem listViewItem)
        {
            if (transportation.BookingId is null)
            {
                return;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerTypeId)
            || x.Field == nameof(Transportation.BossId)
            || x.Field == nameof(Transportation.Weight)
            || x.Field == nameof(Transportation.CommodityId)
            || x.Field == nameof(Transportation.StartShip)
            || x.Field == nameof(Transportation.ShipId)
            || x.Field == nameof(Transportation.BookingId)
            || x.Field == nameof(Transportation.RouteId)))
            {
                transportation.StartShip = Convert.ToDateTime(transportation.StartShip);
                var startShip = transportation.StartShip.Value.ToOdataFormat();
                var listpolicy = await new Client(nameof(SettingPolicy)).GetRawList<SettingPolicy>($"?$expand=SettingPolicyDetail&$filter=ExportListId eq {transportation.ExportListId} and BrandShipId eq {transportation.BrandShipId} and StartDate lt {startShip} and (EndDate gt {startShip} or EndDate eq null) and TypeId eq 1&$orderby=UnitPrice desc");
                if (listpolicy.Nothing())
                {
                    Toast.Success("Không tìm thấy chính sách");
                    return;
                }
                Toast.Warning("Hệ thống đang lấy chính sách hãng tàu");
                var components = new Client(nameof(GridPolicy)).GetRawList<GridPolicy>("?$filter=ComponentId eq 16016");
                var operators = new Client(nameof(MasterData)).GetRawList<MasterData>("?$filter=Parent/Name eq 'Operator'");
                if (transportation.StartShip is null)
                {
                    return;
                }
                await Task.WhenAll(components, operators);
                var componentrs = components.Result;
                var operatorrs = operators.Result;
                var query = new List<string>();
                var rs = listpolicy.SelectMany(item =>
                {
                    var detail = item.SettingPolicyDetail.ToList();
                    var build = detail.GroupBy(z => z.ComponentId).SelectMany(y =>
                    {
                        var group = y.ToList().Select(l =>
                        {
                            var component = componentrs.FirstOrDefault(k => k.Id == l.ComponentId);
                            if (component is null)
                            {
                                return null;
                            }
                            var ope = operatorrs.FirstOrDefault(k => k.Id == l.OperatorId);
                            if (component.ComponentType == "Dropdown" || component.ComponentType == nameof(SearchEntry))
                            {
                                var format = component.FormatCell.Split("}")[0].Replace("{", "");
                                if (component.FieldName == nameof(Transportation.CommodityId))
                                {
                                    return new Client(component.RefName).GetRawList<dynamic>(string.Format($"?$expand=InverseParent&$filter={ope.Name}", format, l.Value), entityName: component.RefName);
                                }
                                else
                                {
                                    return new Client(component.RefName).GetRawList<dynamic>(string.Format($"?$select=Id&$filter={ope.Name}", format, l.Value), entityName: component.RefName);
                                }
                            }
                            else
                            {
                                return null;
                            }
                        });
                        return group;
                    }).ToList();
                    return build;
                }).Where(x => x != null).ToList();
                if (rs.Nothing())
                {
                    Toast.Success("Không tìm thấy chính sách");
                    return;
                }
                var data = await Task.WhenAll(rs);
                if (data.Nothing())
                {
                    return;
                }
                var index = 0;
                foreach (var item in listpolicy)
                {
                    var detail = item.SettingPolicyDetail.ToList();
                    var build = detail.GroupBy(z => z.ComponentId).Select(y =>
                    {
                        var listAnd = new List<string>();
                        var group = y.ToList().Select(l =>
                        {
                            var component = componentrs.FirstOrDefault(k => k.Id == l.ComponentId);
                            if (component is null)
                            {
                                return null;
                            }
                            var ope = operatorrs.FirstOrDefault(k => k.Id == l.OperatorId);
                            if (component.ComponentType == "Dropdown" || component.ComponentType == nameof(SearchEntry))
                            {
                                var rsdynamic = data[index];
                                index++;
                                if (rsdynamic.Any())
                                {
                                    var ids = string.Empty;
                                    if (component.FieldName == nameof(Transportation.CommodityId))
                                    {
                                        var listMasterData = rsdynamic.Select(x => new MasterData() { Id = x.Id, ParentId = x.ParentId, InverseParent = x.InverseParent }).ToList();
                                        var child = listMasterData.Where(x => x.InverseParent.Any()).SelectMany(x => x.InverseParent).ToList();
                                        if (child.Any())
                                        {
                                            var childIds = child.Select(x => x.Id).ToList();
                                            ids = listMasterData.Select(x => x.Id).Cast<int>().ToList().Union(listMasterData.Where(x => x.ParentId != null).Select(x => x.ParentId).Cast<int>().ToList()).Union(childIds).Combine();
                                        }
                                        else
                                        {
                                            ids = listMasterData.Select(x => x.Id).Cast<int>().ToList().Union(listMasterData.Where(x => x.ParentId != null).Select(x => x.ParentId).Cast<int>().ToList()).Where(x => x > 0).Combine();
                                        }
                                    }
                                    else
                                    {
                                        ids = rsdynamic.Select(x => x.Id).Cast<int>().Where(x => x > 0).Combine();
                                    }
                                    var format = component.FormatCell.Split("}")[0].Replace("{", "");
                                    if (ope.Description == "Chứa" || ope.Description == "Bằng")
                                    {
                                        return $"{component.FieldName} in ({ids})";
                                    }
                                    else
                                    {
                                        return $"{component.FieldName} in ({ids}) eq false";
                                    }
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                listAnd.Add($"{string.Format(ope.Name, component.FieldName, l.Value)}");
                                return null;
                            }
                        }).Where(x => x != "()" && !x.IsNullOrWhiteSpace()).ToList();
                        return (group.Count == 0 ? "" : $"({group.Where(x => x != "()" && !x.IsNullOrWhiteSpace()).Combine(" or ")})") + (listAnd.Count == 0 ? "" : $" {listAnd.Where(x => !x.IsNullOrWhiteSpace()).Combine(" and ")}");
                    }).ToList();
                    query.Add(build.Where(x => x != "()" && !x.IsNullOrWhiteSpace()).Combine(" and "));
                }
                var checks = query.Where(x => !x.IsNullOrWhiteSpace()).Select(x => new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$select=Id&$filter=Active eq true and Id eq {transportation.Id} and {x}")).ToList();
                var data1 = new List<Transportation>();
                foreach (var item in checks)
                {
                    data1.Add(await item);
                }
                var indexOfShip = listpolicy.IndexOf(x => x.CheckAll);
                var indexOf = data1.IndexOf(x => x != null);
                if (indexOf == -1)
                {
                    transportation.PolicyId = null;
                    transportation.ShipPolicyPrice = null;
                }
                else
                {
                    if (indexOfShip > 0)
                    {
                        var tranShip = data1[indexOfShip];
                        if (tranShip != null)
                        {
                            transportation.PolicyId = listpolicy[indexOfShip].PolicyId;
                            transportation.ShipPolicyPrice = listpolicy[indexOfShip].UnitPrice;
                            var index1 = 0;
                            foreach (var item in data1)
                            {
                                if (index1 != indexOfShip && item != null)
                                {
                                    transportation.ShipPolicyPrice += listpolicy[index1].UnitPrice;
                                    break;
                                }
                                index1++;
                            }
                        }
                        else
                        {
                            transportation.PolicyId = listpolicy[indexOf].PolicyId;
                            transportation.ShipPolicyPrice = listpolicy[indexOf].UnitPrice;
                        }
                    }
                    else
                    {
                        transportation.PolicyId = listpolicy[indexOf].PolicyId;
                        transportation.ShipPolicyPrice = listpolicy[indexOf].UnitPrice;
                    }
                }
                transportation.ShipPrice = (transportation.ShipUnitPriceQuotation is null ? default(decimal) : transportation.ShipUnitPriceQuotation.Value) - (transportation.ShipPolicyPrice is null ? default(decimal) : transportation.ShipPolicyPrice.Value);
                listViewItem.UpdateView(true);
                listViewItem.FilterChildren(x =>
                x.GuiInfo.FieldName == nameof(Transportation.PolicyId)
                || x.GuiInfo.FieldName == nameof(Transportation.ShipPolicyPrice)
                ).ForEach(x => x.Dirty = true);
                await listViewItem.PatchUpdate(true);
                Toast.Success("Đã áp dụng chính sách");
            }
        }

        public async Task SetPolicyTransportationType(Transportation transportation)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            var listViewItem = gridView.GetListViewItems(transportation).FirstOrDefault();
            if (transportation.RouteId != null || transportation.ClosingId != null)
            {
                transportation.TransportationTypeId = null;
                var components = new Client(nameof(GridPolicy)).GetRawList<GridPolicy>("?$filter=Id in (20347, 20342)");
                var operators = new Client(nameof(MasterData)).GetRawList<MasterData>("?$filter=Parent/Name eq 'Operator'");
                var settingPolicys = new Client(nameof(SettingPolicy)).GetRawList<SettingPolicy>($"?$orderby=Id asc&$expand=SettingPolicyDetail&$filter=TypeId eq 2");
                await Task.WhenAll(components, operators, settingPolicys);
                var listpolicy = settingPolicys.Result;
                var componentrs = components.Result;
                var operatorrs = operators.Result;
                var query = new List<string>();
                var rs = listpolicy.SelectMany(item =>
                {
                    var detail = item.SettingPolicyDetail.ToList();
                    var build = detail.GroupBy(z => z.ComponentId).SelectMany(y =>
                    {
                        var group = y.ToList().Select(l =>
                        {
                            var component = componentrs.FirstOrDefault(k => k.Id == l.ComponentId);
                            if (component is null)
                            {
                                return null;
                            }
                            var ope = operatorrs.FirstOrDefault(k => k.Id == l.OperatorId);
                            if (component.ComponentType == "Dropdown" || component.ComponentType == nameof(SearchEntry))
                            {
                                var format = component.FormatCell.Split("}")[0].Replace("{", "");
                                return new Client(component.RefName).GetRawList<dynamic>(string.Format($"?$select=Id&$filter={ope.Name}", format, l.Value), entityName: component.RefName);
                            }
                            else
                            {
                                return null;
                            }
                        });
                        return group;
                    }).ToList();
                    return build;
                }).Where(x => x != null).ToList();
                var data = await Task.WhenAll(rs);
                var index = 0;
                if (data.Nothing())
                {
                    return;
                }
                foreach (var item in listpolicy)
                {
                    var detail = item.SettingPolicyDetail.ToList();
                    var build = detail.GroupBy(z => z.ComponentId).Select(y =>
                    {
                        var listAnd = new List<string>();
                        var group = y.ToList().Select(l =>
                        {
                            var component = componentrs.FirstOrDefault(k => k.Id == l.ComponentId);
                            if (component is null)
                            {
                                return null;
                            }
                            var ope = operatorrs.FirstOrDefault(k => k.Id == l.OperatorId);
                            if (component.ComponentType == "Dropdown" || component.ComponentType == nameof(SearchEntry))
                            {
                                var rsdynamic = data[index];
                                index++;
                                if (rsdynamic.Any())
                                {
                                    var ids = rsdynamic.Select(x => x.Id).Cast<int>().Combine();
                                    var format = component.FormatCell.Split("}")[0].Replace("{", "");
                                    if (ope.Description == "Chứa" || ope.Description == "Bằng")
                                    {
                                        return $"{component.FieldName} in ({ids})";
                                    }
                                    else
                                    {
                                        return $"{component.FieldName} in ({ids}) eq false";
                                    }
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                listAnd.Add($"{string.Format(ope.Name, component.FieldName, l.Value)}");
                                return null;
                            }
                        }).Where(x => x != "()" && !x.IsNullOrWhiteSpace()).ToList();
                        return (group.Count == 0 ? "" : $"({group.Where(x => x != "()" && !x.IsNullOrWhiteSpace()).Combine(" or ")})") + (listAnd.Count == 0 ? "" : $" {listAnd.Where(x => !x.IsNullOrWhiteSpace()).Combine(" and ")}");
                    }).ToList();
                    var str = build.Where(x => x != "()" && !x.IsNullOrWhiteSpace()).Combine(" or ");
                    query.Add(str);
                    Transportation check = null;
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        check = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and Id eq {transportation.Id} and ({str})");
                    }
                    if (check != null)
                    {
                        transportation.TransportationTypeId = item.TransportationTypeId;
                    }
                }
                await ActionAnalysis(transportation);
            }
        }

        private async Task ActionAnalysis(Transportation transportation)
        {
            var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'Bảo hiểm')");
            var expense = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and ExpenseTypeId eq {expenseType.Id} and RequestChangeId eq null");
            if (expense is null || transportation.TransportationTypeId is null)
            {
                return;
            }
            else
            {
                if (expense.IsPurchasedInsurance == false)
                {
                    expense.TransportationTypeId = transportation.TransportationTypeId;
                    expense.CommodityId = transportation.CommodityId;
                    expense.ContainerTypeId = transportation.ContainerTypeId;
                    containerId = await CheckContainerType(expense);
                    var commodityValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {expense.BossId} and CommodityId eq {expense.CommodityId} and ContainerId eq {containerId}");
                    if (commodityValue != null)
                    {
                        expense.CommodityValue = commodityValue.TotalPrice;
                        expense.IsWet = commodityValue.IsWet;
                        expense.IsBought = commodityValue.IsBought;
                        expense.JourneyId = commodityValue.JourneyId;
                        expense.CustomerTypeId = commodityValue.CustomerTypeId;
                    }
                    else
                    {
                        var newCommodityValue = CreateCommodityValue(expense, (decimal)expense.CommodityValue);
                    }
                    await CalcInsuranceFees(expense, false);
                    await new Client(nameof(Expense)).UpdateAsync<Expense>(expense);
                }
                else
                {
                    var history = new Expense();
                    history.CopyPropFrom(expense);
                    history.Id = 0;
                    history.StatusId = (int)ApprovalStatusEnum.New;
                    history.RequestChangeId = expense.Id;
                    var res = await new Client(nameof(Expense)).CreateAsync<Expense>(history);
                    if (res != null)
                    {
                        expense.TransportationTypeId = transportation.TransportationTypeId;
                        expense.CommodityId = transportation.CommodityId;
                        expense.ContainerTypeId = transportation.ContainerTypeId;
                        await new Client(nameof(Expense)).UpdateAsync<Expense>(expense);
                    }
                }
                Expense expenseSOC = null;
                if (transportation.SocId != null)
                {
                    var expenseTypeSOC = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'BH SOC')");
                    expenseSOC = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and ExpenseTypeId eq {expenseTypeSOC.Id} and RequestChangeId eq null");
                    expenseSOC.TransportationTypeId = expense.TransportationTypeId;
                    expenseSOC.ContainerTypeId = expense.ContainerTypeId;
                    if (expenseSOC.IsPurchasedInsurance == false)
                    {
                        var commodityValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {expenseSOC.BossId} and CommodityId eq {expenseSOC.CommodityId} and ContainerId eq {containerId}");
                        if (commodityValue != null)
                        {
                            expenseSOC.CommodityValue = commodityValue.TotalPrice;
                            expenseSOC.JourneyId = commodityValue.JourneyId;
                            expenseSOC.CustomerTypeId = commodityValue.CustomerTypeId;
                        }
                        await CalcInsuranceFees(expenseSOC, true);
                        await new Client(nameof(Expense)).UpdateAsync<Expense>(expenseSOC);
                    }
                    else
                    {
                        var history = new Expense();
                        history.CopyPropFrom(expense);
                        history.Id = 0;
                        history.StatusId = (int)ApprovalStatusEnum.New;
                        history.RequestChangeId = expense.Id;
                        var res = await new Client(nameof(Expense)).CreateAsync<Expense>(history);
                        if (res != null)
                        {
                            expense.TransportationTypeId = transportation.TransportationTypeId;
                            expense.CommodityId = transportation.CommodityId;
                            expense.ContainerTypeId = transportation.ContainerTypeId;
                            await new Client(nameof(Expense)).UpdateAsync<Expense>(expense);
                        }
                    }
                }
            }
        }

        private async Task CalcInsuranceFees(Expense expense, bool isSOC)
        {
            if (expense.TransportationTypeId is null || expense.JourneyId is null)
            {
                return;
            }
            bool isSubRatio = false;
            if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
            {
                isSubRatio = true;
            }
            var journeyId = expense.JourneyId is null ? "" : "and JourneyId eq " + expense.JourneyId.ToString();
            InsuranceFeesRate insuranceFeesRateDB = null;
            if (expense.IsBought)
            {
                insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true " +
                $" and TransportationTypeId eq {expense.TransportationTypeId} " +
                $" {journeyId} " +
                $" and IsBought eq {expense.IsBought.ToString().ToLower()} " +
                $" and IsSOC eq {isSOC.ToString().ToLower()}" +
                $" and IsSubRatio eq {isSubRatio.ToString().ToLower()}"
                );
            }
            else
            {
                insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true " +
                $" and TransportationTypeId eq {expense.TransportationTypeId} " +
                $" {journeyId} " +
                $" and IsBought eq {expense.IsBought.ToString().ToLower()} " +
                $" and IsSOC eq {isSOC.ToString().ToLower()}"
                );
            }
            if (insuranceFeesRateDB != null)
            {
                if (expense.ExpenseTypeId == 15981)
                {
                    expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                }
                else
                {
                    var getContainerType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {expense.ContainerTypeId}");
                    if (getContainerType != null && getContainerType.Description.ToLower().Contains("lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                    {
                        var insuranceFeesRateColdDB = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 25391");
                        expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                    }
                    else
                    {
                        expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                    }
                    if (insuranceFeesRateDB.IsSubRatio && insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
                    {
                        var extraInsuranceFeesRateDB = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 25374");
                        extraInsuranceFeesRateDB.ForEach(x =>
                        {
                            var prop = expense.GetType().GetProperties().Where(y => y.Name == x.Name && bool.Parse(y.GetValue(expense, null).ToString())).FirstOrDefault();
                            if (prop != null)
                            {
                                expense.InsuranceFeeRate += decimal.Parse(x.Code);
                            }
                        });
                    }
                }
            }
            else
            {
                expense.InsuranceFeeRate = 0;
                expense.TotalPriceBeforeTax = 0;
                expense.TotalPriceAfterTax = 0;
            }
            if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
            {
                CalcInsuranceFeeNoVAT(expense);
            }
            else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
            {
                CalcInsuranceFee(expense);
            }
        }

        private async Task CreateExpenseSOC(Transportation transportation)
        {
            var expenseTypeSOC = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'BH SOC')");
            var expense = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and ExpenseTypeId eq {expenseTypeSOC.Id} and RequestChangeId eq null");
            if (expense is null && transportation.SocId != null)
            {
                var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'Bảo hiểm')");
                expense = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and ExpenseTypeId eq {expenseType.Id} and RequestChangeId eq null");
                var containerId = await CheckContainerType(expense);
                var commodity = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId ne 7651 and contains(Path, '7651') and contains(Description, 'Vỏ rỗng')");
                var commodityValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and CommodityId eq {commodity.Id} and ContainerId eq {containerId}");
                var expenseSOC = new Expense();
                expenseSOC.CopyPropFrom(expense);
                expenseSOC.Id = 0;
                expenseSOC.CommodityId = commodity.Id; // vỏ rỗng
                expenseSOC.ExpenseTypeId = expenseTypeSOC.Id; //SOC
                expenseSOC.IsWet = false;
                expenseSOC.IsBought = false;
                expenseSOC.SteamingTerms = false;
                expenseSOC.BreakTerms = false;
                expenseSOC.CommodityValue = commodityValue.TotalPrice;
                expenseSOC.JourneyId = 12114;
                await CalcInsuranceFees(expenseSOC, true);
                await new Client(nameof(Expense)).CreateAsync<Expense>(expenseSOC);
            }
            else if (expense != null)
            {
                if (transportation.SocId is null)
                {
                    await new Client(nameof(Expense)).HardDeleteAsync(expense.Id);
                }
            }
            else
            {
                return;
            }
        }

        private void CalcInsuranceFee(Expense expense)
        {
            expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
        }

        private void CalcInsuranceFeeNoVAT(Expense expense)
        {
            expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
            expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
        }

        public PatchUpdate GetPatchEntity(CommodityValue commodityValue)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = commodityValue.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(CommodityValue.Active), Value = commodityValue.Active.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(CommodityValue.EndDate), Value = commodityValue.EndDate is null ? "" : commodityValue.EndDate.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntity(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.CommodityValue), Value = expense.CommodityValue is null ? "" : expense.CommodityValue.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.TotalPriceAfterTax), Value = expense.TotalPriceAfterTax.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.TotalPriceBeforeTax), Value = expense.TotalPriceBeforeTax.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public async Task BeforePatchUpdateTransportation(Transportation transportation, PatchUpdate patchUpdate)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            if (patchUpdate.Changes.Any(x => x.Field == nameof(Transportation.ContainerNo) && !x.Value.IsNullOrWhiteSpace()))
            {
                var tran = await Client.FirstOrDefaultAsync<Transportation>($"?$top=1&$select=ClosingDate&$filter=ContainerNo eq '{patchUpdate.Changes.FirstOrDefault(x => x.Field == nameof(Transportation.ContainerNo)).Value}' and Id ne {transportation.Id}");
                if (tran != null)
                {
                    if ((tran.ClosingDate.Value - transportation.ClosingDate.Value).Days < 7)
                    {
                        Toast.Warning("Số cont bạn chọn đã đóng hàng chưa được 7 ngày");
                    }
                }
            }
            var checkLock = new TransportationListAccountantBL();
            await checkLock.RequestUnClosing(transportation, patchUpdate);
        }

        public async Task LockShipTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            var listViewItems = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().Where(x => x.LockShip == false).ToList();
            if (listViewItems.Count <= 0)
            {
                listViewItems = gridView.RowData.Data.Cast<Transportation>().Where(x => x.LockShip == false).ToList();
            }
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không DSVC nào cần khóa");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn khóa " + listViewItems.Count + " DSVC ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var checks = listViewItems.Where(x => x.IsLocked).ToList();
                if (checks.Count > 0)
                {
                    var confirmRequest = new ConfirmDialog
                    {
                        NeedAnswer = true,
                        ComType = nameof(Textbox),
                        Content = $"Đã có {checks.Count} DSVC bị khóa (Hệ thống). Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                        "Hãy nhập lý do",
                    };
                    confirmRequest.Render();
                    confirmRequest.YesConfirmed += async () =>
                    {
                        foreach (var item in checks)
                        {
                            item.ReasonUnLockAll = confirmRequest.Textbox?.Text;
                            await new Client(nameof(Transportation)).PostAsync<Transportation>(item, "RequestUnLockAll");
                        }
                    };
                    var transportationNoLock = listViewItems.Where(x => x.IsLocked == false).ToList();
                    var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "LockShipTransportation");
                    if (res)
                    {
                        await gridView.ApplyFilter(true);
                    }
                }
                else
                {
                    var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "LockShipTransportation");
                    if (res)
                    {
                        await gridView.ApplyFilter(true);
                    }
                }
            };
        }

        public async Task UnLockShipTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Transportation));
            if (gridView is null)
            {
                return;
            }
            var listViewItems = (await gridView.GetRealTimeSelectedRows()).Cast<Transportation>().Where(x => x.LockShip).ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Không DSVC nào cần mở khóa");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn mở khóa " + listViewItems.Count + " DSVC ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var checks = listViewItems.Where(x => x.IsLocked).ToList();
                if (checks.Count > 0)
                {
                    var confirmRequest = new ConfirmDialog
                    {
                        NeedAnswer = true,
                        ComType = nameof(Textbox),
                        Content = $"Đã có {checks.Count} DSVC bị khóa (Hệ thống). Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                        "Hãy nhập lý do",
                    };
                    confirmRequest.Render();
                    confirmRequest.YesConfirmed += async () =>
                    {
                        foreach (var item in checks)
                        {
                            item.ReasonUnLockAll = confirmRequest.Textbox?.Text;
                            await new Client(nameof(Transportation)).PostAsync<Transportation>(item, "RequestUnLockAll");
                        }
                    };
                    var transportationNoLock = listViewItems.Where(x => x.LockShip == false).ToList();
                    await UnLockShipTransportationNoCheck(transportationNoLock);
                    await gridView.ApplyFilter(true);
                }
                else
                {
                    await UnLockShipTransportationNoCheck(listViewItems);
                    await gridView.ApplyFilter(true);
                }
            };
        }

        public async Task UnLockShipTransportationNoCheck(List<Transportation> transportations)
        {
            var checkRequests = transportations.Where(x => x.IsRequestUnLockShip).ToList();
            if (checkRequests.Count > 0)
            {
                var confirmRequets = new ConfirmDialog
                {
                    Content = $"Có {checkRequests.Count} DSVC cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                };
                confirmRequets.Render();
                confirmRequets.YesConfirmed += async () =>
                {
                    await new Client(nameof(Transportation)).PostAsync<bool>(checkRequests, "ApproveUnLockShip");
                };
                await new Client(nameof(Transportation)).PostAsync<bool>(transportations.Where(x => x.IsRequestUnLockShip == false), "UnLockShipTransportation");
            }
            else if (checkRequests.Count <= 0)
            {
                await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "UnLockShipTransportation");
            }
        }
    }
}