using Core.Components.Extensions;
using Core.Components;
using Core.Components.Forms;
using Core.Extensions;
using System;
using TMS.API.Models;
using System.Linq;
using Core.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.ViewModels;
using Bridge.Html5;

namespace TMS.UI.Business.Manage
{
    public class TransportationListAccountantBL : TabEditor
    {
        public Transportation selected;
        public TransportationListAccountantBL() : base(nameof(Transportation))
        {
            Name = "Transportation List Accountant";
        }

        public async Task ImportRevenueSimultaneous()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            await this.OpenPopup(
                featureName: "Import Revenue Simultaneous",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ImportRevenueSimultaneousBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Nhập đồng loạt doanh thu";
                    instance.Entity = new Transportation();
                    return instance;
                });
        }

        public async Task ReloadExpense(Transportation transportation)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Expense));
            grid.DataSourceFilter = $"?$filter=Active eq true and TransportationId eq {transportation.Id} and ((ExpenseTypeId in (15981, 15939) eq false) or IsPurchasedInsurance eq true)";
            selected = transportation;
            await grid.ApplyFilter(true);
        }

        public async Task RequestUnClosing(Transportation transportation, PatchUpdate patch)
        {
            if (transportation.IsLocked == false && transportation.IsKt == false && transportation.IsSubmit == false && transportation.LockShip == false)
            {
                return;
            }
            if (patch.Changes.Any(x => x.Field != nameof(transportation.Notes) &&
            x.Field != nameof(transportation.Id) &&
            x.Field != nameof(transportation.ExportListReturnId) &&
            x.Field != nameof(transportation.UserReturnId)))
            {
                var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and Id eq {transportation.Id}");
                if (tran.IsLocked && transportation.IsLocked)
                {
                    var confirm = new ConfirmDialog
                    {
                        NeedAnswer = true,
                        ComType = nameof(Textbox),
                        Content = $"DSVC này đã bị khóa (Hệ thống). Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                        "Hãy nhập lý do",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        transportation.ReasonUnLockAll = confirm.Textbox?.Text;
                        await new Client(nameof(Transportation)).PostAsync<Transportation>(transportation, "RequestUnLockAll");
                    };
                }
                if (patch.Changes.Any(x => x.Field == nameof(transportation.ShipPrice) ||
                x.Field == nameof(transportation.PolicyId) ||
                x.Field == nameof(transportation.RouteId) ||
                x.Field == nameof(transportation.BrandShipId) ||
                x.Field == nameof(transportation.ShipId) ||
                x.Field == nameof(transportation.Trip) ||
                x.Field == nameof(transportation.StartShip) ||
                x.Field == nameof(transportation.ContainerTypeId) ||
                x.Field == nameof(transportation.BookingId)))
                {
                    if (transportation.LockShip)
                    {
                        var confirm = new ConfirmDialog
                        {
                            NeedAnswer = true,
                            ComType = nameof(Textbox),
                            Content = $"DSVC này đã bị khóa (Cước tàu). Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                        "Hãy nhập lý do",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            transportation.ReasonUnLockShip = confirm.Textbox?.Text;
                            await new Client(nameof(Transportation)).PostAsync<Transportation>(transportation, "RequestUnLockShip");
                        };
                    }
                }
                if (patch.Changes.Any(x => x.Field == nameof(transportation.MonthText)
                || x.Field == nameof(transportation.YearText)
                || x.Field == nameof(transportation.ExportListId)
                || x.Field == nameof(transportation.RouteId)
                || x.Field == nameof(transportation.ShipId)
                || x.Field == nameof(transportation.Trip)
                || x.Field == nameof(transportation.ClosingDate)
                || x.Field == nameof(transportation.StartShip)
                || x.Field == nameof(transportation.ContainerTypeId)
                || x.Field == nameof(transportation.ContainerNo)
                || x.Field == nameof(transportation.SealNo)
                || x.Field == nameof(transportation.BossId)
                || x.Field == nameof(transportation.UserId)
                || x.Field == nameof(transportation.CommodityId)
                || x.Field == nameof(transportation.Cont20)
                || x.Field == nameof(transportation.Cont40)
                || x.Field == nameof(transportation.Weight)
                || x.Field == nameof(transportation.ReceivedId)
                || x.Field == nameof(transportation.FreeText2)
                || x.Field == nameof(transportation.LeftDate)))
                {
                    if (transportation.IsKt)
                    {
                        var confirm = new ConfirmDialog
                        {
                            NeedAnswer = true,
                            ComType = nameof(Textbox),
                            Content = $"DSVC này đã bị khóa (Khai thác). Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                            "Hãy nhập lý do",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            transportation.ReasonUnLockExploit = confirm.Textbox?.Text;
                            await new Client(nameof(Transportation)).PostAsync<Transportation>(transportation, "RequestUnLock");
                        };
                    }
                }
                if (patch.Changes.Any(x => x.Field == nameof(transportation.LotNo)
                || x.Field == nameof(transportation.LotDate)
                || x.Field == nameof(transportation.Vat)
                || x.Field == nameof(transportation.UnitPriceAfterTax)
                || x.Field == nameof(transportation.UnitPriceBeforeTax)
                || x.Field == nameof(transportation.ReceivedPrice)
                || x.Field == nameof(transportation.CollectOnBehaftPrice)
                || x.Field == nameof(transportation.NotePayment)
                || x.Field == nameof(transportation.VendorVatId)))
                {
                    if (transportation.IsSubmit)
                    {
                        var confirm = new ConfirmDialog
                        {
                            NeedAnswer = true,
                            ComType = nameof(Textbox),
                            Content = $"DSVC này đã bị khóa (Kế toán). Bạn có muốn gửi yêu cầu mở khóa không?<br />" +
                            "Hãy nhập lý do",
                        };
                        confirm.Render();
                        confirm.YesConfirmed += async () =>
                        {
                            transportation.ReasonUnLockAccountant = confirm.Textbox?.Text;
                            await new Client(nameof(Transportation)).PostAsync<Transportation>(transportation, "RequestUnLockAccountant");
                        };
                    }
                }
            }
        }

        public async Task ApproveUnLock()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLock");
            if (gridView == null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsKt eq true");
            if (transportations.Count <= 0)
            {
                Toast.Warning("Không có cont nào bị khóa !!!");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = $"Bạn có chắc chắn muốn mở khóa cho {transportations.Count} cont không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var checks = transportations.Where(x => x.IsLocked).ToList();
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
                    var transportationNoLock = transportations.Where(x => x.IsLocked == false).ToList();
                    var res = await ApproveUnLockTransportation(transportationNoLock);
                    if (res)
                    {
                        gridView.RemoveRange(transportationNoLock);
                    }
                }
                else
                {
                    var res = await ApproveUnLockTransportation(transportations);
                    if (res)
                    {
                        gridView.RemoveRange(transportations);
                    }
                }
            };
        }

        public async Task ApproveUnLockAccountant()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockAccountant");
            if (gridView == null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsSubmit eq true");
            if (transportations.Count <= 0)
            {
                Toast.Warning("Không có cont nào bị khóa !!!");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = $"Bạn có chắc chắn muốn mở khóa cho {transportations.Count} cont không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var checks = transportations.Where(x => x.IsLocked).ToList();
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
                    var transportationNoLock = transportations.Where(x => x.IsLocked == false).ToList();
                    var res = await ApproveUnLockAccountantTransportation(transportationNoLock);
                    if (res)
                    {
                        gridView.RemoveRange(transportationNoLock);
                    }
                }
                else
                {
                    var res = await ApproveUnLockAccountantTransportation(transportations);
                    if (res)
                    {
                        gridView.RemoveRange(transportations);
                    }
                }
            };
        }

        public async Task ApproveUnLockAll()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockAll");
            if (gridView == null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsLocked eq true");
            if (transportations.Count <= 0)
            {
                Toast.Warning("Không có cont nào bị khóa !!!");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = $"Bạn có chắc chắn muốn mở khóa cho {transportations.Count} DSVC không?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                foreach (var item in transportations)
                {
                    item.IsLocked = false;
                    item.IsRequestUnLockAll = false;
                }
                var res = await new Client(nameof(Transportation)).BulkUpdateAsync<Transportation>(transportations);
                if (res != null)
                {
                    gridView.RemoveRange(transportations);
                    Toast.Success("Mở khóa thành công");
                }
                else
                {
                    Toast.Warning("Đã có lỗi xảy ra");
                }
            };
        }

        public async Task LockTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsKt eq false");
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
                    foreach (var item in transportationNoLock)
                    {
                        item.IsKt = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                    await gridView.ApplyFilter(true);
                }
                else
                {
                    foreach (var item in listViewItems)
                    {
                        item.IsKt = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                    await gridView.ApplyFilter(true);
                }
            };
        }

        public async Task LockAccountantTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsSubmit eq false");
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
                    foreach (var item in transportationNoLock)
                    {
                        item.IsSubmit = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                    await gridView.ApplyFilter(true);
                }
                else
                {
                    foreach (var item in listViewItems)
                    {
                        item.IsSubmit = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                    await gridView.ApplyFilter(true);
                }
            };
        }

        public async Task LockAllTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsLocked eq false");
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
                foreach (var item in listViewItems)
                {
                    item.IsLocked = true;
                    await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                }
                await gridView.ApplyFilter(true);
            };
        }

        public async Task UnLockTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsKt eq true");
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
                    var transportationNoLock = listViewItems.Where(x => x.IsLocked == false).ToList();
                    await UnLockTransportationNoCheck(transportationNoLock);
                    await gridView.ApplyFilter(true);
                }
                else
                {
                    await UnLockTransportationNoCheck(listViewItems);
                    await gridView.ApplyFilter(true);
                }
            };
        }

        public async Task UnLockAccountantTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsSubmit eq true");
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
                    var transportationNoLock = listViewItems.Where(x => x.IsLocked == false).ToList();
                    await UnLockAccountantTransportationNoCheck(transportationNoLock);
                    await gridView.ApplyFilter(true);
                }
                else
                {
                    await UnLockAccountantTransportationNoCheck(listViewItems);
                    await gridView.ApplyFilter(true);
                }
            };
        }

        public async Task UnLockAllTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in {ids.Combine()} and IsLocked eq true");
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
                var checkRequests = listViewItems.Where(x => x.IsRequestUnLockAll).ToList();
                if (checkRequests.Count > 0)
                {
                    var confirmRequets = new ConfirmDialog
                    {
                        Content = $"Có {checkRequests.Count} DSVC cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                    };
                    confirmRequets.Render();
                    confirmRequets.YesConfirmed += async () =>
                    {
                        foreach (var item in checkRequests)
                        {
                            item.IsLocked = false;
                            item.IsRequestUnLockAll = false;
                            await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                        }
                        await gridView.ApplyFilter(true);
                    };
                    foreach (var item in listViewItems.Where(x => x.IsRequestUnLockAll == false))
                    {
                        item.IsLocked = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                    await gridView.ApplyFilter(true);
                }
                else if (checkRequests.Count <= 0)
                {
                    foreach (var item in listViewItems)
                    {
                        item.IsLocked = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                    await gridView.ApplyFilter(true);
                }
            };
        }

        public async Task<bool> ApproveUnLockTransportation(List<Transportation> transportations)
        {
            foreach (var item in transportations)
            {
                item.IsKt = false;
                item.IsRequestUnLockExploit = false;
            }
            var res = await new Client(nameof(Transportation)).BulkUpdateAsync<Transportation>(transportations);
            if (res != null)
            {
                Toast.Success("Mở khóa thành công");
                return true;
            }
            else
            {
                Toast.Warning("Đã có lỗi xảy ra");
                return false;
            }
        }

        public async Task<bool> ApproveUnLockAccountantTransportation(List<Transportation> transportations)
        {
            foreach (var item in transportations)
            {
                item.IsSubmit = false;
                item.IsRequestUnLockAccountant = false;
            }
            var res = await new Client(nameof(Transportation)).BulkUpdateAsync<Transportation>(transportations);
            if (res != null)
            {
                Toast.Success("Mở khóa thành công");
                return true;
            }
            else
            {
                Toast.Warning("Đã có lỗi xảy ra");
                return false;
            }
        }

        public async Task UnLockTransportationNoCheck(List<Transportation> transportations)
        {
            var checkRequests = transportations.Where(x => x.IsRequestUnLockExploit).ToList();
            if (checkRequests.Count > 0)
            {
                var confirmRequets = new ConfirmDialog
                {
                    Content = $"Có {checkRequests.Count} DSVC cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                };
                confirmRequets.Render();
                confirmRequets.YesConfirmed += async () =>
                {
                    foreach (var item in checkRequests)
                    {
                        item.IsKt = false;
                        item.IsRequestUnLockExploit = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                };
                foreach (var item in transportations.Where(x => x.IsRequestUnLockExploit == false))
                {
                    item.IsKt = false;
                    await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                }
            }
            else if (checkRequests.Count <= 0)
            {
                foreach (var item in transportations)
                {
                    item.IsKt = false;
                    await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                }
            }
        }

        public async Task UnLockAccountantTransportationNoCheck(List<Transportation> transportations)
        {
            var checkRequests = transportations.Where(x => x.IsRequestUnLockAccountant).ToList();
            if (checkRequests.Count > 0)
            {
                var confirmRequets = new ConfirmDialog
                {
                    Content = $"Có {checkRequests.Count} DSVC cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                };
                confirmRequets.Render();
                confirmRequets.YesConfirmed += async () =>
                {
                    foreach (var item in checkRequests)
                    {
                        item.IsSubmit = false;
                        item.IsRequestUnLockAccountant = false;
                        var rs = await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                    }
                };
                foreach (var item in transportations.Where(x => x.IsRequestUnLockAccountant == false))
                {
                    item.IsSubmit = false;
                    var rs = await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                }
            }
            else if (checkRequests.Count <= 0)
            {
                foreach (var item in transportations)
                {
                    item.IsSubmit = false;
                    var rs = await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchEntity(item));
                }
            }
        }

        private int calcRevenue;

        public void CalcRevenue(Revenue revenue)
        {
            Window.ClearTimeout(calcRevenue);
            calcRevenue = Window.SetTimeout(async () =>
            {
                await CalcRevenueAsync(revenue);
            }, 500);
        }

        public async Task CalcRevenueAsync(Revenue revenue)
        {
            var gridView = this.FindComponentByName<GridView>(nameof(Revenue));
            if (gridView is null)
            {
                return;
            }
            revenue.Vat = revenue.Vat == null ? 10 : revenue.Vat;
            revenue.TotalPriceBeforTax = Math.Round(revenue.TotalPrice == null ? 0 : (decimal)revenue.TotalPrice / (1 + ((decimal)revenue.Vat / 100)));
            revenue.VatPrice = Math.Round(revenue.TotalPriceBeforTax == null ? 0 : (decimal)revenue.TotalPriceBeforTax * (decimal)revenue.Vat / 100);
            var res = await new Client(nameof(Revenue)).PatchAsync<Revenue>(GetPatchEntityCalcRevenue(revenue));
            if (res != null)
            {
                await gridView.ApplyFilter(true);
            }
        }

        public async Task ReloadRevenue(Transportation transportation)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Revenue));
            grid.DataSourceFilter = $"?$filter=Active eq true and TransportationId eq {transportation.Id}";
            selected = transportation;
            await grid.ApplyFilter(true);
        }

        public virtual void BeforeCreatedRevenue(Revenue revenue)
        {
            if (selected is null)
            {
                Toast.Warning("Vui lòng chọn cont cần nhập");
                return;
            }
            revenue.TransportationId = selected.Id;
            revenue.Id = 0;
        }

        public PatchUpdate GetPatchEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsLocked), Value = transportation.IsLocked.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsKt), Value = transportation.IsKt.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsSubmit), Value = transportation.IsSubmit.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsRequestUnLockAll), Value = transportation.IsRequestUnLockAll.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsRequestUnLockExploit), Value = transportation.IsRequestUnLockExploit.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsRequestUnLockAccountant), Value = transportation.IsRequestUnLockAccountant.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntityCalcRevenue(Revenue revenue)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = revenue.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.Vat), Value = revenue.Vat.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.TotalPriceBeforTax), Value = revenue.TotalPriceBeforTax.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.VatPrice), Value = revenue.VatPrice.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.TotalPrice), Value = revenue.TotalPrice.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}