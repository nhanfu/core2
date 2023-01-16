using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;
using TMS.API.ViewModels;
using Location = TMS.API.Models.Location;

namespace TMS.UI.Business.Accountant
{
    public class BusinessFreightRateBL : PopupEditor
    {
        public FreightRate freightRateEntity => Entity as FreightRate;
        public BusinessFreightRateBL() : base(nameof(FreightRate))
        {
            Name = "Business Freight Rate";
            DOMContentLoaded += () =>
            {
                if (freightRateEntity.IsEmptyCombination)
                {
                    this.SetShow(false, "wrapper1");
                }
                else
                {
                    this.SetShow(false, "wrapper2");
                }
            };
        }

        public async void ChangeEmptyCombination()
        {
            if (freightRateEntity.IsEmptyCombination)
            {
                this.SetShow(false, "wrapper1");
                this.SetShow(true, "wrapper2");
            }
            else
            {
                this.SetShow(true, "wrapper1");
                this.SetShow(false, "wrapper2");
            }
            await CalcUnitPriceAsync();
        }

        private int awaiter;

        public void CalcUnitPrice()
        {
            Window.ClearTimeout(awaiter);
            awaiter = Window.SetTimeout(async () =>
            {
                await CalcUnitPriceAsync();
            }, 500);
        }

        public async Task CalcUnitPriceAsync()
        {
            if (freightRateEntity.ReceivedId == null || freightRateEntity.ReturnId == null
                || freightRateEntity.RegionReceivedId == null || freightRateEntity.RegionReturnId == null
                || freightRateEntity.ContainerTypeId == null)
            {
                Toast.Warning("Chưa nhập đủ dữ liệu !!!");
                return;
            }
            await GetReceivedCVCUnitPrice(freightRateEntity);
            await GetReturnCVCUnitPrice(freightRateEntity);
            await GetInsuranceFee(freightRateEntity);
            await GetShipUnitPrice(freightRateEntity);
            freightRateEntity.TotalPriceMax = freightRateEntity.ReceivedCVCUnitPrice + freightRateEntity.ReturnCVCUnitPrice + freightRateEntity.InsuranceFee + freightRateEntity.OrtherUnitPrice + freightRateEntity.ProfitUnitPrice;
            freightRateEntity.TotalPriceAVG = freightRateEntity.ReceivedCVCUnitPrice + freightRateEntity.ReturnCVCUnitPrice + freightRateEntity.InsuranceFee + freightRateEntity.OrtherUnitPrice + freightRateEntity.ProfitUnitPrice;
            if (freightRateEntity.IsEmptyCombination)
            {
                await GetReceivedReturnUnitPrice(freightRateEntity);
                freightRateEntity.TotalPriceMax += freightRateEntity.ReceivedReturnUnitPriceMax + freightRateEntity.ShipUnitPriceMax;
                freightRateEntity.TotalPriceAVG += Math.Round((decimal)freightRateEntity.ReceivedReturnUnitPriceAVG + (decimal)freightRateEntity.ShipUnitPriceAVG);
            }
            else
            {
                await GetReceivedUnitPrice(freightRateEntity);
                await GetReturnUnitPrice(freightRateEntity);
                freightRateEntity.TotalPriceMax += freightRateEntity.ReceivedUnitPriceMax + freightRateEntity.ReturnUnitPriceMax + freightRateEntity.ShipUnitPriceMax;
                freightRateEntity.TotalPriceAVG += Math.Round((decimal)freightRateEntity.ReceivedUnitPriceAVG + (decimal)freightRateEntity.ReturnUnitPriceAVG + (decimal)freightRateEntity.ShipUnitPriceAVG);
            }
            UpdateView();
        }

        private async Task GetReceivedCVCUnitPrice(FreightRate freightRate)
        {
            var quotations = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7592 and LocationId eq {freightRate.ReceivedId} and ContainerTypeId eq {freightRate.ContainerTypeId}");
            if (quotations.Count > 0)
            {
                freightRate.ReceivedCVCUnitPrice = quotations.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
            }
            else
            {
                freightRate.ReceivedCVCUnitPrice = 0;
            }
        }

        private async Task GetReturnCVCUnitPrice(FreightRate freightRate)
        {
            var quotations = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7593 and LocationId eq {freightRate.ReturnId} and ContainerTypeId eq {freightRate.ContainerTypeId}");
            if (quotations.Count > 0)
            {
                freightRate.ReturnCVCUnitPrice = quotations.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
            }
            else
            {
                freightRate.ReturnCVCUnitPrice = 0;
            }
        }

