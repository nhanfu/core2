using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using System;
using System.Collections.Generic;
using Core.Clients;
using Bridge.Html5;
using Core.MVVM;
using Core.Enums;
using Core.ViewModels;
using static Retyped.dom.Literals.Types;
using Event = Bridge.Html5.Event;
using static Retyped.es5;
using Math = System.Math;

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanListBL : TabEditor
    {
        private HTMLInputElement _uploader;
        public TransportationPlanListBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan List";
            DOMContentLoaded += () =>
            {
                Html.Take("Body").Form.Attr("method", "POST").Attr("enctype", "multipart/form-data")
                .Display(false).Input.Event(EventType.Change, async (ev) => await SelectedExcel(ev)).Type("file").Id($"id_{GetHashCode()}").Attr("name", "fileImport").Attr("accept", ".xlsx");
                _uploader = Html.Context as HTMLInputElement;
            };
        }

        public async Task EditTransportationPlan(TransportationPlan entity)
        {
            await this.OpenPopup(
                featureName: "TransportationPlan Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationPlanEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa kế hoạch vận chuyển";
                    instance.Entity = entity;
                    return instance;
                });
        }

        public async Task RequestChangeTransportationPlan(TransportationPlan entity)
        {
            await this.OpenPopup(
            featureName: "TransportationPlan Editor",
            factory: () =>
            {
                var type = Type.GetType("TMS.UI.Business.Manage.TransportationPlanEditorBL");
                var instance = Activator.CreateInstance(type) as PopupEditor;
                instance.Title = "Yêu cầu thay đổi kế hoạch vận chuyển";
                instance.Entity = entity;
                return instance;
            });
        }

        public async Task AddTransportationPlan()
        {
            await this.OpenPopup(
                featureName: "TransportationPlan Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Manage.TransportationPlanEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Thêm mới kế hoạch vận chuyển";
                    instance.Entity = new TransportationPlan();
                    return instance;
                });
        }

        public void CalcContainer(TransportationPlan transportationPlan)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(transportationPlan).FirstOrDefault();
            transportationPlan.TotalContainerUsing = transportationPlan.TotalContainerUsing ?? 0;
            transportationPlan.TotalContainerRemain = transportationPlan.TotalContainer - transportationPlan.TotalContainerUsing;
            listViewItem.FilterChildren<CellText>(x => x.GuiInfo.FieldName == nameof(TransportationPlan.TotalContainerUsing) || x.GuiInfo.FieldName == nameof(TransportationPlan.TotalContainerRemain)).ForEach(x => x.Dirty = true);
        }

        public void LockTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            gridView.BodyContextMenuShow += () =>
            {
                ContextMenu.Instance.MenuItems = new List<ContextMenuItem>
                {
                        new ContextMenuItem { Icon = "fal fa-street-view", Text = "Xem danh sách vận chuyển", Click = ViewTransportation },
                };
            };
            var listViewItems = gridView.RowData.Data.Cast<TransportationPlan>().ToList();
            listViewItems.ForEach(x =>
            {
                UpdateListView(x, gridView);
            });
        }

        private static void UpdateListView(TransportationPlan x, GridView gridView)
        {
            var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
            if (listViewItem is null)
            {
                return;
            }
            listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
            if (x.IsTransportation)
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
            }
            else
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
            }
            listViewItem.Element.RemoveClass("bg-host");
            if (x.StatusId == (int)ApprovalStatusEnum.Approving)
            {
                listViewItem.Element.AddClass("bg-host");
            }
        }

        public async Task CreateTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var selected = (await gridView.GetRealTimeSelectedRows()).Cast<TransportationPlan>().ToList();
            if (selected.Nothing())
            {
                selected = gridView.GetFocusedRows().Cast<TransportationPlan>().ToList();
            }
            if (selected.Nothing())
            {
                Toast.Warning("Vui lòng chọn kết hoạch vận chuyển");
                return;
            }
            if (selected.Any(x => x.ContainerTypeId is null))
            {
                Toast.Warning("Vui lòng chọn loại cont");
                return;
            }
            if (selected.Any(x => x.RouteId is null))
            {
                Toast.Warning("Vui lòng chọn tuyến đường");
                return;
            }
            if (selected.Any(x => x.BossId is null))
            {
                Toast.Warning("Vui lòng chọn chủ hàng");
                return;
            }
            if (selected.Any(x => x.CommodityId is null))
            {
                Toast.Warning("Vui lòng chọn vật tư hàng hóa");
                return;
            }
            if (selected.Any(x => x.TotalContainer is null || x.TotalContainer == 0))
            {
                Toast.Warning("Vui lòng nhập số lượng cont");
                return;
            }
            if (selected.Any(x => x.ReceivedId is null))
            {
                Toast.Warning("Vui lòng chọn địa chỉ nhận hàng");
                return;
            }
            if (selected.Any(x => x.ClosingDate is null))
            {
                Toast.Warning("Vui lòng chọn ngày đóng hàng");
                return;
            }
            if (selected.Any(x => x.Id < 0))
            {
                Toast.Warning("Vui lòng lưu trước khi vận chuyển");
                return;
            }
            //var settingsInsurances = gridView.GetSelectedRows().Cast<TransportationPlan>().Where(x => x.IsSettingsInsurance == false).ToList();
            //if (settingsInsurances.Count > 0)
            //{
            //    Toast.Warning($"Có {settingsInsurances.Count} KHVC chưa được cấu hình GTHH. Vui lòng cấu hình trước khi vận chuyển");
            //    foreach (var item in settingsInsurances)
            //    {
            //        var listViewItem = gridView.GetListViewItems(item).FirstOrDefault();
            //        if (listViewItem is null)
            //        {
            //            return;
            //        }
            //        if (listViewItem != null)
            //        {
            //            listViewItem.Element.AddClass("bg-red1");
            //        }
            //    }
            //    return;
            //}
            selected = selected.Where(x => x.TotalContainerRemain > 0).ToList();
            var count = 0;
            var listAccept = new List<TransportationPlan>();
            var listAcceptNoContract = new List<TransportationPlan>();
            foreach (var item in selected)
            {
                if (!await CheckContract(item))
                {
                    count++;
                }
                else
                {
                    listAccept.Add(item);
                }
            }
            if (count > 0)
            {
                var confirmDialog = new ConfirmDialog
                {
                    Content = $"Có {count} kế hoạch chưa có hợp đồng bạn có muốn vận chuyển không?"
                };
                confirmDialog.YesConfirmed += async () =>
                {
                    await ActionCreateTransportation(selected);
                };
                AddChild(confirmDialog);
            }
            else
            {
                await ActionCreateTransportation(selected);
            }
        }

        public void ViewTransportation(object arg)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(TransportationPlan));

            Task.Run(async () =>
            {
                var selected = (await gridView.GetRealTimeSelectedRows());
                if (selected.Nothing())
                {
                    selected = gridView.RowData.Data;
                }
                var coords = selected.Cast<TransportationPlan>().ToList().Select(x => x.Id).Distinct().ToList();
                var fe = Tabs.Where(x => x.Name == "Transportation List").FirstOrDefault();
                if (fe is null)
                {
                    Toast.Warning("Vui lòng mở màn hình danh sách vận chuyển");
                    return;
                }
                var gridView1 = fe.FilterChildren<GridView>().FirstOrDefault(x => x.GuiInfo.Id == 16016);
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
                        FieldName = "TransportationPlanId",
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

        private async Task ActionCreateTransportation(List<TransportationPlan> selected)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var containerTypeIds = selected.Where(x => x.ContainerTypeId != null).Select(x => x.ContainerTypeId.Value).ToList();
            var containerTypeDb = new Client(nameof(MasterData)).GetRawListById<MasterData>(containerTypeIds);
            var containers = new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC'))");
            var commodityTypeIds = selected.Where(x => x.CommodityId != null).Select(x => x.CommodityId.Value).ToList();
            var bossIds = selected.Where(x => x.BossId != null).Select(x => x.BossId.Value).ToList();
            var commodityValueDB = new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId in ({bossIds.Combine()}) and CommodityId in ({commodityTypeIds.Combine()}) and ContainerId eq {containerId}");
            var expenseTypeDB = new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and ParentId eq 7577 and contains(Name, 'Bảo hiểm')");
            var masterDataDB = new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq 11685");

            var isWets = selected.Select(x => x.IsWet.ToLower()).Distinct().ToList();
            var isBoughts = selected.Select(x => x.IsBought.ToLower()).Distinct().ToList();

            var insuranceFeesRates = new Client(nameof(InsuranceFeesRate)).GetRawList<InsuranceFeesRate>($"?$filter=Active eq true and IsSOC eq false");
            await Task.WhenAll(containerTypeDb, containers, commodityValueDB, expenseTypeDB, masterDataDB, insuranceFeesRates);
            var cont20Rs = containers.Result.FirstOrDefault(x => x.Name.Contains("20DC"));
            var cont40Rs = containers.Result.FirstOrDefault(x => x.Name.Contains("40HC"));
            var dir = containerTypeDb.Result.ToDictionary(x => x.Id);
            var rs = new List<Transportation>();
            foreach (var item in selected)
            {
                decimal cont40 = 0;
                decimal cont20 = 0;
                if (item.ContainerTypeId != null)
                {
                    var cont = dir.GetValueOrDefault(item.ContainerTypeId ?? 0);
                    if (cont.Name.Contains("4"))
                    {
                        cont40 = 1;
                    }
                    if (cont.Name.Contains("2"))
                    {
                        cont20 = 1;
                    }
                }
                var containertype = dir.GetValueOrDefault(item.ContainerTypeId ?? 0);
                if (containertype.Enum == 1)
                {
                    containerId = cont20Rs.Id;
                }
                else
                {
                    containerId = cont40Rs.Id;
                }
                var containerTypeId = await CheckContainerType(item);
                var commodidtyValue = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {item.BossId} and CommodityId eq {item.CommodityId} and ContainerId eq {containerTypeId}");
                if (commodidtyValue is null && item.BossId != null && item.CommodityId != null && item.ContainerTypeId != null && item.IsCompany == false)
                {
                    var newCommodityValue = await CreateCommodityValue(item);
                    await new Client(nameof(CommodityValue)).CreateAsync<CommodityValue>(newCommodityValue);
                }
                var expense = new Expense();
                expense.CopyPropFrom(item);
                expense.Id = 0;
                expense.Quantity = 1;
                expense.ExpenseTypeId = expenseTypeDB.Result.Id; //Bảo hiểm
                expense.Vat = masterDataDB is null ? 0 : decimal.Parse(masterDataDB.Result.Name);
                expense.SaleId = item.UserId;
                expense.Notes = "";
                if (expense.JourneyId == 12114 || expense.JourneyId == 16001)
                {
                    expense.StartShip = item.ClosingDate;
                }
                bool isSubRatio = false;
                if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
                {
                    isSubRatio = true;
                }
                var insuranceFeesRateDB = insuranceFeesRates.Result.FirstOrDefault(x => x.TransportationTypeId == expense.TransportationTypeId
                && x.JourneyId == expense.JourneyId
                && x.IsBought == expense.IsBought
                && x.IsSubRatio == isSubRatio
                && x.IsSOC == false);
                if (insuranceFeesRateDB != null)
                {
                    var getContainerType = await new Client(nameof(MasterData)).FirstOrDefaultAsync<MasterData>($"?$filter=Active eq true and Id eq {expense.ContainerTypeId}");
                    if (getContainerType != null && getContainerType.Description.Contains("Lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
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
                            foreach (var prop in expense.GetType().GetProperties())
                            {
                                if (prop.Name == x.Name && bool.Parse(prop.GetValue(expense, null).ToString()))
                                {
                                    expense.InsuranceFeeRate += decimal.Parse(x.Code);
                                    break;
                                }
                            }
                        });
                    }
                }
                else
                {
                    expense.InsuranceFeeRate = 0;
                }
                if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
                {
                    expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
                    expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
                }
                else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
                {
                    expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
                    expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
                }
                for (int i = 0; i < item.TotalContainerRemain; i++)
                {
                    var transportation = new Transportation();
                    transportation.CopyPropFrom(item);
                    transportation.Id = 0;
                    transportation.Cont20 = cont20;
                    transportation.Cont40 = cont40;
                    transportation.TransportationPlanId = item.Id;
                    transportation.Notes = null;
                    transportation.ClosingNotes = item.Notes;
                    transportation.ExportListId = Client.Token.Vendor.Id;
                    transportation.Expense.Add(expense);
                    await new Client(nameof(Transportation)).CreateAsync<Transportation>(transportation);
                }
            }
            Toast.Success("Tạo chuyến xe thành công");
            await gridView.ApplyFilter(true);
            Toast.Success("Khóa kế hoạch vận chuyển thành công");
            gridView.ClearSelected();
        }

        public async Task CheckClosingDate(TransportationPlan transportationPlan)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            await CheckContract(transportationPlan);
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            if (transportationPlan.ClosingDate.Value.Date < DateTime.Now.Date)
            {
                var confirmDialog = new ConfirmDialog
                {
                    Content = "Ngày đóng hàng nhỏ hơn ngày hiện tại?"
                };
                confirmDialog.NoConfirmed += async () =>
                {
                    gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
                    transportationPlan.ClosingDate = null;
                    await gridView.AddOrUpdateRow(transportationPlan);
                };
                AddChild(confirmDialog);
            }
        }

        public async Task<bool> CheckContract(TransportationPlan transportationPlan)
        {
            if (transportationPlan.BossId != null && transportationPlan.ClosingDate != null)
            {
                var contact = await new Client(nameof(TransportationContract)).FirstOrDefaultAsync<TransportationContract>($"?$filter=BossId eq {transportationPlan.BossId} and cast(StartDate,Edm.DateTimeOffset) lt {transportationPlan.ClosingDate.Value.ToOdataFormat()} and cast(EndDate,Edm.DateTimeOffset) gt {transportationPlan.ClosingDate.Value.ToOdataFormat()}");
                if (contact is null)
                {
                    Toast.Warning("Khách hàng chưa có hợp đồng vui lòng bổ sung hợp đồng");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task CheckContractAndAnalysis(TransportationPlan transportationPlan)
        {
            Analysis(transportationPlan);
            await CheckContract(transportationPlan);
        }

        public async Task BeforeCreated(TransportationPlan transportationPlan)
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItem = gridView.GetListViewItems(transportationPlan).FirstOrDefault();
            if (listViewItem is null)
            {
                transportationPlan.IsTransportation = false;
                transportationPlan.TotalContainerUsing = 0;
                transportationPlan.TotalContainerRemain = transportationPlan.TotalContainer;
                transportationPlan.PlanDate = DateTime.Now;
                return;
            }
            var user = await new Client(nameof(User)).FirstOrDefaultAsync<API.Models.User>($"?$orderby=Id desc&$filter=Active eq true and Id eq {EditForm.CurrentUserId}&$take=1");
            transportationPlan.RouteId = user.RouteId;
            var updated = listViewItem.FilterChildren<SearchEntry>(x => x.GuiInfo.FieldName == nameof(TransportationPlan.RouteId)).ToList();
            updated.ForEach(x => x.Dirty = true);
        }

        private async Task SelectedExcel(Event e)
        {
            var files = e.Target["files"] as FileList;
            if (files.Nothing())
            {
                return;
            }

            var uploadForm = _uploader.ParentElement as HTMLFormElement;
            var formData = new FormData(uploadForm);
            await Client.SubmitAsync<List<TransportationPlan>>(new XHRWrapper
            {
                FormData = formData,
                Url = "ImportExcel",
                Method = HttpMethod.POST,
                ResponseMimeType = Utils.GetMimeType("xlsx")
            });
        }

        public void ImportExcel()
        {
            _uploader.Click();
        }

        public void CheckQuotationTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var listViewItems = gridView.RowData.Data.Cast<Transportation>().ToList();
            if (EditForm.Name != "Transportation Return Plan List")
            {
                listViewItems.ForEach(x =>
                {
                    var listViewItem = gridView.GetListViewItems(x).FirstOrDefault();
                    if (listViewItem is null)
                    {
                        return;
                    }
                    listViewItem.Element.RemoveClass("bg-red");
                    if (!x.IsQuotationReturn)
                    {
                        listViewItem.Element.AddClass("bg-red");
                    }
                });
            }
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
                if (transportationPlan.TransportationTypeId != null || commodityValueDB != null)
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
                    transportationPlan.IsSettingsInsurance = true;
                    Toast.Success("GTHH đã tồn tại trong hệ thống với giá trị là: " + decimal.Parse(transportationPlan.CommodityValue.ToString()).ToString("N0"));
                }
                await new Client(nameof(TransportationPlan)).PatchAsync<object>(GetPatchEntity(transportationPlan));
                if (commodityValueDB == null || (transportationPlan.TransportationTypeId != null && transportationPlan.JourneyId == null))
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
                    var components = new Client(nameof(GridPolicy)).GetRawList<GridPolicy>("?$filter=Id in (20511, 17793)");
                    var operators = new Client(nameof(MasterData)).GetRawList<MasterData>("?$filter=Parent/Name eq 'Operator'");
                    var settingPolicys = new Client(nameof(SettingPolicy)).GetRawList<SettingPolicy>($"?$expand=SettingPolicyDetail&$filter=TypeId eq 2");
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
                            check = await new Client(nameof(TransportationPlan)).FirstOrDefaultAsync<TransportationPlan>($"?$filter=Active eq true and Id eq {transportationPlan.Id} and ({str}) and RequestChangeId eq null");
                        }
                        if (check != null)
                        {
                            transportationPlan.TransportationTypeId = item.TransportationTypeId;
                            var patchModel = GetPatchEntity(transportationPlan);
                            await new Client(nameof(TransportationPlan)).PatchAsync<TransportationPlan>(patchModel);
                        }
                    }
                    var checks = query.Where(x => !x.IsNullOrWhiteSpace()).Select(x => new Client(nameof(TransportationPlan)).FirstOrDefaultAsync<TransportationPlan>($"?$filter=Active eq true and Id eq {transportationPlan.Id} and ({x}) and RequestChangeId eq null")).ToList();
                    var data1 = await Task.WhenAll(checks);
                    var indexOf = data1.IndexOf(x => x != null);
                    if (indexOf == -1)
                    {
                        transportationPlan.TransportationTypeId = null;
                        var patchModel = GetPatchEntity(transportationPlan);
                        await new Client(nameof(TransportationPlan)).PatchAsync<TransportationPlan>(patchModel);
                    }
                }
                Analysis(transportationPlan);
            }, 500);
        }

        public void AfterPatchUpdateTransportationPlan(TransportationPlan transportationPlan, PatchUpdate patchUpdate, ListViewItem listViewItem)
        {
            if (listViewItem is null)
            {
                return;
            }
            listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
            if (transportationPlan.IsTransportation)
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
            }
            else
            {
                listViewItem.FilterChildren(y => y.GuiInfo.FieldName == "btnRequestChange" && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
            }
            listViewItem.Element.RemoveClass("bg-host");
            if (transportationPlan.StatusId == (int)ApprovalStatusEnum.Approving)
            {
                listViewItem.Element.AddClass("bg-host");
            }
            if (listViewItem.ListViewSection.ListView.GuiInfo.FieldName == "TransportationPlan2" && transportationPlan.IsTransportation)
            {
                listViewItem.ListViewSection.ListView.RemoveRowById(transportationPlan.Id);
                listViewItem.ListViewSection.ListView.SelectedIds.Remove(transportationPlan.Id);
            }
        }

        private static bool ListViewItemFilter(object updatedData, EditableComponent x)
        {
            if (x is GroupViewItem)
            {
                return false;
            }

            return x.Entity != null && x.Entity.GetType().Name == updatedData.GetType().Name && x.Entity[IdField].ToString() == updatedData[IdField].ToString();
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
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsSettingsInsurance), Value = transportationPlan.IsSettingsInsurance.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}