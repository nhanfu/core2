using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using Retyped.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.UI.Business.Accountant;
using static Retyped.dom.Literals.Types;
using Number = Core.Components.Number;

namespace TMS.UI.Business.Manage
{
    public class InsuranceFeesBL : TabEditor
    {
        public GridView gridView { get; set; }
        public InsuranceFeesBL() : base(nameof(Expense))
        {
            Name = "InsuranceFees List";
        }

        public async Task EditInsuranceFees(Expense entity)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView.Name.Contains("ExpenseIsDelete"))
            {
                ApproveDelete(entity);
            }
            else
            {
                await this.OpenPopup(
               featureName: "InsuranceFees Editor",
               factory: () =>
               {
                   var type = Type.GetType("TMS.UI.Business.Manage.InsuranceFeesEditorBL");
                   var instance = Activator.CreateInstance(type) as PopupEditor;
                   instance.Title = "Yêu cầu thay đổi thông tin phí bảo hiểm";
                   instance.Entity = entity;
                   return instance;
               });
            }
        }

        public async Task ViewChangeInsuranceFees(Expense entity)
        {
            await this.OpenPopup(
               featureName: "InsuranceFees View Change",
               factory: () =>
               {
                   var type = Type.GetType("TMS.UI.Business.Manage.InsuranceFeesViewChangeBL");
                   var instance = Activator.CreateInstance(type) as PopupEditor;
                   instance.Title = "Lịch sử thay đổi";
                   instance.Entity = entity;
                   return instance;
               });
        }

        public async Task UpdateDataFromTransportation()
        {
            await this.OpenPopup(
               featureName: "InsuranceFees Update Data",
               factory: () =>
               {
                   var type = Type.GetType("TMS.UI.Business.Manage.InsuranceFeesUpdateDataBL");
                   var instance = Activator.CreateInstance(type) as PopupEditor;
                   instance.Title = "Cập nhật dữ liệu";
                   instance.Entity = new Expense();
                   return instance;
               });
        }

        public async Task LockExpense()
        {
            await ChangeBackgroudColor();
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItems = gridView.RowData.Data.Cast<Expense>().ToList();
            if (gridView.Name.Contains("ExpenseIsDelete"))
            {
                listViewItems.ForEach(x =>
                {
                    var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                    if (listViewItem is null)
                    {
                        return;
                    }
                    listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
                });
            }
            else
            {
                var ids = listViewItems.Select(x => x.Id).ToList();
                var listExpenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and IsPurchasedInsurance eq true and Id in ({ids.Combine()}) and StatusId eq {(int)ApprovalStatusEnum.Approving} and RequestChangeId eq null ");
                var checkHistorys = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and RequestChangeId in ({ids.Combine()}) and StatusId eq {(int)ApprovalStatusEnum.New}");
                foreach (var item in listViewItems)
                {
                    var expenses = listExpenses.Where(x => x.Id == item.Id).ToList();
                    var checkHistory = checkHistorys.Where(x => x.RequestChangeId == item.Id).ToList();
                    UpdateListView(item, gridView, expenses, checkHistory);
                }
            }
            ReloadMenu();
        }

        public virtual void ReloadMenu()
        {
            var gridView = this.FindComponentByName<GridView>(nameof(Expense));
            if (gridView == null)
            {
                return;
            }
            var menus = new List<ContextMenuItem>();
            gridView.BodyContextMenuShow += () =>
            {
                menus.Add(new ContextMenuItem { Icon = "fas fa-download mr-1", Text = "Cập nhật TT GTHH", Click = UpdateCommodityValueOfInsuranceFees });
                ContextMenu.Instance.MenuItems = menus;
            };
        }

        public void UpdateCommodityValueOfInsuranceFees(object arg)
        {
            Task.Run(async () =>
            {
                var gridView = this.FindComponentByName<GridView>(nameof(Expense));
                if (gridView is null)
                {
                    return;
                }
                var ids = gridView.SelectedIds.ToList();
                var expenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and ExpenseTypeId in (15981, 15939) and RequestChangeId eq null and Id in ({ids.Combine()}) and IsPurchasedInsurance eq false");
                if (expenses != null)
                {
                    var bossIds = expenses.Select(x => x.BossId).ToList();
                    var commodityIds = expenses.Select(x => x.CommodityId).ToList();
                    var containerTypeIds = new Dictionary<int, int>();
                    var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
                    foreach (var item in expenses)
                    {
                        var containerType = containerTypes.Where(x => x.Id == item.ContainerTypeId).FirstOrDefault();
                        if (containerType.Description.Contains("Cont 20"))
                        {
                            containerTypeIds.Add(item.Id, containerTypes.Find(x => x.Name.Contains("20DC")).Id);
                        }
                        else if (containerType.Description.Contains("Cont 40"))
                        {
                            containerTypeIds.Add(item.Id, containerTypes.Find(x => x.Name.Contains("40HC")).Id);
                        }
                        else if (containerType.Description.Contains("Cont 45"))
                        {
                            containerTypeIds.Add(item.Id, containerTypes.Find(x => x.Name.Contains("45HC")).Id);
                        }
                        else if (containerType.Description.Contains("Cont 50"))
                        {
                            containerTypeIds.Add(item.Id, containerTypes.Find(x => x.Name.Contains("50DC")).Id);
                        }
                    }
                    var commodityValueDB = await new Client(nameof(CommodityValue)).GetRawList<CommodityValue>($"?$filter=Active eq true and BossId in ({bossIds.Combine()}) and CommodityId in ({commodityIds.Combine()})");
                    Spinner.AppendTo(this.Element, false, true, 30000);
                    var res = new Expense();
                    foreach (var item in expenses)
                    {
                        var containerTypeId = containerTypeIds.GetValueOrDefault(item.Id);
                        var commodityValue = commodityValueDB.Where(x => x.BossId == item.BossId && x.CommodityId == item.CommodityId && x.ContainerId == containerTypeId).FirstOrDefault();
                        if (commodityValue != null)
                        {
                            item.SteamingTerms = commodityValue.SteamingTerms;
                            item.BreakTerms = commodityValue.BreakTerms;
                            item.IsBought = commodityValue.IsBought;
                            item.IsWet = commodityValue.IsWet;
                            item.JourneyId = commodityValue.JourneyId;
                            item.CustomerTypeId = commodityValue.CustomerTypeId;
                            item.CommodityValue = commodityValue.TotalPrice;
                            item.CommodityValueNotes = commodityValue.Notes;
                            if (item.ExpenseTypeId == 15939)
                            {
                                await CalcInsuranceFees(item, false);
                            }
                            else if(item.ExpenseTypeId == 15981)
                            {
                                await CalcInsuranceFees(item, true);
                            }
                            res = await new Client(nameof(Expense)).UpdateAsync<Expense>(item);
                        }
                    }
                    if (res != null)
                    {
                        Spinner.Hide();
                        Toast.Success("Đã cập nhật thành công.");
                    }
                }
            });
        }

        private void UpdateListView(Expense x, GridView gridView, List<Expense> expenses, List<Expense> checkHistory)
        {
            var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
            if (listViewItem is null)
            {
                return;
            }
            if (x.IsClosing)
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "IsClosing").ForEach(y => y.Disabled = true);
            }
            else
            {
                if (expenses.Count > 0)
                {
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange").ForEach(y => y.Disabled = false);
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange").ForEach(y => y.Disabled = true);
                }
                else
                {
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange").ForEach(y => y.Disabled = true);
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange").ForEach(y => y.Disabled = false);
                }
            }
            if (checkHistory.Count > 0)
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnViewChange" || y.GuiInfo.FieldName == "IsApproveChange" || y.GuiInfo.FieldName == "NotesInsuranceFees").ForEach(y => y.Disabled = false);
            }
            else
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnViewChange" || y.GuiInfo.FieldName == "IsApproveChange").ForEach(y => y.Disabled = true);
            }
        }

        public async Task UpdateVATInsuranceFees()
        {
            var entity = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 11685");
            await this.OpenPopup(
                featureName: "UpdateVATInsuranceFees",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.UpdateVATInsuranceFeesBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa VAT phí bảo hiểm";
                    instance.Entity = entity;
                    return instance;
                });
        }

        private int commodityAwaiter;

        public async Task UpdateCommodityValue(Expense expense)
        {
            var commodity = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and contains(Path, '7651') and contains(Description, 'Vỏ rỗng')");
            if (expense.CommodityId == commodity.Id)
            {
                return;
            }
            Window.ClearTimeout(commodityAwaiter);
            commodityAwaiter = Window.SetTimeout(async () =>
            {
                await UpdateCommodityAsync(expense);
            }, 500);
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

        private async Task UpdateCommodityAsync(Expense expense)
        {
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
                        var newCommodityValue = CreateCommodityValue(expense);
                        newCommodityValue.TotalPrice = (decimal)expense.CommodityValue;
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
                            await new Client(nameof(CommodityValue)).UpdateAsync(commodityValueDB);
                            var newCommodityValue = CreateCommodityValue(expense);
                            newCommodityValue.TotalPrice = (decimal)expense.CommodityValue;
                            await new Client(nameof(CommodityValue)).CreateAsync(newCommodityValue);
                        };
                    }
                    await CalcInsuranceFees(expense, false);
                    await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchEntity(expense));
                }
            }
        }

        private CommodityValue CreateCommodityValue(Expense expense)
        {
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
            var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var newCommodityValue = new CommodityValue();
            newCommodityValue.CopyPropFrom(expense);
            newCommodityValue.Id = 0;
            newCommodityValue.ContainerId = containerId;
            newCommodityValue.Notes = expense.CommodityValueNotes;
            newCommodityValue.TotalPrice = 0;
            newCommodityValue.Active = true;
            newCommodityValue.CreatedBy = Client.Token.UserId;
            newCommodityValue.InsertedDate = DateTime.Now.Date;
            newCommodityValue.StartDate = DateTime.Now.Date;
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

        private async Task CalcInsuranceFees(Expense expense, bool isSOC)
        {
            bool isSubRatio = false;
            if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
            {
                isSubRatio = true;
            }
            var journeyId = expense.JourneyId is null ? "" : "and JourneyId eq " + expense.JourneyId.ToString();
            InsuranceFeesRate insuranceFeesRateDB = null;
            if (expense.IsBought)
            {
                insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} {journeyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSOC eq {isSOC.ToString().ToLower()} and IsSubRatio eq {isSubRatio.ToString().ToLower()}");
            }
            else
            {
                insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} {journeyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSOC eq {isSOC.ToString().ToLower()}");
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
                    if (insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
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

        public void UpdateDatePurchasedInsurance(Expense expense)
        {
            gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Expense));
            var listViewItem = gridView.GetListViewItems(expense).FirstOrDefault();
            if (expense.IsPurchasedInsurance)
            {
                expense.DatePurchasedInsurance = DateTime.Now.Date;
                listViewItem.UpdateView();
                listViewItem.FilterChildren<Datepicker>(x => x.GuiInfo.FieldName == nameof(Expense.DatePurchasedInsurance)).ToList().ForEach(x => x.Dirty = true);
                listViewItem.Element.AddClass("bg-host");
            }
            else
            {
                var confirm = new ConfirmDialog
                {
                    Content = "Bạn có chắc chắn muốn bỏ mua BH?",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    expense.IsPurchasedInsurance = false;
                    await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchIsPurchasedInsuranceEntity(expense), ig: "true");
                    listViewItem.UpdateView(false, nameof(Expense.IsPurchasedInsurance));
                    listViewItem.Element.RemoveClass("bg-host");
                };
                confirm.NoConfirmed += async () =>
                {
                    expense.IsPurchasedInsurance = true;
                    await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchIsPurchasedInsuranceEntity(expense), ig: "true");
                    listViewItem.UpdateView(false, nameof(Expense.IsPurchasedInsurance));
                };
            }
        }

        public async Task UpdateIsClosing(Expense expense)
        {
            gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.Name == nameof(Expense));
            var listViewItem = gridView.GetListViewItems(expense).FirstOrDefault();
            if (expense.IsClosing)
            {
                if (expense.IsPurchasedInsurance == false)
                {
                    expense.IsPurchasedInsurance = true;
                    expense.DatePurchasedInsurance = DateTime.Now.Date;
                    listViewItem.UpdateView();
                    listViewItem.FilterChildren<Checkbox>(x => x.GuiInfo.FieldName == nameof(Expense.IsPurchasedInsurance)).ToList().ForEach(x => x.Dirty = true);
                    listViewItem.FilterChildren<Datepicker>(x => x.GuiInfo.FieldName == nameof(Expense.DatePurchasedInsurance)).ToList().ForEach(x => x.Dirty = true);
                    await listViewItem.PatchUpdate();
                    listViewItem.Element.AddClass("bg-host");
                }
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "IsClosing").ForEach(y => y.Disabled = true);
            }
            else
            {
                var confirm = new ConfirmDialog
                {
                    Content = "Bạn có chắc chắn muốn bỏ chốt BH?",
                };
                confirm.Render();
                confirm.YesConfirmed +=  async () =>
                {
                    expense.IsClosing = false;
                    var res = await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchIsClosingEntity(expense), ig: "true");
                    listViewItem.UpdateView(false, nameof(Expense.IsClosing));
                };
                confirm.NoConfirmed += async () =>
                {
                    expense.IsClosing = true;
                    await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchIsClosingEntity(expense), ig: "true");
                    listViewItem.UpdateView(false, nameof(Expense.IsClosing));
                };
            }
        }

        public async Task ChangeBackgroudColor()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            if (gridView is null)
            {
                return;
            }
            var listViewItems = gridView.RowData.Data.Cast<Expense>().ToList();
            foreach (var item in listViewItems)
            {
                var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                if (listViewItem is null)
                {
                    return;
                }
                if (listViewItem != null)
                {
                    var checkHistory = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and RequestChangeId eq {item.Id} and StatusId eq 2");
                    if (checkHistory.Count > 0)
                    {
                        listViewItem.Element.AddClass("bg-red1");
                    }
                    else
                    {
                        listViewItem.Element.RemoveClass("bg-red1");
                    }
                    if (item.IsPurchasedInsurance)
                    {
                        listViewItem.Element.AddClass("bg-host");
                    }
                    else
                    {
                        listViewItem.Element.RemoveClass("bg-host");
                    }
                }
            }
        }

        public void SetPurchasedForExpenses()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Expense));
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var expenses = gridView.GetSelectedRows().Cast<Expense>().ToList();
            var listViewItems = expenses.Where(x => x.IsPurchasedInsurance == false).ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Bạn chưa chọn dữ liệu");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn mua BH cho " + listViewItems.Count() + " Cont ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var res = await new Client(nameof(Expense)).PostAsync<bool>(ids, "PurchasedInsuranceFees");
                if (res)
                {
                    await gridView.ApplyFilter(true);
                    foreach (var item in listViewItems)
                    {
                        var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                        if (listViewItem != null)
                        {
                            listViewItem.Element.AddClass("bg-host");
                        }
                    }
                    Toast.Success("Đã cập nhật thành công");
                }
                else
                {
                    Toast.Warning("Đã có lỗi xảy ra");
                }
            };
        }

        public void SetClosingForExpenses()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(Expense));
            if (gridView is null)
            {
                return;
            }
            var ids = gridView.SelectedIds.ToList();
            var expenses = gridView.GetSelectedRows().Cast<Expense>().ToList();
            var listViewItems = expenses.Where(x => x.IsClosing == false).ToList();
            if (listViewItems.Count <= 0)
            {
                Toast.Warning("Bạn chưa chọn dữ liệu");
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn chốt BH cho " + listViewItems.Count() + " Cont ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var res = await new Client(nameof(Expense)).PostAsync<bool>(expenses, "ClosingInsuranceFees");
                if (res)
                {
                    await gridView.ApplyFilter(true);
                    foreach (var item in listViewItems)
                    {
                        var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
                        if (listViewItem != null)
                        {
                            listViewItem.Element.AddClass("bg-host");
                        }
                    }
                    Toast.Success("Đã cập nhật thành công");
                }
                else
                {
                    Toast.Warning("Đã có lỗi xảy ra");
                }
            };
        }

        private int awaiter;

        public void ApproveDelete(Expense expense)
        {
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn duyệt yêu cầu hủy ?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var transportation = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($"?$filter=Active eq true and Id eq {expense.TransportationId}");
                var expenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and TransportationId eq {expense.TransportationId} and RequestChangeId eq null");
                foreach (var x in expenses)
                {
                    if (x.IsClosing && x.IsPurchasedInsurance)
                    {
                        var confirmDel = new ConfirmDialog
                        {
                            Content = "Đã có phí BH được mua và chốt, bạn có muốn tiếp tục duyệt và tìm cont thay thế không ?",
                        };
                        confirmDel.Render();
                        confirmDel.YesConfirmed += async () =>
                        {
                            if (x.TransportationTypeId == null || x.CustomerTypeId == null || x.JourneyId == null)
                            {
                                Toast.Warning("Không tìm thấy cont phù hợp!");
                            }
                            var saleFilter = x.SaleId == null ? "" : $"and SaleId eq {x.SaleId}";
                            var findReplace = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and IsPurchasedInsurance eq false and TransportationTypeId eq {x.TransportationTypeId} and IsWet eq {x.IsWet.ToString().ToLower()} and IsBought eq {x.IsBought.ToString().ToLower()} and JourneyId eq {x.JourneyId} and CustomerTypeId eq {x.CustomerTypeId} {saleFilter} and Id ne {x.Id}");
                            if (findReplace == null)
                            {
                                findReplace = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and IsPurchasedInsurance eq false and TransportationTypeId eq {x.TransportationTypeId} and IsWet eq {x.IsWet.ToString().ToLower()} and IsBought eq {x.IsBought.ToString().ToLower()} and JourneyId eq {x.JourneyId} and CustomerTypeId eq {x.CustomerTypeId} and Id ne {x.Id}");
                            }
                            if (findReplace.Count <= 0)
                            {
                                Toast.Warning("Không tìm thấy cont phù hợp!");
                            }
                            else
                            {
                                var selectExpenseReplace = findReplace.OrderBy(item => Math.Abs((int)x.CommodityValue - (int)item.CommodityValue)).FirstOrDefault();
                                var expenseReplace = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($"?$filter=Active eq true and Id eq {selectExpenseReplace.Id}");
                                var confirmReplace = new ConfirmDialog
                                {
                                    Content = $"Đã tìm thấy cont Id: {expenseReplace.Id} có GTHH " + decimal.Parse(expenseReplace.CommodityValue.ToString()).ToString("N0") + ". Bạn có muốn thay thế không?",
                                };
                                confirmReplace.Render();
                                confirmReplace.YesConfirmed += async () =>
                                {
                                    expenseReplace.IsPurchasedInsurance = true;
                                    expenseReplace.DatePurchasedInsurance = x.DatePurchasedInsurance;
                                    var bossName = await new Client(nameof(Vendor)).FirstOrDefaultAsync<Vendor>($"?$filter=Active eq true and Id eq {x.BossId}");
                                    var commodityName = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {x.CommodityId}");
                                    var saleName = await new Client(nameof(TMS.API.Models.User)).FirstOrDefaultAsync<TMS.API.Models.User>($"?$filter=Active eq true and Id eq {x.CommodityId}");
                                    var note1 = x.StartShip == null ? "" : x.StartShip.Value.Date.ToString("dd/MM/yyyy");
                                    var note2 = bossName == null ? "" : bossName.Name;
                                    var note3 = commodityName == null ? "" : commodityName.Description;
                                    var note4 = saleName == null ? "" : saleName.FullName;
                                    var note5 = x.DatePurchasedInsurance == null ? "" : x.DatePurchasedInsurance.Value.Date.ToString("dd/MM/yyyy");
                                    expenseReplace.NotesInsuranceFees = $"Thay thế cont bị hủy: Id: {x.Id}, đóng ngày: " + note1 + ", chủ hàng: " + note2 + ", vật tư: " + note3 + ", sale: " + note4 + ", mua ngày: " + note5;
                                    var resUpdate = await new Client(nameof(Expense)).UpdateAsync<Expense>(expenseReplace);
                                    if (resUpdate != null)
                                    {
                                        Toast.Success("Thay thế thành công!");
                                        var res = await new Client(nameof(Expense)).HardDeleteAsync(x.Id);
                                        if (res)
                                        {
                                            var check = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and TransportationId eq {expense.TransportationId} and RequestChangeId eq null");
                                            if (check.Count <= 0)
                                            {
                                                var resTr = await new Client(nameof(Transportation)).HardDeleteAsync(transportation.Id);
                                                if (resTr) { Toast.Success("Hủy thành công"); } else { Toast.Warning("Đã xảy ra lỗi trong quá trình xử lý."); }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Toast.Warning("Đã xảy ra lỗi trong quá trình xử lý.");
                                    }
                                };
                            }
                        };
                    }
                    else
                    {
                        await new Client(nameof(Expense)).HardDeleteAsync(x.Id);
                    }
                }
                Window.ClearTimeout(awaiter);
                awaiter = Window.SetTimeout(async () =>
                {
                    var check = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and TransportationId eq {expense.TransportationId} and RequestChangeId eq null");
                    if (check.Count <= 0)
                    {
                        var resTr = await new Client(nameof(Transportation)).HardDeleteAsync(transportation.Id);
                        if (resTr) { Toast.Success("Hủy thành công"); } else { Toast.Warning("Đã xảy ra lỗi trong quá trình xử lý."); }
                    }
                }, 1000);
            };
        }

        public async Task ExportCheckChange()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.RefName == nameof(Expense));
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
            var path = await new Client(nameof(Expense)).PostAsync<string>(listViewItems, "ExportCheckChange");
            Client.Download($"/excel/Download/{path}");
            Toast.Success("Xuất file thành công");
        }

        public async Task BeforePatchUpdate(Expense entity, PatchUpdate patch)
        {
            var oldEntity = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($"?$filter=Active eq true and Id eq {entity.Id}");
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.ExpenseTypeId)) &&
            (oldEntity.ExpenseTypeId != entity.ExpenseTypeId))
            {
                var expenseTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($@"?$filter=Active eq true and ParentId eq 7577 and (contains(Name,'Bảo hiểm') or contains(Name,'BH SOC'))");
                if (expenseTypes.Where(x => x.Id == entity.ExpenseTypeId).Any())
                {
                    var check = await new Client(nameof(Expense)).FirstOrDefaultAsync<Expense>($@"?$filter=Active eq true and TransportationId eq {entity.TransportationId} and ExpenseTypeId eq {entity.ExpenseTypeId} and RequestChangeId eq null");
                    if (check != null && entity.RequestChangeId == null)
                    {
                        return;
                    }
                }
            }
            if (patch.Changes.Any(x => x.Field == nameof(oldEntity.IsWet) ||
            x.Field == nameof(oldEntity.SteamingTerms) ||
            x.Field == nameof(oldEntity.BreakTerms)) &&
            (oldEntity.IsWet != entity.IsWet) ||
            (oldEntity.SteamingTerms != entity.SteamingTerms) ||
            (oldEntity.BreakTerms != entity.BreakTerms))
            {
                if (entity.IsWet && entity.SteamingTerms && entity.BreakTerms)
                {
                    return;
                }
            }
            if (patch.Changes.Any(x =>
            x.Field == nameof(oldEntity.TransportationTypeId) ||
            x.Field == nameof(oldEntity.JourneyId) ||
            x.Field == nameof(oldEntity.IsWet) ||
            x.Field == nameof(oldEntity.SteamingTerms) ||
            x.Field == nameof(oldEntity.BreakTerms) ||
            x.Field == nameof(oldEntity.IsBought)) &&
            (oldEntity.TransportationTypeId != entity.TransportationTypeId) ||
            (oldEntity.JourneyId != entity.JourneyId) ||
            (oldEntity.IsWet != entity.IsWet) ||
            (oldEntity.SteamingTerms != entity.SteamingTerms) ||
            (oldEntity.BreakTerms != entity.BreakTerms) ||
            (oldEntity.IsBought != entity.IsBought))
            {
                var tran = await new Client(nameof(Transportation)).FirstOrDefaultAsync<Transportation>($@"?$filter=Active eq true and Id eq {entity.TransportationId}");
                var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($@"?$filter=Active eq true and ParentId eq 7577 and contains(Name,'Bảo hiểm')");
                var expenseTypeSOC = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($@"?$filter=Active eq true and ParentId eq 7577 and contains(Name,'BH SOC')");
                if (entity.ExpenseTypeId == expenseType.Id)
                {
                    SetWetAndJourneyForExpense(entity, tran);
                    await CalcInsuranceFees(entity, false);
                }
                else if (entity.ExpenseTypeId == expenseTypeSOC.Id)
                {
                    await CalcInsuranceFees(entity, true);
                }
                else
                {
                    return;
                }
                await new Client(nameof(Expense)).UpdateAsync<Expense>(entity);
            }
        }

        private void SetWetAndJourneyForExpense(Expense expense, Transportation transportation)
        {
            if (expense.TransportationTypeId != null)
            {
                if (expense.TransportationTypeId != 11673) //Tàu
                {
                    if (expense.IsPurchasedInsurance == false && expense.RequestChangeId == null)
                    {
                        expense.IsWet = expense.TransportationTypeId == 11677 ? true : false;
                        if (expense.TransportationTypeId != 11677)
                        {
                            expense.JourneyId = 12114;
                        }
                        else
                        {
                            expense.JourneyId = null;
                        }
                    }
                }
                if (expense.JourneyId == 12114 || expense.JourneyId == 16001)
                {
                    expense.StartShip = transportation.ClosingDate;
                }
                else
                {
                    expense.StartShip = transportation.StartShip;
                }
            }
        }

        public async Task UpdateDataForInsuranceFees()
        {
            var res = await new Client(nameof(Expense)).PostAsync<bool>(null, "UpdateDataForInsuranceFees");
            if (res)
            {
                Toast.Success("Đã cập nhật thành công");
            }
            else
            {
                Toast.Warning("Cập nhật thất bại");
            }
        }

        public PatchUpdate GetPatchEntity(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.CommodityValue), Value = expense.CommodityValue.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.InsuranceFeeRate), Value = expense.InsuranceFeeRate.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.TotalPriceBeforeTax), Value = expense.TotalPriceBeforeTax.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.TotalPriceAfterTax), Value = expense.TotalPriceAfterTax.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntityPurchased(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.IsPurchasedInsurance), Value = expense.IsPurchasedInsurance.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.DatePurchasedInsurance), Value = expense.DatePurchasedInsurance.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.IsClosing), Value = expense.IsClosing.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchIsPurchasedInsuranceEntity(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.IsPurchasedInsurance), Value = expense.IsPurchasedInsurance.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchIsClosingEntity(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.IsClosing), Value = expense.IsClosing.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}