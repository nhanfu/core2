using Bridge.Html5;
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

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanEditorMobileBL : TabEditor
    {
        public TransportationPlan transportationPlanEntity => Entity as TransportationPlan;
        public TransportationPlanEditorMobileBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan Editor Mobile";
            DOMContentLoaded += () =>
            {
                if (transportationPlanEntity.Id <= 0)
                {
                    this.SetShow(false, "btnDelete");
                }
                if (transportationPlanEntity.IsTransportation)
                {
                    LockUpdateButCancel();
                }
            };
        }

        public override async Task<bool> Save(object entity = null)
        {
            if (!(await IsFormValid()))
            {
                return false;
            }
            if (transportationPlanEntity.RouteId == null)
            {
                Toast.Warning("Tuyến vận chuyển không được trống");
                return false;
            }
            if (transportationPlanEntity.BossId == null)
            {
                Toast.Warning("Chủ hàng không được trống");
                return false;
            }
            if (transportationPlanEntity.CommodityId == null)
            {
                Toast.Warning("Vật tư hàng hóa không được trống");
                return false;
            }
            if (transportationPlanEntity.ContainerTypeId == null)
            {
                Toast.Warning("Loại cont không được trống");
                return false;
            }
            if (transportationPlanEntity.JourneyId == null)
            {
                Toast.Warning("Hành trình bảo hiểm không được trống");
                return false;
            }
            if (transportationPlanEntity.IsWet && transportationPlanEntity.SteamingTerms && transportationPlanEntity.BreakTerms)
            {
                Toast.Warning("Không thể cùng lúc có nhiều hơn 2 điều khoản");
                return false;
            }
            bool isSubRatio = false;
            if (((transportationPlanEntity.IsWet || transportationPlanEntity.SteamingTerms || transportationPlanEntity.BreakTerms) && transportationPlanEntity.IsBought == false) || (transportationPlanEntity.IsBought && transportationPlanEntity.IsWet))
            {
                isSubRatio = true;
            }
            InsuranceFeesRate insuranceFeesRate = null;
            if (transportationPlanEntity.IsBought)
            {
                insuranceFeesRate = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {transportationPlanEntity.TransportationTypeId} and JourneyId eq {transportationPlanEntity.JourneyId} and IsBought eq {transportationPlanEntity.IsBought.ToString().ToLower()} and IsSOC eq false and IsSubRatio eq {isSubRatio.ToString().ToLower()}");
            }
            else
            {
                insuranceFeesRate = await new Client(nameof(InsuranceFeesRate)).FirstOrDefaultAsync<InsuranceFeesRate>($"?$filter=Active eq true and TransportationTypeId eq {transportationPlanEntity.TransportationTypeId} and JourneyId eq {transportationPlanEntity.JourneyId} and IsBought eq {transportationPlanEntity.IsBought.ToString().ToLower()} and IsSOC eq false");
            }
            if (insuranceFeesRate is null)
            {
                Toast.Warning("Hiện tại chưa có mức tỷ lệ phí phù hợp cho các điều kiện này. Vui lòng cấu hình lại !!!");
                return false;
            }
            transportationPlanEntity.TotalContainerRemain = transportationPlanEntity.TotalContainerRemain ?? transportationPlanEntity.TotalContainer;
            transportationPlanEntity.TotalContainerUsing = transportationPlanEntity.TotalContainerUsing ?? 0;
            if (transportationPlanEntity.CommodityValue == 0 || transportationPlanEntity.CommodityValue == null)
            {
                var confirm = new ConfirmDialog
                {
                    Content = "Bạn có chắc chắn muốn lưu khi GTHH bằng 0 không?",
                };
                confirm.Render();
                confirm.YesConfirmed += async () =>
                {
                    transportationPlanEntity.CommodityValue = 0;
                    transportationPlanEntity.IsSettingsInsurance = true;
                    await base.Save(entity);
                    Dispose();
                };
                return false;
            }
            else
            {
                transportationPlanEntity.IsSettingsInsurance = true;
            }
            if (transportationPlanEntity.IsSettingsInsurance == false)
            {
                Toast.Warning("Bạn chưa cấu hình GTHH. Vui lòng cấu hình lại !!!");
                return false;
            }
            var rs = await base.Save(entity);
            Dispose();
            return rs;
        }

        public async Task SetPolicyAndAnalysis()
        {
            await SetPolicy();
            await Analysis();
        }

        public async Task SetPolicy()
        {
            if (transportationPlanEntity.RouteId == null)
            {
                return;
            }
            var transportationTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 11670");
            var route = await new Client(nameof(Route)).FirstOrDefaultAsync<Route>($"?$filter=Active eq true and Id eq {transportationPlanEntity.RouteId}");
            if (route.Name.ToLower().Contains("sắt"))
            {
                transportationPlanEntity.TransportationTypeId = transportationTypes.Where(x => x.Name.Contains("Sắt")).FirstOrDefault().Id;
            }
            else if (route.Name.ToLower().Contains("bộ") || route.Name.ToLower().Contains("trucking vtqt"))
            {
                transportationPlanEntity.TransportationTypeId = transportationTypes.Where(x => x.Name.Contains("Bộ")).FirstOrDefault().Id;
            }
            else
            {
                transportationPlanEntity.TransportationTypeId = transportationTypes.Where(x => x.Name.Contains("Tàu")).FirstOrDefault().Id;
            }
            this.UpdateView(false, nameof(TransportationPlan.TransportationTypeId));
        }

        public async Task Analysis()
        {
            if (transportationPlanEntity.BossId == null || transportationPlanEntity.CommodityId == null || transportationPlanEntity.ContainerTypeId == null)
            {
                return;
            }
            var containerId = await CheckContainerType(transportationPlanEntity);
            var commodityValueDB = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {transportationPlanEntity.BossId} and CommodityId eq {transportationPlanEntity.CommodityId} and ContainerId eq {containerId}");
            if (commodityValueDB != null)
            {
                transportationPlanEntity.SteamingTerms = commodityValueDB.SteamingTerms;
                transportationPlanEntity.BreakTerms = commodityValueDB.BreakTerms;
                transportationPlanEntity.IsWet = commodityValueDB.IsWet;
                transportationPlanEntity.IsBought = commodityValueDB.IsBought;
                transportationPlanEntity.CustomerTypeId = commodityValueDB.CustomerTypeId;
                transportationPlanEntity.CommodityValue = commodityValueDB.TotalPrice;
                transportationPlanEntity.JourneyId = commodityValueDB.JourneyId;
                transportationPlanEntity.IsSettingsInsurance = true;
                if (transportationPlanEntity.JourneyId == null)
                {
                    this.SetDisabled(true, "CustomerTypeId", "IsWet", "IsBought", "CommodityValue", "IsCompany", "SteamingTerms", "BreakTerms");
                    this.SetDisabled(false, "JourneyId");
                }
                else
                {
                    this.SetDisabled(true, "CustomerTypeId", "IsWet", "IsBought", "CommodityValue", "IsCompany", "SteamingTerms", "BreakTerms", "JourneyId");
                }
                Toast.Success("GTHH đã tồn tại trong hệ thống với giá trị là: " + decimal.Parse(transportationPlanEntity.CommodityValue.ToString()).ToString("N0"));
            }
            else
            {
                transportationPlanEntity.SteamingTerms = false;
                transportationPlanEntity.BreakTerms = false;
                transportationPlanEntity.IsWet = false;
                transportationPlanEntity.IsBought = false;
                transportationPlanEntity.CustomerTypeId = null;
                transportationPlanEntity.CommodityValue = null;
                transportationPlanEntity.JourneyId = null;
                transportationPlanEntity.IsSettingsInsurance = false;
                if (transportationPlanEntity.TransportationTypeId != null)
                {
                    if (transportationPlanEntity.TransportationTypeId != 11673)
                    {
                        transportationPlanEntity.JourneyId = 12114;
                        transportationPlanEntity.IsWet = false;
                    }
                }
                this.SetDisabled(false, "CustomerTypeId", "IsWet", "IsBought", "CommodityValue", "IsCompany", "SteamingTerms", "BreakTerms", "JourneyId");
            }
            this.UpdateView(false, nameof(TransportationPlan.CustomerTypeId),
                                           nameof(TransportationPlan.IsWet),
                                           nameof(TransportationPlan.IsBought),
                                           nameof(TransportationPlan.CommodityValue),
                                           nameof(TransportationPlan.IsCompany),
                                           nameof(TransportationPlan.SteamingTerms),
                                           nameof(TransportationPlan.BreakTerms),
                                           nameof(TransportationPlan.JourneyId));
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

        public PatchUpdate GetPatchEntityTransportationType(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>
            {
                new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() },
                new PatchUpdateDetail { Field = nameof(TransportationPlan.TransportationTypeId), Value = transportationPlan.TransportationTypeId.ToString() }
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