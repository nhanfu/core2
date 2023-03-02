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

        public virtual void ReloadMenu()
        {
            var gridView = this.FindActiveComponent<GridView>().ToList();
            if (gridView == null)
            {
                return;
            }
            var menus = new List<ContextMenuItem>();
            if (gridView.Any(x=>x.Name == "TransportationAccountant"))
            {
                var grid = gridView.Where(x => x.Name == "TransportationAccountant").FirstOrDefault();
                grid.BodyContextMenuShow += () =>
                {
                    menus.Add(new ContextMenuItem { Icon = "fal fa-ballot-check", Text = "Nhập đồng loạt DT", Click = ImportRevenueSimultaneous });
                    menus.Add(new ContextMenuItem
                    {
                        Icon = "fas fa-tasks-alt mr-1",
                        Text = "Hệ thống",
                        MenuItems = new List<ContextMenuItem>
                        {
                            new ContextMenuItem { Text = "Khóa hệ thống", Click = LockAllTransportation },
                            new ContextMenuItem { Text = "Mở khóa hệ thống", Click = UnLockAllTransportation },
                        }
                    });
                    menus.Add(new ContextMenuItem
                    {
                        Icon = "fas fa-tasks-alt mr-1",
                        Text = "Khai thác",
                        MenuItems = new List<ContextMenuItem>
                        {
                            new ContextMenuItem { Text = "Khóa khai thác", Click = LockTransportation },
                            new ContextMenuItem { Text = "Mở khóa khai thác", Click = UnLockTransportation },
                        }
                    });
                    menus.Add(new ContextMenuItem
                    {
                        Icon = "fas fa-tasks-alt mr-1",
                        Text = "Kế toán",
                        MenuItems = new List<ContextMenuItem>
                        {
                            new ContextMenuItem { Text = "Khóa kế toán", Click = LockAccountantTransportation },
                            new ContextMenuItem { Text = "Mở khóa kế toán", Click = UnLockAccountantTransportation },
                        }
                    });
                    menus.Add(new ContextMenuItem
                    {
                        Icon = "fas fa-tasks-alt mr-1",
                        Text = "Doanh thu",
                        MenuItems = new List<ContextMenuItem>
                        {
                            new ContextMenuItem { Text = "Khóa doanh thu", Click = LockRevenueTransportation },
                            new ContextMenuItem { Text = "Mở khóa doanh thu", Click = UnLockRevenueTransportation },
                        }
                    });
                    ContextMenu.Instance.MenuItems = menus;
                };
            }
            else if (gridView.Any(x => x.Name == "TransportationUnLockAll"))
            {
                var grid = gridView.Where(x => x.Name == "TransportationUnLockAll").FirstOrDefault();
                grid.BodyContextMenuShow += () =>
                {
                    menus.Add(new ContextMenuItem { Icon = "fas fa-thumbs-up mr-1", Text = "Duyệt mở khóa", Click = ApproveUnLockAll });
                    menus.Add(new ContextMenuItem { Icon = "fas fa-thumbs-down mr-1", Text = "Hủy mở khóa", Click = RejectUnLockAll });
                    ContextMenu.Instance.MenuItems = menus;
                };
            }
            else if (gridView.Any(x => x.Name == "TransportationUnLock"))
            {
                var grid = gridView.Where(x => x.Name == "TransportationUnLock").FirstOrDefault();
                grid.BodyContextMenuShow += () =>
                {
                    menus.Add(new ContextMenuItem { Icon = "fas fa-thumbs-up mr-1", Text = "Duyệt mở khóa", Click = ApproveUnLock });
                    menus.Add(new ContextMenuItem { Icon = "fas fa-thumbs-down mr-1", Text = "Hủy mở khóa", Click = RejectUnLock });
                    ContextMenu.Instance.MenuItems = menus;
                };
            }
            else if (gridView.Any(x => x.Name == "TransportationUnLockAccountant"))
            {
                var grid = gridView.Where(x => x.Name == "TransportationUnLockAccountant").FirstOrDefault();
                grid.BodyContextMenuShow += () =>
                {
                    menus.Add(new ContextMenuItem { Icon = "fas fa-thumbs-up mr-1", Text = "Duyệt mở khóa", Click = ApproveUnLockAccountant });
                    menus.Add(new ContextMenuItem { Icon = "fas fa-thumbs-down mr-1", Text = "Hủy mở khóa", Click = RejectUnLockAccountant });
                    ContextMenu.Instance.MenuItems = menus;
                };
            }
            else
            {
                return;
            }
        }

        public void ImportRevenueSimultaneous(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant" || x.GuiInfo.FieldName == "Revenue");
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
                        instance.Entity = new Revenue();
                        return instance;
                    });
            });
        }

        public async Task ReloadExpense(Transportation transportation)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Expense));
            grid.DataSourceFilter = $"?$filter=Active eq true and TransportationId eq {transportation.Id} and ((ExpenseTypeId in (15981, 15939) eq false) or IsPurchasedInsurance eq true)";
            selected = transportation;
            await grid.ApplyFilter(true);
        }

        public async Task MessageConfirmLockOrUnLock(Transportation transportation, PatchUpdate patch)
        {
            var grid = this.FindComponentByName<GridView>("TransportationAccountant");
            if (patch.Changes.Any(x => x.Field == nameof(transportation.IsLocked)))
            {
                if (transportation.IsLocked)
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn khóa hệ thống ?",
                    };
                    confirm.Render();
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsLocked = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsLockedEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
                else
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn mở khóa hệ thống ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        var checkRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and IsRequestUnLockAll eq true");
                        if (checkRequests.Count > 0)
                        {
                            var confirmRequets = new ConfirmDialog
                            {
                                Content = $"Cont này có yêu cần duyệt bạn có muốn duyệt mở khóa không ?",
                            };
                            confirmRequets.Render();
                            confirmRequets.YesConfirmed += async () =>
                            {
                                var trans = new List<Transportation>();
                                trans.Add(transportation);
                                await new Client(nameof(Transportation)).PostAsync<bool>(trans, "ApproveUnLockAll");
                            };
                        }
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsLocked = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsLockedEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
            }
            else if (patch.Changes.Any(x => x.Field == nameof(transportation.IsSubmit)))
            {
                if (transportation.IsSubmit)
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn khóa kế toán ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        await RequestUnClosing(transportation, patch);
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsSubmit = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsSubmitEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
                else
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn mở khóa kế toán ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        if (transportation.IsLocked)
                        {
                            await RequestUnClosing(transportation, patch);
                        }
                        else
                        {
                            var checkRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and IsRequestUnLockAccountant eq true");
                            if (checkRequests.Count > 0)
                            {
                                var confirmRequets = new ConfirmDialog
                                {
                                    Content = $"Cont này có yêu cần duyệt bạn có muốn duyệt mở khóa không ?",
                                };
                                confirmRequets.Render();
                                confirmRequets.YesConfirmed += async () =>
                                {
                                    var trans = new List<Transportation>();
                                    trans.Add(transportation);
                                    await new Client(nameof(Transportation)).PostAsync<bool>(trans, "ApproveUnLockAccountantTransportation");
                                };
                            }
                        }
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsSubmit = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsSubmitEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
            }
            else if (patch.Changes.Any(x => x.Field == nameof(transportation.IsKt)))
            {
                if (transportation.IsKt)
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn khóa khai thác ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        await RequestUnClosing(transportation, patch);
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsKt = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsKtEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
                else
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn mở khóa khai thác ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        if (transportation.IsLocked)
                        {
                            await RequestUnClosing(transportation, patch);
                        }
                        else
                        {
                            var checkRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and TransportationId eq {transportation.Id} and IsRequestUnLockExploit eq true");
                            if (checkRequests.Count > 0)
                            {
                                var confirmRequets = new ConfirmDialog
                                {
                                    Content = $"Cont này có yêu cần duyệt bạn có muốn duyệt mở khóa không ?",
                                };
                                confirmRequets.Render();
                                confirmRequets.YesConfirmed += async () =>
                                {
                                    var trans = new List<Transportation>();
                                    trans.Add(transportation);
                                    await new Client(nameof(Transportation)).PostAsync<bool>(trans, "ApproveUnLockTransportation");
                                };
                            }
                        }
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsKt = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsKtEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
            }
            else if (patch.Changes.Any(x => x.Field == nameof(transportation.IsLockedRevenue)))
            {
                if (transportation.IsLockedRevenue)
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn khóa doanh thu ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        await RequestUnClosing(transportation, patch);
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsLockedRevenue = false;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsLockedRevenueEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
                else
                {
                    var confirm = new ConfirmDialog
                    {
                        Content = "Bạn có chắc chắn muốn mở khóa doanh thu ?",
                    };
                    confirm.Render();
                    confirm.YesConfirmed += async () =>
                    {
                        await RequestUnClosing(transportation, patch);
                    };
                    confirm.NoConfirmed += async () =>
                    {
                        transportation.IsLockedRevenue = true;
                        await new Client(nameof(Transportation)).PatchAsync<Transportation>(GetPatchIsLockedRevenueEntity(transportation));
                        await grid.ApplyFilter(true);
                    };
                }
            }
            else
            {
                await RequestUnClosing(transportation, patch);
            }
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
            x.Field != nameof(transportation.UserReturnId) &&
            x.Field != nameof(transportation.IsLocked)))
            {
                var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and Id eq {transportation.Id}");
                if (tran.IsLocked)
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
                || x.Field == nameof(transportation.ShipDate)
                || x.Field == nameof(transportation.ReturnDate)
                || x.Field == nameof(transportation.ReturnId)
                || x.Field == nameof(transportation.FreeText3)))
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
            }
        }

        public async Task RequestUnClosingRevenue(Revenue revenue, PatchUpdate patch)
        {
            if (selected.IsLocked == false)
            {
                if (patch.Changes.Any(x => x.Field == nameof(revenue.Vat)
                    || x.Field == nameof(revenue.TotalPrice)) && selected.IsLockedRevenue == false)
                {
                    await CalcRevenueAsync(revenue);
                }
                if (selected.IsSubmit)
                {
                    if (patch.Changes.Any(x => x.Field == nameof(Revenue.Name)
                    || x.Field == nameof(Revenue.LotNo)
                    || x.Field == nameof(Revenue.LotDate)
                    || x.Field == nameof(Revenue.UnitPriceAfterTax)
                    || x.Field == nameof(Revenue.UnitPriceBeforeTax)
                    || x.Field == nameof(Revenue.ReceivedPrice)
                    || x.Field == nameof(Revenue.CollectOnBehaftPrice)
                    || x.Field == nameof(Revenue.NotePayment)
                    || x.Field == nameof(Revenue.Note)
                    || x.Field == nameof(Revenue.RevenueAdjustment)))
                    {
                        if (selected.IsSubmit)
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
                                selected.ReasonUnLockAccountant = confirm.Textbox?.Text;
                                await new Client(nameof(Transportation)).PostAsync<Transportation>(selected, "RequestUnLockAccountant");
                            };
                        }
                    }
                }
                else
                {
                    if (patch.Changes.Any(x => x.Field == nameof(revenue.UnitPriceAfterTax)
                    || x.Field == nameof(revenue.ReceivedPrice)))
                    {
                        await CalcRevenueTotalPriceAsync(revenue);
                    }
                }
            }
            else
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
                    selected.ReasonUnLockAll = confirm.Textbox?.Text;
                    await new Client(nameof(Transportation)).PostAsync<Transportation>(selected, "RequestUnLockAll");
                };
            }
        }

        public void ApproveUnLock(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLock");
                if (gridView == null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var tranRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsRequestUnLockExploit eq true");
                var tranIds = tranRequests.Select(x => x.TransportationId).ToList();
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({tranIds.Combine()}) and IsKt eq true");
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "ApproveUnLockTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportationNoLock);
                            Toast.Success("Mở khóa thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "ApproveUnLockTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportations);
                            Toast.Success("Mở khóa thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void ApproveUnLockAccountant(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockAccountant");
                if (gridView == null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var tranRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsRequestUnLockAccountant eq true");
                var tranIds = tranRequests.Select(x => x.TransportationId).ToList();
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({tranIds.Combine()}) and IsSubmit eq true");
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "ApproveUnLockAccountantTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportationNoLock);
                            Toast.Success("Mở khóa thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "ApproveUnLockAccountantTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportations);
                            Toast.Success("Mở khóa thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void ApproveUnLockAll(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockAll");
                if (gridView == null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var tranRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsRequestUnLockAll eq true");
                var tranIds = tranRequests.Select(x => x.TransportationId).ToList();
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({tranIds.Combine()}) and IsLocked eq true");
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
                    var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "ApproveUnLockAll");
                    if (res)
                    {
                        gridView.RemoveRange(transportations);
                        Toast.Success("Mở khóa thành công");
                    }
                    else
                    {
                        Toast.Warning("Đã có lỗi xảy ra");
                    }
                };
            });
        }

        public void RejectUnLock(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLock");
                if (gridView == null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var tranRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsRequestUnLockExploit eq true");
                var tranIds = tranRequests.Select(x => x.TransportationId).ToList();
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({tranIds.Combine()}) and IsKt eq true");
                if (transportations.Count <= 0)
                {
                    Toast.Warning("Không có cont nào bị khóa !!!");
                    return;
                }
                var confirm = new ConfirmDialog
                {
                    NeedAnswer = true,
                    ComType = nameof(Textbox),
                    Content = $"Bạn có chắc chắn muốn hủy yêu cầu cho {transportations.Count} cont không??<br />" +
                            "Hãy nhập lý do",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    tranRequests.ForEach(x => x.ReasonReject = confirm.Textbox?.Text);
                    await new Client(nameof(TransportationRequest)).BulkUpdateAsync<TransportationRequest>(tranRequests);
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "RejectUnLockTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportationNoLock);
                            Toast.Success("Hủy yêu cầu thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "RejectUnLockTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportations);
                            Toast.Success("Hủy yêu cầu thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void RejectUnLockAccountant(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockAccountant");
                if (gridView == null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var tranRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsRequestUnLockAccountant eq true");
                var tranIds = tranRequests.Select(x => x.TransportationId).ToList();
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({tranIds.Combine()}) and IsSubmit eq true");
                if (transportations.Count <= 0)
                {
                    Toast.Warning("Không có cont nào bị khóa !!!");
                    return;
                }
                var confirm = new ConfirmDialog
                {
                    NeedAnswer = true,
                    ComType = nameof(Textbox),
                    Content = $"Bạn có chắc chắn muốn hủy yêu cầu cho {transportations.Count} cont không??<br />" +
                            "Hãy nhập lý do",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    tranRequests.ForEach(x => x.ReasonReject = confirm.Textbox?.Text);
                    await new Client(nameof(TransportationRequest)).BulkUpdateAsync<TransportationRequest>(tranRequests);
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "RejectUnLockAccountantTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportationNoLock);
                            Toast.Success("Hủy yêu cầu thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "RejectUnLockAccountantTransportation");
                        if (res)
                        {
                            gridView.RemoveRange(transportations);
                            Toast.Success("Hủy yêu cầu thành công");
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void RejectUnLockAll(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationUnLockAll");
                if (gridView == null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var tranRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsRequestUnLockAll eq true");
                var tranIds = tranRequests.Select(x => x.TransportationId).ToList();
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({tranIds.Combine()}) and IsLocked eq true");
                if (transportations.Count <= 0)
                {
                    Toast.Warning("Không có cont nào bị khóa !!!");
                    return;
                }
                var confirm = new ConfirmDialog
                {
                    NeedAnswer = true,
                    ComType = nameof(Textbox),
                    Content = $"Bạn có chắc chắn muốn hủy yêu cầu cho {transportations.Count} cont không??<br />" +
                            "Hãy nhập lý do",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    tranRequests.ForEach(x => x.ReasonReject = confirm.Textbox?.Text);
                    await new Client(nameof(TransportationRequest)).BulkUpdateAsync<TransportationRequest>(tranRequests);
                    var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "RejectUnLockAll");
                    if (res)
                    {
                        gridView.RemoveRange(transportations);
                        Toast.Success("Hủy yêu cầu thành công");
                    }
                    else
                    {
                        Toast.Warning("Đã có lỗi xảy ra");
                    }
                };
            });
        }

        public void LockTransportation(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsKt eq false");
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "LockTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "LockTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void LockAccountantTransportation(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsSubmit eq false");
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "LockAccountantTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "LockAccountantTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void LockAllTransportation(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsLocked eq false");
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
                    var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "LockAllTransportation");
                    if (res)
                    {
                        await gridView.ApplyFilter(true);
                    }
                    else
                    {
                        Toast.Warning("Đã có lỗi xảy ra");
                    }
                };
            });
        }

        public void LockRevenueTransportation(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsLockedRevenue eq false");
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(transportationNoLock, "LockRevenueTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "LockRevenueTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void UnLockTransportation(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsKt eq true");
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
            });
        }

        public void UnLockAccountantTransportation(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsSubmit eq true");
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
            });
        }

        public void UnLockAllTransportation(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsLocked eq true");
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
                    var checkRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and TransportationId in ({listViewItems.Select(x => x.Id).Combine()}) and IsRequestUnLockAll eq true");
                    if (checkRequests.Count > 0)
                    {
                        var confirmRequets = new ConfirmDialog
                        {
                            Content = $"Có {checkRequests.Count} yêu cầu cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                        };
                        confirmRequets.Render();
                        confirmRequets.YesConfirmed += async () =>
                        {
                            var rs = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "ApproveUnLockAll");
                            if (rs)
                            {
                                await gridView.ApplyFilter(true);
                            }
                            else
                            {
                                Toast.Warning("Đã có lỗi xảy ra");
                            }
                        };
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems.Where(x => x.IsRequestUnLockAll == false), "UnLockAllTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                    else if (checkRequests.Count <= 0)
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "UnLockAllTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                        else
                        {
                            Toast.Warning("Đã có lỗi xảy ra");
                        }
                    }
                };
            });
        }

        public void UnLockRevenueTransportation(object arg)
        {
            Task.Run(async () => 
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var listViewItems = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and Id in ({ids.Combine()}) and IsLockedRevenue eq true");
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
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "UnLockRevenueTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                    }
                    else
                    {
                        var res = await new Client(nameof(Transportation)).PostAsync<bool>(listViewItems, "UnLockRevenueTransportation");
                        if (res)
                        {
                            await gridView.ApplyFilter(true);
                        }
                    }
                };
            });
        }

        public async Task UnLockTransportationNoCheck(List<Transportation> transportations)
        {
            var checkRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and TransportationId in ({transportations.Select(x => x.Id).Combine()}) and IsRequestUnLockExploit eq true");
            if (checkRequests.Count > 0)
            {
                var confirmRequets = new ConfirmDialog
                {
                    Content = $"Có {checkRequests.Count} yêu cầu cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                };
                confirmRequets.Render();
                confirmRequets.YesConfirmed += async () =>
                {
                    await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "ApproveUnLockTransportation");
                };
                await new Client(nameof(Transportation)).PostAsync<bool>(transportations.Where(x => x.IsRequestUnLockExploit == false), "UnLockTransportation");
            }
            else if (checkRequests.Count <= 0)
            {
                await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "UnLockTransportation");
            }
        }

        public async Task UnLockAccountantTransportationNoCheck(List<Transportation> transportations)
        {
            var checkRequests = await new Client(nameof(TransportationRequest)).GetRawList<TransportationRequest>($"?$filter=Active eq true and TransportationId in ({transportations.Select(x => x.Id).Combine()}) and IsRequestUnLockAccountant eq true");
            if (checkRequests.Count > 0)
            {
                var confirmRequets = new ConfirmDialog
                {
                    Content = $"Có {checkRequests.Count} yêu cầu cần duyệt bạn có muốn duyệt mở khóa toàn bộ không ?",
                };
                confirmRequets.Render();
                confirmRequets.YesConfirmed += async () =>
                {
                    await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "ApproveUnLockAccountantTransportation");
                };
                await new Client(nameof(Transportation)).PostAsync<bool>(transportations.Where(x => x.IsRequestUnLockAccountant == false), "UnLockAccountantTransportation");
            }
            else if (checkRequests.Count <= 0)
            {
                await new Client(nameof(Transportation)).PostAsync<bool>(transportations, "UnLockAccountantTransportation");
            }
        }

        public async Task CalcRevenueAsync(Revenue revenue)
        {
            revenue.Vat = revenue.Vat != null ? revenue.Vat : 10;
            revenue.TotalPriceBeforTax = Math.Round((decimal)revenue.TotalPrice / (1 + ((decimal)revenue.Vat / 100)));
            revenue.VatPrice = Math.Round((decimal)revenue.TotalPriceBeforTax * (decimal)revenue.Vat / 100);
            await new Client(nameof(Revenue)).PatchAsync<Revenue>(GetPatchEntityCalcRevenue(revenue));
        }

        public async Task CalcRevenueTotalPriceAsync(Revenue revenue)
        {
            revenue.UnitPriceAfterTax = revenue.UnitPriceAfterTax == null ? 0 : revenue.UnitPriceAfterTax;
            revenue.ReceivedPrice = revenue.ReceivedPrice == null ? 0 : revenue.ReceivedPrice;
            revenue.TotalPrice = revenue.UnitPriceAfterTax + revenue.ReceivedPrice;
            await new Client(nameof(Revenue)).PatchAsync<Revenue>(GetPatchEntityCalcRevenueTotalPrice(revenue));
            await CalcRevenueAsync(revenue);
        }

        public async Task ReloadRevenue(Transportation transportation)
        {
            var grid = this.FindComponentByName<GridView>(nameof(Revenue));
            grid.DataSourceFilter = $"?$filter=Active eq true and TransportationId eq {transportation.Id}";
            selected = transportation;
            await grid.ApplyFilter(true);
        }

        public void BeforeCreatedRevenue(Revenue revenue)
        {
            if (selected == null)
            {
                return;
            }
            if (selected.IsSubmit &&
                    (revenue.Name != null ||
                    revenue.LotNo != null || 
                    revenue.LotDate != null ||
                    (revenue.UnitPriceAfterTax != null && revenue.UnitPriceAfterTax != 0) || 
                    (revenue.UnitPriceBeforeTax != null && revenue.UnitPriceBeforeTax != 0) || 
                    (revenue.ReceivedPrice != null && revenue.ReceivedPrice != 0) || 
                    (revenue.CollectOnBehaftPrice != null && revenue.CollectOnBehaftPrice != 0) ||
                    revenue.NotePayment != null ||
                    revenue.Note != null ||
                    (revenue.RevenueAdjustment != null && revenue.RevenueAdjustment != 0)))
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
                    selected.ReasonUnLockAccountant = confirm.Textbox?.Text;
                    await new Client(nameof(Transportation)).PostAsync<Transportation>(selected, "RequestUnLockAccountant");
                };
                return;
            }
            revenue.Vat = revenue.Vat == null || revenue.Vat == 0 ? 10 : revenue.Vat;
            revenue.BossId = selected.BossId;
            revenue.ContainerNo = selected.ContainerNo;
            revenue.SealNo = selected.SealNo;
            revenue.ContainerTypeId = selected.ContainerTypeId;
            revenue.ClosingDate= selected.ClosingDate;
            revenue.TransportationId = selected.Id;
            revenue.Id = 0;
        }

        private int updateRevenue;

        public void UpdateTransportationWhenUpdateRevenue()
        {
            Window.ClearTimeout(updateRevenue);
            updateRevenue = Window.SetTimeout(async () =>
            {
                await UpdateTransportationWhenUpdateRevenueAsync();
            }, 1000);
        }

        public async Task UpdateTransportationWhenUpdateRevenueAsync()
        {
            var grid = this.FindComponentByName<GridView>("TransportationAccountant");
            await grid.ApplyFilter(true);
        }

        public async Task ExportTransportationAndRevenue()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == "TransportationAccountant");
            if (gridView is null)
            {
                return;
            }
            var listViewItems = gridView.SelectedIds.ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Bạn chưa chọn dữ liệu");
                return;
            }
            var path = await new Client(nameof(Transportation)).PostAsync<string>(listViewItems, "ExportTransportationAndRevenue");
            Client.Download($"/excel/Download/{path}");
            Toast.Success("Xuất file thành công");
        }

        public PatchUpdate GetPatchIsLockedEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsLocked), Value = transportation.IsLocked.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchIsKtEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsKt), Value = transportation.IsKt.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchIsSubmitEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsSubmit), Value = transportation.IsSubmit.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchIsLockedRevenueEntity(Transportation transportation)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportation.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Transportation.IsLockedRevenue), Value = transportation.IsLockedRevenue.ToString() });
            return new PatchUpdate { Changes = details };
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

        public PatchUpdate GetPatchEntityCalcRevenueTotalPrice(Revenue revenue)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = revenue.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.TotalPrice), Value = revenue.TotalPrice.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.UnitPriceAfterTax), Value = revenue.UnitPriceAfterTax.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Revenue.ReceivedPrice), Value = revenue.ReceivedPrice.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}