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
using Math = System.Math;

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanListBL : TabEditor
    {
        public TransportationPlanListBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan List";
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
            entity = await Client.GetAsync<TransportationPlan>(entity.Id);
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

        public void ReportTransportationPlan()
        {
            var confirmDialog = new ConfirmDialog
            {
                Content = $"Nhập ngày cần xuất báo cáo trễ sau 5 giờ",
                NeedAnswer = true,
                MultipleLine = false,
                ComType = nameof(Datepicker)
            };
            confirmDialog.YesConfirmed += async () =>
            {
            };
            confirmDialog.Entity = new { ReasonOfChange = string.Empty };
            confirmDialog.Render();
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
            UpdateListView(gridView);
        }

        private void UpdateListView(GridView gridView)
        {
            foreach (var listViewItem in gridView.MainSection.FilterChildren<ListViewItem>())
            {
                if (listViewItem is null)
                {
                    return;
                }
                var x = listViewItem.Entity as TransportationPlan;
                listViewItem.FilterChildren(y => !y.GuiInfo.Disabled).ForEach(y => y.Disabled = false);
                if (x.IsTransportation)
                {
                    listViewItem.FilterChildren(y => y.GuiInfo.FieldName != "btnRequestChange" && y.GuiInfo.FieldName != nameof(TransportationPlan.NotesContract) && !y.GuiInfo.Disabled).ForEach(y => y.Disabled = true);
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
        }

        public async Task CreateTransportation()
        {
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            var data = await gridView.GetRealTimeSelectedRows();
            var selected = data.Cast<TransportationPlan>().ToList();
            var selectedIds = selected.Select(x => x.Id).ToList();
            selected = await new Client(nameof(TransportationPlan)).GetRawListById<TransportationPlan>(selectedIds);
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
            if (selected.Any(x => x.TotalContainerRemain == 0))
            {
                Toast.Warning("Có kế hoạch vận chuyển đã được lấy qua");
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
            selected = selected.Where(x => x.TotalContainerRemain > 0).ToList();
            var count = 0;
            var listAccept = new List<TransportationPlan>();
            var listAcceptNoContract = new List<TransportationPlan>();
            foreach (var item in selected)
            {
                if (!await CheckContract(item, null))
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
            this.SetDisabled(true, "btnTransportation");
            var res = await new Client(nameof(TransportationPlan)).PostAsync<bool>(selected, "CreateTransportation");
            this.SetDisabled(false, "btnTransportation");
            if (res)
            {
                Toast.Success("Tạo chuyến xe thành công");
                await gridView.ApplyFilter(true);
                Toast.Success("Khóa kế hoạch vận chuyển thành công");
                gridView.ClearSelected();
            }
            else
            {
                Toast.Warning("Lỗi tạo chuyến xe");
            }
        }

        public async Task CheckClosingDate(TransportationPlan transportationPlan)
        {
            if (transportationPlan.ClosingDate is null || transportationPlan.BossId is null)
            {
                return;
            }
            var gridView = this.FindActiveComponent<GridView>().FirstOrDefault();
            await CheckContract(transportationPlan, null);
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

        public async Task<bool> CheckContract(TransportationPlan transportationPlan, Vendor vendor)
        {
            if (vendor != null && vendor.TaxCode.IsNullOrWhiteSpace())
            {
                Toast.Warning("MST/CCCD không được để trống");
                await this.OpenPopup(
                featureName: "Vendor Editor",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Shop.VendorEditorBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Chỉnh sửa chủ hàng";
                    instance.Entity = vendor;
                    return instance;
                });
            }
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

        public void BeforeCreated(TransportationPlan transportationPlan)
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

        public async Task Analysis(TransportationPlan transportationPlan, PatchUpdate patchUpdate)
        {
            if (transportationPlan.BossId is null || transportationPlan.CommodityId is null || transportationPlan.ContainerTypeId is null || transportationPlan.RouteId is null)
            {
                return;
            }
            if (!patchUpdate.Changes.Any(x => x.Field == nameof(transportationPlan.BossId) || x.Field == nameof(transportationPlan.CommodityId) || x.Field == nameof(transportationPlan.ContainerTypeId) || x.Field == nameof(transportationPlan.RouteId)))
            {
                return;
            }
            if (transportationPlan.TransportationTypeId is null)
            {
                await SetPolicy(transportationPlan, patchUpdate);
            }
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
                transportationPlan.SteamingTerms = commodityValueDB.SteamingTerms;
                transportationPlan.BreakTerms = commodityValueDB.BreakTerms;
                transportationPlan.IsBought = commodityValueDB.IsBought;
                transportationPlan.CustomerTypeId = commodityValueDB.CustomerTypeId;
                transportationPlan.CommodityValue = commodityValueDB.TotalPrice;
                transportationPlan.CommodityValueNotes = commodityValueDB.Notes;
                transportationPlan.IsSettingsInsurance = true;
                Toast.Success("GTHH đã tồn tại trong hệ thống với giá trị là: " + decimal.Parse(transportationPlan.CommodityValue.ToString()).ToString("N0"));
            }
            await new Client(nameof(TransportationPlan)).PatchAsync<object>(GetPatchEntity(transportationPlan), ig: $"&disableTrigger=true");
            if (commodityValueDB == null || (transportationPlan.TransportationTypeId != null && (transportationPlan.JourneyId == null || transportationPlan.JourneyId == 0)))
            {
                await SettingsCommodityValue(transportationPlan);
            }
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

        public async Task<List<int>> CheckContainerTypes(List<TransportationPlan> transportationPlans)
        {
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeIds = new List<int>();
            var containers = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC') or contains(Name, '45HC') or contains(Name, '50DC'))");
            foreach (var item in transportationPlans)
            {
                var containerTypeName = containerTypeCodes.GetValueOrDefault((int)item.ContainerTypeId);
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
                containerTypeIds.Add(containerId);
            }
            return containerTypeIds;
        }

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

        public async Task SetPolicy(TransportationPlan transportationPlan, PatchUpdate patchUpdate)
        {
            if (transportationPlan.RouteId != null && patchUpdate.Changes.Any(x => x.Field == nameof(TransportationPlan.RouteId)))
            {
                var transportationTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 11670");
                var route = await new Client(nameof(Route)).FirstOrDefaultAsync<Route>($"?$filter=Active eq true and Id eq {transportationPlan.RouteId}");
                if (route != null)
                {
                    if (route.Name.ToLower().Contains("sắt"))
                    {
                        transportationPlan.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("sắt")).FirstOrDefault().Id;
                    }
                    else if (route.Name.ToLower().Contains("bộ") || route.Name.ToLower().Contains("trucking vtqt"))
                    {
                        transportationPlan.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("bộ")).FirstOrDefault().Id;
                    }
                    else
                    {
                        transportationPlan.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("tàu")).FirstOrDefault().Id;
                    }
                }
                await new Client(nameof(TransportationPlan)).PatchAsync<TransportationPlan>(GetPatchEntityTransportationType(transportationPlan), ig: $"&disableTrigger=true");
            }
        }

        public async Task AfterPatchUpdateTransportationPlan(TransportationPlan transportationPlan, PatchUpdate patchUpdate, ListViewItem listViewItem)
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
            await SetPolicy(transportationPlan, patchUpdate);
            await Analysis(transportationPlan, patchUpdate);
        }

        public void AfterWebsocketTransportationPlan(TransportationPlan transportationPlan, ListViewItem listViewItem)
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
            newCommodityValue.CreatedBy = transportationPlan.InsertedBy;
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

        public PatchUpdate GetPatchEntityTransportationType(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>
            {
                new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.TransportationTypeId), Value = transportationPlan.TransportationTypeId.ToString() }
            };
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntityJourneyId(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>
            {
                new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.JourneyId), Value = transportationPlan.JourneyId.ToString() }
            };
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchIsTransportation(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>
            {
                new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.IsTransportation), Value = true.ToString() }
            };
            return new PatchUpdate { Changes = details };
        }

        public PatchUpdate GetPatchEntity(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>
            {
                new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.TransportationTypeId), Value = transportationPlan.TransportationTypeId.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.IsWet), Value = transportationPlan.IsWet.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.SteamingTerms), Value = transportationPlan.SteamingTerms.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.BreakTerms), Value = transportationPlan.BreakTerms.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.IsBought), Value = transportationPlan.IsBought.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.JourneyId), Value = transportationPlan.JourneyId.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.CustomerTypeId), Value = transportationPlan.CustomerTypeId.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.CommodityValue), Value = transportationPlan.CommodityValue.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.IsSettingsInsurance), Value = transportationPlan.IsSettingsInsurance.ToString() }
            };
            return new PatchUpdate { Changes = details };
        }
    }
}