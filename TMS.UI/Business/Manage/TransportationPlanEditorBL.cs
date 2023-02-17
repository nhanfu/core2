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
using static Retyped.googlemaps.Literals.Types;

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanEditorBL : PopupEditor
    {
        public TransportationPlan transportationPlanEntity => Entity as TransportationPlan;
        public TransportationPlanEditorBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan Editor";

        }

        public void SetGridView()
        {
            ToggleApprovalBtn(null);
        }

        protected override void ToggleApprovalBtn(object entity = null)
        {
            var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            this.SetShow(false, "btnSave", "btnCreate", "btnApprove", "btnSend", "btnReject");
            if (_gridView.RowData.Data.Nothing())
            {
                this.SetShow(true, "btnCreate");
                return;
            }
            var listViewItem = _gridView.RowData.Data.Cast<TransportationPlan>().OrderByDescending(x => x.Id).FirstOrDefault();
            if (listViewItem is null)
            {
                this.SetShow(true, "btnCreate");
                return;
            }
            if (listViewItem.StatusId == (int)ApprovalStatusEnum.New || listViewItem.StatusId == (int)ApprovalStatusEnum.Rejected)
            {
                this.SetShow(true, "btnSend");
            }
            else if (listViewItem.StatusId == (int)ApprovalStatusEnum.Approving)
            {
                this.SetShow(true, "btnApprove", "btnReject");
            }
            else if (listViewItem.StatusId == (int)ApprovalStatusEnum.Approved || listViewItem is null)
            {
                this.SetShow(true, "btnCreate");
            }
        }

        public void CalcContEditor()
        {
            if (transportationPlanEntity.TotalContainerUsing > transportationPlanEntity.TotalContainer)
            {
                Toast.Warning("Số Container sử dụng không được lớn hơn số lượng Container");
                transportationPlanEntity.TotalContainerUsing = transportationPlanEntity.TotalContainer;
            }
            transportationPlanEntity.TotalContainerRemain = transportationPlanEntity.TotalContainer - transportationPlanEntity.TotalContainerUsing;
            UpdateView();
        }

        private void CompareChanges(object change, object cutting)
        {
            if (change != null)
            {
                var listItem = change.GetType().GetProperties();
                var content = this.FindComponentByName<Section>("Wrapper1");
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = gridView.GetListViewItems(change).FirstOrDefault();
                content.FilterChildren(x => true).ForEach(x => x.ParentElement.RemoveClass("bg-warning"));
                listViewItem.FilterChildren(x => true).ForEach(x => x.Element.RemoveClass("text-warning"));
                foreach (var item in listItem)
                {
                    var a1 = change[item.Name];
                    var a2 = cutting[item.Name];
                    if (a1 == null && a2 == null)
                    {
                        continue;
                    }

                    if (a1 != null && a2 == null || a1 == null && a2 != null || a1 != null && a2 != null && a1.ToString() != a2.ToString())
                    {
                        content.FilterChildren(x => x.Name == item.Name).ForEach(x =>
                        {
                            x.ParentElement.AddClass("bg-warning");
                        });
                        listViewItem.FilterChildren(x => x.Name == item.Name).FirstOrDefault()?.Element?.AddClass("text-warning");
                    }
                }
            }
        }

        public void SelectedCompare(TransportationPlan transportationPlan)
        {
            CompareChanges(transportationPlan, transportationPlanEntity);
        }

        public async Task CreateRequestChange(TransportationPlan transportationPlan)
        {
            var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var checkExist = await new Client(nameof(TransportationPlan)).FirstOrDefaultAsync<TransportationPlan>($"?$orderby=Id desc&$filter=RequestChangeId eq {transportationPlan.Id} and StatusId in ({(int)ApprovalStatusEnum.New},{(int)ApprovalStatusEnum.Approving})");
            if (checkExist != null)
            {
                Toast.Warning("Có kế yêu cầu thay đổi đã được tạo trước đó vui lòng thay đổi ở dưới là gửi đi");
                return;
            }
            var requestChange = new TransportationPlan();
            requestChange.CopyPropFrom(transportationPlan);
            requestChange.Id = 0;
            requestChange.StatusId = (int)ApprovalStatusEnum.New;
            requestChange.RequestChangeId = transportationPlan.Id;
            var rs = await new Client(nameof(TransportationPlan)).CreateAsync(requestChange);
            await _gridView.ApplyFilter();
            _gridView.AllListViewItem.ForEach(x => x.SetDisabled(false));
        }

        public override async Task RequestApprove()
        {
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return;
            }
            var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = _gridView.RowData.Data.Cast<TransportationPlan>().FirstOrDefault(x => x.StatusId == (int)ApprovalStatusEnum.New);
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn gửi yêu cầu phê duyệt?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and TransportationPlanId eq {transportationPlanEntity.Id}");
                if (transportations != null)
                {
                    var transportationIds = transportations.Select(x => x.Id).ToList();
                    var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'Bảo hiểm')");
                    var expenseSOCType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'BH SOC')");
                    var expenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and TransportationId in ({transportationIds.Combine()}) and ExpenseTypeId in ({expenseType.Id}, {expenseSOCType.Id})");
                    if (expenses != null)
                    {
                        var expensesPurchasedInsurance = expenses.Where(x => x.IsPurchasedInsurance).ToList();
                        if (expensesPurchasedInsurance.Count > 0)
                        {
                            var confirmExpenses = new ConfirmDialog
                            {
                                Content = $"Đã có {expensesPurchasedInsurance.Count}/{expenses.Count} cont được mua BH, bạn có chắc chắn muốn gửi yêu cầu cập nhật thông tin không?",
                            };
                            confirmExpenses.Render();
                            confirmExpenses.YesConfirmed += async () =>
                            {
                                Entity.ClearReferences();
                                if (Entity[IdField].As<int>() <= 0)
                                {
                                    await Save(listViewItem);
                                }
                                var res = await RequestApprove(listViewItem);
                                ProcessEnumMessage(res);
                            };
                        }
                        else
                        {
                            Entity.ClearReferences();
                            if (Entity[IdField].As<int>() <= 0)
                            {
                                await Save(listViewItem);
                            }
                            var res = await RequestApprove(listViewItem);
                            ProcessEnumMessage(res);
                        }
                    }
                }
            };
        }

        public async Task ApproveRequestChange()
        {
            var isValid = await IsFormValid();
            if (!isValid)
            {
                return;
            }
            var confirm = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn phê duyệt?",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = _gridView.RowData.Data.Cast<TransportationPlan>().FirstOrDefault(x => x.StatusId == (int)ApprovalStatusEnum.Approving);
                await Approve(listViewItem);
                listViewItem.ClearReferences();
                Window.SetTimeout(async () =>
                {
                    try
                    {
                        var containerTypeId = await CheckContainerType(listViewItem);
                        var commodidtyValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {listViewItem.BossId} and CommodityId eq {listViewItem.CommodityId} and ContainerId eq {containerTypeId}");
                        if (commodidtyValue is null && listViewItem.BossId != null && listViewItem.CommodityId != null && listViewItem.ContainerTypeId != null && listViewItem.IsCompany == false)
                        {
                            var newCommodityValue = await CreateCommodityValue(listViewItem);
                            await new Client(nameof(CommodityValue)).CreateAsync<CommodityValue>(newCommodityValue);
                        }
                        var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and TransportationPlanId eq {transportationPlanEntity.Id}");
                        if (transportations != null)
                        {
                            var transportationIds = transportations.Select(x => x.Id).ToList();
                            var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'Bảo hiểm')");
                            var expenseSOCType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'BH SOC')");
                            var expenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and TransportationId in ({transportationIds.Combine()}) and ExpenseTypeId in ({expenseType.Id}, {expenseSOCType.Id}) and RequestChangeId eq null");
                            if (expenses != null)
                            {
                                await UpdateExpenses(expenses, listViewItem);
                            }
                        }
                    }
                    catch
                    {
                    }
                }, 100);
            };
        }

        private async Task UpdateExpenses(List<Expense> expenses, TransportationPlan listViewItem)
        {
            var expenseType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'Bảo hiểm')");
            foreach (var item in expenses)
            {
                item.TransportationTypeId = listViewItem.TransportationTypeId;
                item.BossId = listViewItem.BossId;
                item.SaleId = listViewItem.UserId;
                item.CommodityId = item.ExpenseTypeId == expenseType.Id ? listViewItem.CommodityId : item.CommodityId;
                item.RouteId = listViewItem.RouteId;
                item.ContainerTypeId = listViewItem.ContainerTypeId;
                if (item.JourneyId == 12114 || item.JourneyId == 16001)
                {
                    item.StartShip = listViewItem.ClosingDate;
                }
                if (item.IsPurchasedInsurance == false)
                {
                    item.IsWet = listViewItem.IsWet;
                    item.IsBought = listViewItem.IsBought;
                    item.CustomerTypeId = listViewItem.CustomerTypeId;
                    item.JourneyId = listViewItem.JourneyId;
                    item.CommodityValue = listViewItem.CommodityValue;
                    item.IsCompany = listViewItem.IsCompany;
                    if (item.ExpenseTypeId == expenseType.Id)
                    {
                        await CalcInsuranceFees(item, false);
                    }
                    else
                    {
                        await CalcInsuranceFees(item, true);
                    }
                    await new Client(nameof(Expense)).UpdateAsync<Expense>(item);
                }
                else
                {
                    var requestChange = new Expense();
                    requestChange.CopyPropFrom(item);
                    requestChange.Id = 0;
                    requestChange.StatusId = (int)ApprovalStatusEnum.New;
                    requestChange.RequestChangeId = item.Id;
                    item.StatusId = (int)ApprovalStatusEnum.Approving;
                    await new Client(nameof(Expense)).PatchAsync<Expense>(GetPatchEntityApprove(item));
                    await new Client(nameof(Expense)).CreateAsync(requestChange);
                    var res = await new Client(nameof(Expense)).PostAsync<bool>(requestChange, "RequestApprove");
                }
            }
        }

        public override void Reject()
        {
            var confirm = new ConfirmDialog
            {
                NeedAnswer = true,
                ComType = nameof(Textbox),
                Content = $"Bạn có chắc chắn muốn trả về?<br />" +
                    "Hãy nhập lý do trả về",
            };
            confirm.Render();
            confirm.YesConfirmed += async () =>
            {
                var _gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = _gridView.RowData.Data.Cast<TransportationPlan>().FirstOrDefault(x => x.StatusId == (int)ApprovalStatusEnum.Approving);
                listViewItem.ClearReferences();
                var res = await Client.CreateAsync<object>(listViewItem, "Reject?reasonOfChange=" + confirm.Textbox?.Text);
                ProcessEnumMessage(res);
            };
        }

        public void Analysis(TransportationPlan transportationPlan)
        {
            if (transportationPlan.TransportationTypeId is null || transportationPlan.BossId is null || transportationPlan.CommodityId is null || transportationPlan.ContainerTypeId is null || transportationPlan.RouteId is null)
            {
                return;
            }
            Window.ClearTimeout(Awaiter);
            Awaiter = Window.SetTimeout(async () =>
            {
                var containerId = await CheckContainerType(transportationPlan);
                var commodityValueDB = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {transportationPlan.BossId} and CommodityId eq {transportationPlan.CommodityId} and ContainerId eq {containerId}");
                if (commodityValueDB != null && transportationPlan.TransportationTypeId != null)
                {
                    if (commodityValueDB != null)
                    {
                        transportationPlan.JourneyId = commodityValueDB.JourneyId;
                        transportationPlan.IsWet = commodityValueDB.IsWet;
                    }
                    else
                    {
                        if (transportationPlan.TransportationTypeId != null)
                        {
                            if (transportationPlan.TransportationTypeId != 11673)
                            {
                                transportationPlan.JourneyId = 12114;
                                transportationPlan.IsWet = false;
                            }
                            else
                            {
                                transportationPlan.JourneyId = null;
                            }
                        }
                    }
                }
                if (commodityValueDB != null)
                {
                    transportationPlan.IsBought = commodityValueDB.IsBought;
                    transportationPlan.CustomerTypeId = commodityValueDB.CustomerTypeId;
                    transportationPlan.CommodityValue = commodityValueDB.TotalPrice;
                    Toast.Success("GTHH đã tồn tại trong hệ thống với giá trị là: " + decimal.Parse(transportationPlan.CommodityValue.ToString()).ToString("N0"));
                }
                await new Client(nameof(TransportationPlan)).PatchAsync<object>(GetPatchEntity(transportationPlan));
                var transportations = await new Client(nameof(Transportation)).GetRawList<Transportation>($"?$filter=Active eq true and TransportationPlanId eq {transportationPlanEntity.Id}");
                var transportationIds = transportations.Select(x => x.Id).ToList();
                var expenseTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and (contains(Name, 'Bảo hiểm') or contains(Name, 'SOC'))");
                var expenseTypeIds = expenseTypes.Select(x => x.Id).ToList();
                var expenses = await new Client(nameof(Expense)).GetRawList<Expense>($"?$filter=Active eq true and TransportationId in ({transportationIds.Combine()}) and ExpenseTypeId in ({expenseTypeIds.Combine()})");
                var checkPurchasedInsurance = expenses.Select(x => x.IsPurchasedInsurance == false).Any();
                if ((commodityValueDB == null || (transportationPlan.TransportationTypeId != null && transportationPlan.JourneyId == null)) && checkPurchasedInsurance)
                {
                    await SettingsCommodityValue(transportationPlan);
                }
            }, 500);
        }

        private async Task SettingsCommodityValue(TransportationPlan transportationPlan)
        {
            await this.OpenPopup(
                featureName: "SettingsCommodityValue",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Setting.SettingsCommodityValueBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Cấu hình bảo hiểm và GTHH";
                    instance.Entity = transportationPlan;
                    return instance;
                });
        }

        private int containerId = 0;

        public async Task<int> CheckContainerType(TransportationPlan transportationPlan)
        {
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)transportationPlan.ContainerTypeId);
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

        private int Awaiter;

        public void SetPolicy(TransportationPlan transportationPlan)
        {
            Window.ClearTimeout(Awaiter);
            Awaiter = Window.SetTimeout(async () =>
            {
                var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
                var listViewItem = gridView.GetListViewItems(transportationPlan).FirstOrDefault();
                if (transportationPlan.RouteId != null || transportationPlan.BossId != null)
                {
                    var components = new Client(nameof(GridPolicy)).GetRawList<GridPolicy>("?$filter=Id in (20268)");
                    var operators = new Client(nameof(MasterData)).GetRawList<MasterData>("?$filter=Parent/Name eq 'Operator'");
                    var settingPolicys = new Client(nameof(SettingPolicy)).GetRawList<SettingPolicy>($"?$expand=SettingPolicyDetail&$filter=TypeId eq 2 and TransportationTypeId ne 11677");
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
                        TransportationPlan check = null;
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            check = await new Client(nameof(TransportationPlan)).FirstOrDefaultAsync<TransportationPlan>($"?$filter=Active eq true and Id eq {transportationPlan.Id} and ({str}) and RequestChangeId ne null and StatusId eq 2");
                        }
                        if (check != null)
                        {
                            transportationPlan.TransportationTypeId = item.TransportationTypeId;
                            var patchModel = GetPatchEntity(transportationPlan);
                            await new Client(nameof(TransportationPlan)).PatchAsync<object>(patchModel);
                        }
                    }
                }
                Analysis(transportationPlan);
            }, 500);
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
            var insuranceFeesRateDB = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {expense.TransportationTypeId} and JourneyId eq {expense.JourneyId} and IsBought eq {expense.IsBought.ToString().ToLower()} and IsSOC eq {isSOC.ToLower()}");
            if (insuranceFeesRateDB != null)
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
                if (isSubRatio && insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
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

        private async Task<CommodityValue> CreateCommodityValue(TransportationPlan transportationPlan)
        {
            var startDate1 = new DateTime(DateTime.Now.Year, 1, 1);
            var endDate1 = new DateTime(DateTime.Now.Year, 6, 30);
            var startDate2 = new DateTime(DateTime.Now.Year, 7, 1);
            var endDate2 = new DateTime(DateTime.Now.Year, 12, 31);
            var containerId = await CheckContainerType(transportationPlan);
            var newCommodityValue = new CommodityValue();
            newCommodityValue.CopyPropFrom(transportationPlan);
            newCommodityValue.Id = 0;
            newCommodityValue.ContainerId = containerId;
            newCommodityValue.TotalPrice = (decimal)transportationPlan.CommodityValue;
            newCommodityValue.SaleId = transportationPlan.UserId;
            newCommodityValue.StartDate = DateTime.Now.Date;
            newCommodityValue.Notes = "";
            newCommodityValue.Active = true;
            newCommodityValue.InsertedDate = DateTime.Now.Date;
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

        public PatchUpdate GetPatchEntity(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.TransportationTypeId), Value = transportationPlan.TransportationTypeId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsWet), Value = transportationPlan.IsWet.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsBought), Value = transportationPlan.IsBought.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.JourneyId), Value = transportationPlan.JourneyId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.CustomerTypeId), Value = transportationPlan.CustomerTypeId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.CommodityValue), Value = transportationPlan.CommodityValue.ToString() });
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntityApprove(Expense expense)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = expense.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(Expense.StatusId), Value = expense.StatusId.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}