using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TMS.API.Models;

namespace TMS.API.ViewModels
{
    public class ImportLocationVM
    {
        public string Type { get; set; }
        public string RegionText { get; set; }
        public string RegionTextEn { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DescriptionEn { get; set; }
        public string User { get; set; }
    }

    public class ImportVendorLocationVM
    {
        public string Region { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string User { get; set; }
    }

    public class ImportCommodityVM
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DescriptionEn { get; set; }
        public string User { get; set; }
    }

    public class ImportRouteVM
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string User { get; set; }
    }

    public class ImportShip
    {
        public string Level { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string ShipBrandText { get; set; }
        public string ShipBrandTextEn { get; set; }
        public string User { get; set; }
    }

    public class ImportVendorVM
    {
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string CommodityText { get; set; }
        public string CommodityTextEn { get; set; }
        public string UserName { get; set; }
        public string Code { get; set; }
        public string CompanyName { get; set; }
        public string PhoneNumber { get; set; }
        public string TaxCode { get; set; }
        public string Address { get; set; }
        public string User { get; set; }
    }

    public class ImportQuotationVM
    {
        public string StartDate { get; set; }
        public string RouteText { get; set; }
        public string RouteTextEn { get; set; }
        public string BrandShipText { get; set; }
        public string BrandShipTextEn { get; set; }
        public string ContainerTypeText { get; set; }
        public string ContainerTypeTextEn { get; set; }
        public string PolicyTypeText { get; set; }
        public string PolicyTypeTextEn { get; set; }
        public string UnitPrice { get; set; }
        public string UnitPrice1 { get; set; }
        public string UnitPrice2 { get; set; }
        public string UnitPrice3 { get; set; }
        public string Note { get; set; }
        public string VendorLocationText { get; set; }
        public string VendorLocationTextEn { get; set; }
        public string VendorText { get; set; }
        public string VendorTextEn { get; set; }
        public string LocationText { get; set; }
        public string LocationTextEn { get; set; }
        public string User { get; set; }
    }

    public class ImportQuotationExpenseVM
    {
        public string BrandShipText { get; set; }
        public string BrandShipTextEn { get; set; }
        public string ExpenseTypeText { get; set; }
        public string ExpenseTypeTextEn { get; set; }
        public string VSC { get; set; }
        public string VS20UnitPrice { get; set; }
        public string VS40UnitPrice { get; set; }
        public string DOUnitPrice { get; set; }
    }

    public class ImportMasterDataVM
    {
        public string Level { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string Description { get; set; }
        public string DescriptionEn { get; set; }
        public string Enum { get; set; }
        public string Code { get; set; }
        public string User { get; set; }
    }

    public class ImportTransportationPlan
    {
        public string RouteTextEn { get; set; }
        public string BossTextEn { get; set; }
        public string CommodityTextEn { get; set; }
        public string ContainerTextEn { get; set; }
        public string ReceivedTextEn { get; set; }
        public string PlanDate { get; set; }
        public string UserText { get; set; }
        public string RouteText { get; set; }
        public string BossText { get; set; }
        public string CommodityText { get; set; }
        public string ContainerText { get; set; }
        public string ContractText { get; set; }
        public string QuotationText { get; set; }
        public string ClosingDate { get; set; }
        public string ReceivedText { get; set; }
        public string TotalContainer { get; set; }
        public string TotalContainerUsing { get; set; }
        public string Notes { get; set; }
        public string Notes1 { get; set; }
        public string User { get; set; }
        public string Name { get; set; }
    }

    public class ImportCommodityValue
    {
        public string SaleText { get; set; }
        public string BossText { get; set; }
        public string BossTextEn { get; set; }
        public string CommodityText { get; set; }
        public string CommodityTextEn { get; set; }
        public string TotalPrice1 { get; set; }
        public string TotalPrice2 { get; set; }
        public string IsWetText { get; set; }
        public string Notes { get; set; }
        public string JourneyText { get; set; }
        public string IsBoughtText { get; set; }
        public string CustomerTypeText { get; set; }
        public string SteamingTerms { get; set; }
        public string BreakTerms { get; set; }
    }