        private async Task GetInsuranceFee(FreightRate freightRate)
        {
            var container = await CheckContainerType(freightRate);
            var settingPolicys = await new Client(nameof(SettingPolicy)).GetRawList<SettingPolicy>($"?$filter=Active eq true and contains(Name, '{container.Description}')");
            if (settingPolicys != null)
            {
                foreach (var item in settingPolicys)
                {
                    var settingPolicyDetails = await new Client(nameof(SettingPolicyDetail)).GetRawList<SettingPolicyDetail>($"?$filter=Active eq true and SettingPolicyId eq {item.Id}");
                    if (settingPolicyDetails != null && settingPolicyDetails.Count == 1)
                    {
                        var check = settingPolicyDetails.Any(x => x.ComponentId == 17791);
                        freightRate.InsuranceFee = check ? item.UnitPrice : 0;
                    }
                }
            }
        }

        private async Task GetReceivedUnitPrice(FreightRate freightRate)
        {
            var locations = await new Client(nameof(Location)).GetRawList<Location>($"?$filter=Active eq true and RegionId eq {freightRate.RegionReceivedId}");
            if (locations == null)
            {
                freightRate.ReceivedUnitPriceMax += 0;
                freightRate.ReceivedUnitPriceAVG += 0;
                return;
            }
            foreach (var location in locations)
            {
                var services = await new Client(nameof(LocationService)).GetRawList<LocationService>($"?$filter=Active eq true and LocationId eq {location} and ServiceId in (7583, 7584, 7585, 7586)");
                if (services == null)
                {
                    locations.Remove(location);
                }
            }
            if (locations == null)
            {
                freightRate.ReceivedUnitPriceMax += 0;
                freightRate.ReceivedUnitPriceAVG += 0;
                return;
            }
            var quotationHollows = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7594 and ContainerTypeId eq {freightRate.ContainerTypeId}");
            var quotationGoods = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7596 and ContainerTypeId eq {freightRate.ContainerTypeId}");
            if (quotationHollows.Count > 0 && quotationGoods.Count > 0)
            {
                foreach (var item in quotationHollows)
                {
                    var check = locations.Any(x => x.Id == item.LocationId);
                    if (check)
                    {
                        freightRate.ReceivedUnitPriceMax = quotationHollows.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
                        freightRate.ReceivedUnitPriceAVG = quotationHollows.Where(x => x.UnitPrice > 0).Average(x => x.UnitPrice);
                    }
                    else
                    {
                        freightRate.ReceivedUnitPriceMax = 0;
                        freightRate.ReceivedUnitPriceAVG = 0;
                    }
                }
                foreach (var item in quotationGoods)
                {
                    var check = locations.Any(x => x.Id == item.LocationId);
                    if (check)
                    {
                        freightRate.ReceivedUnitPriceMax += quotationGoods.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
                        freightRate.ReceivedUnitPriceAVG += quotationGoods.Where(x => x.UnitPrice > 0).Average(x => x.UnitPrice);
                    }
                    else
                    {
                        freightRate.ReceivedUnitPriceMax += 0;
                        freightRate.ReceivedUnitPriceAVG += 0;
                    }
                }
            }
        }

        private async Task GetReturnUnitPrice(FreightRate freightRate)
        {
            var locations = await new Client(nameof(Location)).GetRawList<Location>($"?$filter=Active eq true and RegionId eq {freightRate.RegionReturnId}");
            if (locations == null)
            {
                freightRate.ReturnUnitPriceMax = 0;
                freightRate.ReturnUnitPriceAVG = 0;
                return;
            }
            foreach (var location in locations)
            {
                var services = await new Client(nameof(LocationService)).GetRawList<LocationService>($"?$filter=Active eq true and LocationId eq {location} and ServiceId in (7583, 7584, 7585, 7586)");
                if (services == null)
                {
                    locations.Remove(location);
                }
            }
            if (locations == null)
            {
                freightRate.ReturnUnitPriceMax = 0;
                freightRate.ReturnUnitPriceAVG = 0;
                return;
            }
            var quotationHollows = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7594 and ContainerTypeId eq {freightRate.ContainerTypeId}");
            var quotationGoods = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7596 and ContainerTypeId eq {freightRate.ContainerTypeId}");
            if (quotationHollows.Count > 0 && quotationGoods.Count > 0)
            {
                foreach (var item in quotationHollows)
                {
                    var check = locations.Any(x => x.Id == item.LocationId);
                    if (check)
                    {
                        freightRate.ReturnUnitPriceMax = quotationHollows.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
                        freightRate.ReturnUnitPriceAVG = quotationHollows.Where(x => x.UnitPrice > 0).Average(x => x.UnitPrice);
                    }
                    else
                    {
                        freightRate.ReturnUnitPriceMax = 0;
                        freightRate.ReturnUnitPriceAVG = 0;
                    }
                }
                foreach (var item in quotationGoods)
                {
                    var check = locations.Any(x => x.Id == item.LocationId);
                    if (check)
                    {
                        freightRate.ReturnUnitPriceMax += quotationGoods.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
                        freightRate.ReturnUnitPriceAVG += quotationGoods.Where(x => x.UnitPrice > 0).Average(x => x.UnitPrice);
                    }
                    else
                    {
                        freightRate.ReturnUnitPriceMax += 0;
                        freightRate.ReturnUnitPriceAVG += 0;
                    }
                }
            }
        }

