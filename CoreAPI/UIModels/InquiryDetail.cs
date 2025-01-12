using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class InquiryDetail
{
    public string Id { get; set; }

    public string DescriptionId { get; set; }

    public bool OtherFee { get; set; }

    public string InquiryId { get; set; }

    public int? ServiceId { get; set; }

    public string PodId { get; set; }

    public string PolId { get; set; }

    public decimal? TotalAmount { get; set; }

    public string UserReceiverId { get; set; }

    public string GroupReceiverId { get; set; }

    public DateTime? Deadline { get; set; }

    public bool IsApproved { get; set; }

    public bool IsDecline { get; set; }

    public string UserApprovedId { get; set; }

    public string UserDeclineId { get; set; }

    public string Note { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public int? StatusId { get; set; }

    public string Freq { get; set; }

    public string CurrencyId { get; set; }

    public string CurrencyCode { get; set; }

    public string CarrierId { get; set; }

    public string VendorId { get; set; }

    public string ProviderId { get; set; }

    public string PlaceReceiptId { get; set; }

    public string PlaceDeliveryId { get; set; }

    public string FinalDestinationId { get; set; }

    public string ViaId { get; set; }

    public string TransitTime { get; set; }

    public decimal? Cost { get; set; }

    public decimal? Rate { get; set; }

    public decimal? Vat { get; set; }

    public bool IsObh { get; set; }

    public string GroupFee { get; set; }

    public string RouteId { get; set; }

    public decimal? MinLCLCost { get; set; }

    public decimal? MinLCLRate { get; set; }

    public string Pickup { get; set; }

    public string Delivery { get; set; }

    public int? Order { get; set; }

    public decimal? UnitPrice { get; set; }

    public DateTime? CutOff { get; set; }

    public string PayeeId { get; set; }

    public string TruckTypeId { get; set; }

    public string UnitId { get; set; }

    public decimal? Quantity { get; set; }

    public string ScheduleIds { get; set; }

    public string ScheduleIdsText { get; set; }

    public string PricingId { get; set; }

    public int? VoucherTypeId { get; set; }

    public DateTime? EtaDate { get; set; }

    public DateTime? EtdDate { get; set; }

    public string FormatChat { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }

    public decimal? CW { get; set; }

    public decimal? GW { get; set; }

    public decimal? CBM { get; set; }

    public bool IsFreight { get; set; }

    public bool IsContainer { get; set; }

    public bool IsCBM { get; set; }

    public bool IsKGS { get; set; }

    public bool IsGW { get; set; }

    public int? ProgressId { get; set; }

    public bool IsSend { get; set; }

    public decimal? MinQuantityCost { get; set; }

    public decimal? MinQuantityRate { get; set; }

    public string PayerId { get; set; }

    public string OtherUnitId { get; set; }

    public bool IsGrossWeight { get; set; }

    public bool DisableRow { get; set; }

    public bool IsLogistics { get; set; }

    public bool IsTrucking { get; set; }

    public string QuotationId { get; set; }
}