    public class ImportBooking
    {
        public string BrandShipText { get; set; }
        public string BrandShipTextEn { get; set; }
        public string LineText { get; set; }
        public string LineTextEn { get; set; }
        public string ShipText { get; set; }
        public string ShipTextEn { get; set; }
        public string Trip { get; set; }
        public string BookingNo { get; set; }
        public string StartShip { get; set; }
        public string PickupEmptyText { get; set; }
        public string PickupEmptyTextEn { get; set; }
        public string PortLoadingText { get; set; }
        public string PortLoadingTextEn { get; set; }
        public string Teus20 { get; set; }
        public string Teus40 { get; set; }
        public string Teus20Using { get; set; }
        public string Teus40Using { get; set; }
        public string Teus20Remain { get; set; }
        public string Teus40Remain { get; set; }
        public string Note1 { get; set; }
        public string PackingMethodText { get; set; }
        public string PackingMethodTextEn { get; set; }
        public string Note { get; set; }
        public string User { get; set; }
    }

    public class ImportTransportation
    {
        public string ListExport { get; set; }
        public string ListExportEn { get; set; }
        public string Route { get; set; }
        public string RouteEn { get; set; }
        public string Booking { get; set; }
        public string BookingEn { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string BrandShip { get; set; }
        public string BrandShipEn { get; set; }
        public string Line { get; set; }
        public string LineEn { get; set; }
        public string Ship { get; set; }
        public string ShipEn { get; set; }
        public string Trip { get; set; }
        public string Bill { get; set; }
        public string ClosingDate { get; set; }
        public string StartDate { get; set; }
        public string Closing { get; set; }
        public string ClosingEn { get; set; }
        public string Soc { get; set; }
        public string SocEn { get; set; }
        public string SplitBill { get; set; }
        public string IsEmptyCombination { get; set; }
        public string EmptyCombinationId { get; set; }
        public string EmptyCombinationIdEn { get; set; }
        public string IsClosingCustomer { get; set; }
        public string ContainerTypeId { get; set; }
        public string ContainerTypeIdEn { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public string Boss { get; set; }
        public string BossEn { get; set; }
        public string Sale { get; set; }
        public string SaleEn { get; set; }
        public string CommodityId { get; set; }
        public string CommodityIdEn { get; set; }
        public string Cont20 { get; set; }
        public string Cont40 { get; set; }
        public string Weight { get; set; }
        public string Received { get; set; }
        public string ReceivedEn { get; set; }
        public string ClosingNotes { get; set; }
        public string ClosingUser { get; set; }
        public string ClosingDriver { get; set; }
        public string Closingtruck { get; set; }
        public string PickupEmptyId { get; set; }
        public string PickupEmptyIdEn { get; set; }
        public string PortLoadingId { get; set; }
        public string PortLoadingIdEn { get; set; }
        public string IsClampingFee { get; set; }
        public string ClosingUnitPrice { get; set; }
        public string IsEmptyLift { get; set; }
        public string IsLanding { get; set; }
        public string LiftFee { get; set; }
        public string LandingFee { get; set; }
        public string CheckFee { get; set; }
        public string OrtherFee { get; set; }
        public string OrtherFeeInvoinceNo { get; set; }
        public string CollectOnBehaftFee { get; set; }
        public string CollectOnBehaftInvoinceNoFee { get; set; }
        public string InsuranceFee { get; set; }
        public string TotalFee { get; set; }
        public string ShipPrice { get; set; }
        public string ShipRoses { get; set; }
        public string ShipNote { get; set; }
        public string ShipDate { get; set; }
        public string Dem { get; set; }
        public string DemDate { get; set; }
        public string LeftDate { get; set; }
        public string ReturnDate { get; set; }
        public string ClosingCont { get; set; }
        public string ShellDate { get; set; }
        public string ReturnId { get; set; }
        public string ReturnIdEn { get; set; }
        public string ReturnNotes { get; set; }
        public string ReturnUserId { get; set; }
        public string ReturnVendorId { get; set; }
        public string ReturnVendorIdEn { get; set; }
        public string ReturnDriverId { get; set; }
        public string ReturnTruckId { get; set; }
        public string NotificationCount { get; set; }
        public string PortLiftId { get; set; }
        public string PortLiftIdEn { get; set; }
        public string ReturnEmptyId { get; set; }
        public string ReturnEmptyIdEn { get; set; }
        public string ReturnUnitPrice { get; set; }
        public string IsLiftFee { get; set; }
        public string IsClosingEmptyFee { get; set; }
        public string ReturnLiftFee { get; set; }
        public string ReturnClosingFee { get; set; }
        public string ReturnDo { get; set; }
        public string ReturnVs { get; set; }
        public string ReturnCheckFee { get; set; }
        public string ReturnOrtherFee { get; set; }
        public string ReturnOrtherInvoinceFee { get; set; }
        public string ReturnCollectOnBehaftFee { get; set; }
        public string ReturnCollectOnBehaftInvoinceFee { get; set; }
        public string ReturnPlusFee { get; set; }
        public string ReturnTotalFee { get; set; }
        public string IsKt { get; set; }
        public string Notes { get; set; }
        public string InsertedBy { get; set; }
        public string Bet { get; set; }
    }

    public class ImportTeus
    {
        public string BrandShipText { get; set; }
        public string BrandShipTextEn { get; set; }
        public string ShipText { get; set; }
        public string ShipTextEn { get; set; }
        public string Trip { get; set; }
        public string StartShip { get; set; }
        public string Teus20 { get; set; }
        public string Teus40 { get; set; }
        public string Teus20Using { get; set; }
        public string Teus40Using { get; set; }
        public string Teus20Remain { get; set; }
        public string Teus40Remain { get; set; }
        public string User { get; set; }
    }

    public class ImportTransportationContract
    {
        public string Code { get; set; }
        public string ContractName { get; set; }
        public string ContractNo { get; set; }
        public string BossText { get; set; }
        public string BossTextEn { get; set; }
        public string CompanyName { get; set; }
        public string UserText { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string SignDate { get; set; }
        public string TotalPrice { get; set; }
        public string Notes { get; set; }
        public string User { get; set; }
    }

    public class CheckTransportationVM
    {
        public string No { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string Boss { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public int? Cont20 { get; set; }
        public int? Cont40 { get; set; }
        public string Received { get; set; }
        public string PickupEmpty { get; set; }
        public string PortLoading { get; set; }
        public decimal? LiftFee { get; set; }
        public decimal? LandingFee { get; set; }
        public decimal? FeeVat1 { get; set; }
        public decimal? FeeVat2 { get; set; }
        public decimal? FeeVat3 { get; set; }
        public decimal? Fee1 { get; set; }
        public decimal? Fee2 { get; set; }
        public decimal? Fee3 { get; set; }
        public decimal? Fee4 { get; set; }
        public decimal? Fee5 { get; set; }
        public decimal? TotalPriceAfterTax { get; set; }
        public decimal? CollectOnSupPrice { get; set; }
    }

    public class CheckCompineTransportationVM
    {
        public string No { get; set; }
        public string Vendor { get; set; }
        public int? Id { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string Boss { get; set; }
        public string ContainerNo { get; set; }
        public string SealNo { get; set; }
        public int? Cont20 { get; set; }
        public int? Cont40 { get; set; }
        public string Received { get; set; }
        public string PickupEmpty { get; set; }
        public string PortLoading { get; set; }
        public decimal? LiftFee { get; set; }
        public decimal? LiftFeeDB { get; set; }
        public decimal? LandingFee { get; set; }
        public decimal? LandingFeeDB { get; set; }
        public decimal? FeeVat1 { get; set; }
        public decimal? FeeVat1DB { get; set; }
        public decimal? FeeVat2 { get; set; }
        public decimal? FeeVat2DB { get; set; }
        public decimal? FeeVat3 { get; set; }
        public decimal? FeeVat3DB { get; set; }
        public decimal? Fee1 { get; set; }
        public decimal? Fee1DB { get; set; }
        public decimal? Fee2 { get; set; }
        public decimal? Fee2DB { get; set; }
        public decimal? Fee3 { get; set; }
        public decimal? Fee3DB { get; set; }
        public decimal? Fee4 { get; set; }
        public decimal? Fee4DB { get; set; }
        public decimal? Fee5 { get; set; }
        public decimal? Fee6 { get; set; }
        public decimal? Fee5DB { get; set; }
        public decimal? TotalPriceAfterTax { get; set; }
        public decimal? TotalPriceAfterTaxDB { get; set; }
        public decimal? CollectOnSupPrice { get; set; }
        public decimal? ClosingPercentCheck { get; set; }
    }

    public class ImportObjectVM
    {
        public string Groupid { get; set; }
        public string YearCreated { get; set; }
        public string GroupSaleId { get; set; }
        public string Code { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string StaffName { get; set; }
        public string PositionName { get; set; }
        public string ClassifyName { get; set; }
        public string BankNo { get; set; }
        public string BankName { get; set; }
        public string CityName { get; set; }
        public string DepartmentName { get; set; }
        public string BranchName { get; set; }
        public string VendorId { get; set; }
        public string Notes { get; set; }
        public string UserName { get; set; }
    }
}
