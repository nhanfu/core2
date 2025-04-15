using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentSI
{
    public string Id { get; set; }

    public string ShipmentId { get; set; }

    public int? TypeId { get; set; }

    public string Code { get; set; }

    public string BookingNo { get; set; }

    public DateTime? IssueDate { get; set; }

    public string MblTypeId { get; set; }

    public string PolId { get; set; }

    public string PodId { get; set; }

    public string PlaceDeliveryId { get; set; }

    public string FinalDestinationId { get; set; }

    public DateTime? LoadingDate { get; set; }

    public string Vessel { get; set; }

    public string VesselVoy { get; set; }

    public string PaymentTermId { get; set; }

    public string VendorId { get; set; }

    public string Pic { get; set; }

    public string ShipperDes { get; set; }

    public string ConsigneeDes { get; set; }

    public string ConsigneeId { get; set; }

    public string ConsigneeIdText { get; set; }

    public string NotifyPartyDes { get; set; }

    public string NotifyPartyId { get; set; }

    public string NotifyPartyIdText { get; set; }

    public string RealShipperId { get; set; }

    public string RealShipperIdText { get; set; }

    public string RealShipperDes { get; set; }

    public string RealConsigneeId { get; set; }

    public string RealConsigneeIdText { get; set; }

    public string RealConsigneeDes { get; set; }

    public string Notes { get; set; }

    public string ShippingMark { get; set; }

    public string DetailsGoods { get; set; }

    public string DeliveryTermId { get; set; }

    public string ClauseId { get; set; }

    public string ContainerText { get; set; }

    public decimal? GW { get; set; }

    public decimal? CBM { get; set; }

    public int? SeqKey { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string Package { get; set; }

    public int? ProgressId { get; set; }

    public int? SITypeId { get; set; }

    public string CustomerId { get; set; }

    public int? StatusId { get; set; }

    public string FormatChat { get; set; }

    public int? VoucherTypeId { get; set; }

    public bool IsSend { get; set; }

    public string HblTypeId { get; set; }

    public decimal? Quantity { get; set; }

    public string Remark { get; set; }

    public string UserReceiverId { get; set; }

    public string UserApprovedIds { get; set; }

    public int? ServiceId { get; set; }

    public string UserCreateId { get; set; }

    public string UnitId { get; set; }

    public string FeatureName { get; set; }

    public string ReceiverIds { get; set; }

    public string ShipmentFeature { get; set; }

    public string RecordId { get; set; }
}