        private async Task GetReceivedReturnUnitPrice(FreightRate freightRate)
        {
            var locations = await new Client(nameof(Location)).GetRawList<Location>($"?$filter=Active eq true and RegionId in ({freightRate.RegionReceivedId}, {freightRate.RegionReturnId})");
            if (locations == null)
            {
                freightRate.ReceivedReturnUnitPriceMax = 0;
                freightRate.ReceivedReturnUnitPriceAVG = 0;
                return;
            }
            foreach (var location in locations)
            {
                var services = await new Client(nameof(LocationService)).GetRawList<LocationService>($"?$filter=Active eq true and LocationId eq {location} and ServiceId in (7583, 7584)");
                if (services == null)
                {
                    locations.Remove(location);
                }
            }
            if (locations == null)
            {
                freightRate.ReceivedReturnUnitPriceMax = 0;
                freightRate.ReceivedReturnUnitPriceAVG = 0;
                return;
            }
            var quotations = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7596 and ContainerTypeId eq {freightRate.ContainerTypeId}");
            if (quotations.Count > 0)
            {
                foreach (var item in quotations)
                {
                    var check = locations.Any(x => x.Id == item.LocationId);
                    if (check)
                    {
                        freightRate.ReceivedReturnUnitPriceMax = quotations.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
                        freightRate.ReceivedReturnUnitPriceAVG = quotations.Where(x => x.UnitPrice > 0).Average(x => x.UnitPrice);
                    }
                    else
                    {
                        freightRate.ReceivedReturnUnitPriceMax = 0;
                        freightRate.ReceivedReturnUnitPriceAVG = 0;
                    }
                }

            }
        }

        private async Task GetShipUnitPrice(FreightRate freightRate)
        {
            var quotations = await new Client(nameof(Quotation)).GetRawList<Quotation>($"?$filter=Active eq true and TypeId eq 7598 and ContainerTypeId eq {freightRate.ContainerTypeId}");
            if (quotations.Count > 0)
            {
                freightRate.ShipUnitPriceMax = quotations.Where(x => x.UnitPrice > 0).Max(x => x.UnitPrice);
                freightRate.ShipUnitPriceAVG = quotations.Where(x => x.UnitPrice > 0).Average(x => x.UnitPrice);
            }
            else
            {
                freightRate.ShipUnitPriceMax = 0;
                freightRate.ShipUnitPriceAVG = 0;
            }
        }

        public async Task<MasterData> CheckContainerType(FreightRate freightRate)
        {
            MasterData container = null;
            var containerTypes = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and ParentId eq 7565");
            var containerTypeCodes = containerTypes.ToDictionary(x => x.Id);
            var containerTypeName = containerTypeCodes.GetValueOrDefault((int)freightRate.ContainerTypeId);
            var containers = await new Client(nameof(MasterData)).GetRawList<MasterData>($"?$filter=Active eq true and (contains(Name, '40HC') or contains(Name, '20DC'))");
            if (containerTypeName.Description.Contains("Cont 20"))
            {
                container = containers.Find(x => x.Name.Contains("20DC"));
            }
            else if (containerTypeName.Description.Contains("Cont 40"))
            {
                container = containers.Find(x => x.Name.Contains("40HC"));
            }
            return container;
        }

        public async Task ConfirmBusinessFreightRate()
        {
            if (freightRateEntity.Id <= 0)
            {
                Toast.Warning("Bạn phải lưu trước khi thực hiện thao tác này !!!");
                return;
            }
            await this.OpenPopup(
                featureName: "Confirm Business Freight Rate",
                factory: () =>
                {
                    var type = Type.GetType("TMS.UI.Business.Accountant.ConfirmBusinessFreightRateBL");
                    var instance = Activator.CreateInstance(type) as PopupEditor;
                    instance.Title = "Lấy dữ liệu sang biểu giá CVC";
                    instance.Entity = freightRateEntity;
                    return instance;
                });
        }
    }
}
