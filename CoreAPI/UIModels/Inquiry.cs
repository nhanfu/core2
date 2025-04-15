using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Inquiry
{
    public string Id { get; set; }

    public int? TypeId { get; set; }

    public string Code { get; set; }

    public string CustomerId { get; set; }

    public string PodId { get; set; }

    public string PolId { get; set; }

    public string DestinationId { get; set; }

    public decimal? Quantity { get; set; }

    public string DeliveryTerm { get; set; }

    public string CommodityId { get; set; }

    public DateTime? EtdDate { get; set; }

    public DateTime? EtaDate { get; set; }

    public string ContainerTypeId { get; set; }

    public string UnitId { get; set; }

    public string DeminsionText { get; set; }

    public decimal? DeminsionLength { get; set; }

    public decimal? DeminsionWidth { get; set; }

    public decimal? DeminsionHeight { get; set; }

    public decimal? DeminsionQuantity { get; set; }

    public decimal? CW { get; set; }

    public decimal? GW { get; set; }

    public decimal? CBM { get; set; }

    public string Pickup { get; set; }

    public string Delivery { get; set; }

    public string Attachment { get; set; }

    public string SaleId { get; set; }

    public string ServiceText { get; set; }

    public string UserReceiverText { get; set; }

    public string GroupReceiverId { get; set; }

    public int? StatusId { get; set; }

    public string Note { get; set; }

    public DateTime? SendDate { get; set; }

    public string SendBy { get; set; }

    public string ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public int? SeqKey { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int? FreightStateId { get; set; }

    public int? ShipmentTypeId { get; set; }

    public DateTime? ServiceDate { get; set; }

    public string Address { get; set; }

    public string Pic { get; set; }

    public string PhoneNumber { get; set; }

    public DateTime? TransitTime { get; set; }

    public string TransitPortId { get; set; }

    public string IncotermId { get; set; }

    public string ShipperId { get; set; }

    public string ShipperIdName { get; set; }

    public string AgentId { get; set; }

    public string AgentIdText { get; set; }

    public string Subject { get; set; }

    public int? ServiceId { get; set; }

    public string Cont20Id { get; set; }

    public string Cont20x2Id { get; set; }

    public string Cont40Id { get; set; }

    public string Cont45Id { get; set; }

    public string Cont40HCId { get; set; }

    public string ConsigneeId { get; set; }

    public string ConsigneeIdText { get; set; }

    public string Remark { get; set; }

    public bool IsSimple { get; set; }

    public decimal? VolumeWeight { get; set; }

    public decimal? Kgs { get; set; }

    public DateTime? PickupDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public string EmptyPickupId { get; set; }

    public string EmptyReturnId { get; set; }

    public string CustomsOfficeId { get; set; }

    public DateTime? CargoReadyDate { get; set; }

    public int? VoucherTypeId { get; set; }

    public string StatusText { get; set; }

    public decimal? ExchangeRateVND { get; set; }

    public decimal? ExchangeRateUSD { get; set; }

    public string UserReceiverIds { get; set; }

    public string GroupReceiverIds { get; set; }

    public string UserReceiverIdsText { get; set; }

    public string GroupReceiverIdsText { get; set; }

    public string Url { get; set; }

    public string FormatChat { get; set; }

    public int? ProgressId { get; set; }

    public string InquiryDetailId { get; set; }

    public decimal? DemFree { get; set; }

    public decimal? DetFree { get; set; }

    public string PkEmptyId { get; set; }

    public string ReturnsId { get; set; }

    public string UserViewIds { get; set; }

    public string UserApprovedIds { get; set; }

    public string ForwardId { get; set; }

    public bool NoApproved { get; set; }

    public string BookingLocalId { get; set; }

    public string FeatureName { get; set; }
}
