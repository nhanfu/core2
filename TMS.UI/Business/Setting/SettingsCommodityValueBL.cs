using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.Notifications;
using Core.ViewModels;
using Retyped.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.UI.Business.Manage;
using static Retyped.canvasjs.Literals.Types;
using static Retyped.dom.Literals.Types;

namespace TMS.UI.Business.Setting
{
    class SettingsCommodityValueBL : PopupEditor
    {
        public TransportationPlan transportationPlanEntity => Entity as TransportationPlan;
        public SettingsCommodityValueBL() : base(nameof(TransportationPlan))
        {
            Name = "SettingsCommodityValue";
        }

        protected async override void ToggleApprovalBtn(object entity = null)
        {
            var containerId = await CheckContainerType(transportationPlanEntity);
            var commodityValueDB = await new Client(nameof(CommodityValue)).FirstOrDefaultAsync<CommodityValue>($"?$filter=Active eq true and BossId eq {transportationPlanEntity.BossId} and CommodityId eq {transportationPlanEntity.CommodityId} and ContainerId eq {containerId}");
            if (commodityValueDB != null && transportationPlanEntity.JourneyId == null)
            {
                this.SetDisabled(true, "CustomerTypeId", "IsWet", "IsBought", "CommodityValue", "IsCompany", "SteamingTerms", "BreakTerms");
            }
            base.ToggleApprovalBtn(entity);
        }

        private int containerId = 0;

        public async Task<int> CheckContainerType(TransportationPlan transportationPlan)
        {
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)transportationPlan.ContainerTypeId);
            var containers = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC'))");
            if (containerTypeName.Description.Contains("Cont 20"))
            {
                containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
            }
            else if (containerTypeName.Description.Contains("Cont 40"))
            {
                containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
            }
            return containerId;
        }

        public async Task UpdateTransportationPlan()
        {
            if (transportationPlanEntity.IsWet && transportationPlanEntity.SteamingTerms && transportationPlanEntity.BreakTerms)
            {
                Toast.Warning("Không thể cùng lúc có nhiều hơn 2 điều khoản");
                return;
            }
            if (transportationPlanEntity.JourneyId is null)
            {
                Toast.Warning("Hành trình vận chuyển không được trống");
                return;
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
                return;
            }
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
                    await SaveTransportationPlanAsync();
                };
            }
            else
            {
                await SaveTransportationPlanAsync();
            }
        }

        private async Task SaveTransportationPlanAsync()
        {
            transportationPlanEntity.IsSettingsInsurance = true;
            var rs = await new Client(nameof(TransportationPlan)).PatchAsync<object>(GetPatchEntity(transportationPlanEntity));
            if (rs != null)
            {
                Toast.Success("Đã cập nhật thành công");
            }
            else
            {
                Toast.Warning("Chưa cập nhật thành công");
            }
            Dispose();
        }

        public async Task CheckCommodityValue()
        {
            if (transportationPlanEntity.IsCompany)
            {
                transportationPlanEntity.CommodityValue = await SetPolicy(transportationPlanEntity);
            }
            else
            {
                transportationPlanEntity.CommodityValue = 0;
            }
            UpdateView(false, nameof(TransportationPlan.CommodityValue));
        }

        public async Task<decimal> SetPolicy(TransportationPlan transportationPlan)
        {
            var gridView = Parent.FindActiveComponent<GridView>().FirstOrDefault(x => x.GuiInfo.FieldName == nameof(TransportationPlan));
            var listViewItem = gridView.GetListViewItems(transportationPlan).FirstOrDefault();
            if (transportationPlan.RouteId != null || transportationPlan.BossId != null)
            {
                var components = new Client(nameof(GridPolicy)).GetRawList<GridPolicy>("?$filter=Id in (17792, 17791)");
                var operators = new Client(nameof(MasterData)).GetRawList<MasterData>("?$filter=Parent/Name eq 'Operator'");
                var settingPolicys = new Client(nameof(SettingPolicy)).GetRawList<SettingPolicy>($"?$expand=SettingPolicyDetail&$filter=TypeId eq 3");
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
                                if (ope.Name.Contains("eq"))
                                {
                                    return new Client(component.RefName).GetRawList<dynamic>($"?$select=Id&$filter=contains({format},'" + l.Value + "')", entityName: component.RefName);
                                }
                                else if (ope.Name.Contains("ne"))
                                {
                                    return new Client(component.RefName).GetRawList<dynamic>($"?$select=Id&$filter=contains({format},'" + l.Value + "') eq false", entityName: component.RefName);
                                }
                                else
                                {
                                    return null;
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
                                    return $"{component.FieldName} in ({ids})";
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
                    var str = build.Where(x => x != "()" && !x.IsNullOrWhiteSpace()).Combine(" and ");
                    query.Add(str);
                    TransportationPlan check = null;
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        check = await new Client(nameof(TransportationPlan)).FirstOrDefaultAsync<TransportationPlan>($"?$filter=Active eq true and Id eq {transportationPlan.Id} and ({str}) and RequestChangeId eq null");
                    }
                }
                var checks = query.Where(x => !x.IsNullOrWhiteSpace()).Select(x => new Client(nameof(TransportationPlan)).FirstOrDefaultAsync<TransportationPlan>($"?$filter=Active eq true and Id eq {transportationPlan.Id} and ({x}) and RequestChangeId eq null")).ToList();
                var data1 = await Task.WhenAll(checks);
                var indexOf = data1.IndexOf(x => x != null);
                if (indexOf == -1)
                {
                    transportationPlan.CommodityValue = 0;
                }
                else
                {
                    transportationPlan.CommodityValue = listpolicy[indexOf].UnitPrice;
                }
            }
            return (decimal)transportationPlan.CommodityValue;
        }

        public PatchUpdate GetPatchEntity(TransportationPlan transportationPlan)
        {
            var details = new List<PatchUpdateDetail>();
            details.Add(new PatchUpdateDetail { Field = Utils.IdField, Value = transportationPlan.Id.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.TransportationTypeId), Value = transportationPlan.TransportationTypeId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.JourneyId), Value = transportationPlan.JourneyId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.CustomerTypeId), Value = transportationPlan.CustomerTypeId.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsBought), Value = transportationPlan.IsBought.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsWet), Value = transportationPlan.IsWet.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.CommodityValue), Value = transportationPlan.CommodityValue.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsCompany), Value = transportationPlan.IsCompany.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.IsSettingsInsurance), Value = transportationPlan.IsSettingsInsurance.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.SteamingTerms), Value = transportationPlan.SteamingTerms.ToString() });
            details.Add(new PatchUpdateDetail { Field = nameof(TransportationPlan.BreakTerms), Value = transportationPlan.BreakTerms.ToString() });
            return new PatchUpdate { Changes = details };
        }
    }
}
