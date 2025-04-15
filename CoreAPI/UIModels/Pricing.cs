using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Pricing
{
    public string Id { get; set; }

    public string Code { get; set; }

    public int? TypeId { get; set; }

    public string VendorTypeId { get; set; }

    public decimal? DemFree { get; set; }

    public decimal? DetFree { get; set; }

    public decimal? FreeTime { get; set; }

    public decimal? Sto { get; set; }

    public string VendorId { get; set; }

    public string AgentId { get; set; }

    public DateTime? BookingDate { get; set; }

    public string CarrierId { get; set; }

    public string PolId { get; set; }

    public string PodId { get; set; }

    public string ViaId { get; set; }

    public string PkEmptyId { get; set; }

    public string ReturnsId { get; set; }

    public string ScheduleIds { get; set; }

    public string ScheduleIdsText { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string TransitTime { get; set; }

    public DateTime? CreatedOn { get; set; }

    public int? StatusId { get; set; }

    public string Cont20Text { get; set; }

    public string Cont40Text { get; set; }

    public string ContText { get; set; }

    public string CommodityId { get; set; }

    public string ServiceText { get; set; }

    public string CutOff { get; set; }

    public string MakeupLevel { get; set; }

    public string Note { get; set; }

    public decimal? ExchangeRate { get; set; }

    public bool IsPublic { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsLock { get; set; }

    public int? VoucherTypeId { get; set; }

    public string AmountText { get; set; }

    public int? ServiceId { get; set; }

    public string ModeId { get; set; }

    public decimal? Scc { get; set; }

    public decimal? Fsc { get; set; }

    public decimal? MinQuantity { get; set; }

    public string Url { get; set; }

    public string AttachedFile { get; set; }

    public string FormatChat { get; set; }

    public string EmptyReturnId { get; set; }

    public string FeatureName { get; set; }
}
