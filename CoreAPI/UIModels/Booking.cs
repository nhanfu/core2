using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Booking
{
    public string Id { get; set; }

    public int? TypeId { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string BookingNo { get; set; }

    public DateTime? BookingDate { get; set; }

    public string CustomerId { get; set; }

    public string PolId { get; set; }

    public string PodId { get; set; }

    public string ShipperId { get; set; }

    public string ConsigneeId { get; set; }

    public decimal? Volume { get; set; }

    public DateTime? PickupDate { get; set; }

    public DateTime? DropOffDate { get; set; }

    public int? StatusId { get; set; }

    public string Pic { get; set; }

    public string PhoneNumber { get; set; }

    public string PaymentTermId { get; set; }

    public string DeliveryTerm { get; set; }

    public string IncotermId { get; set; }

    public int? ShipmentTypeId { get; set; }

    public int? ServiceId { get; set; }

    public int? Service2Id { get; set; }

    public string HblNo { get; set; }

    public string HblTypeId { get; set; }

    public string CommodityId { get; set; }

    public string ServiceText { get; set; }

    public string SaleId { get; set; }

    public decimal? Quantity { get; set; }

    public string UnitId { get; set; }

    public decimal? CBM { get; set; }

    public string ShipperDes { get; set; }

    public string ConsigneeDes { get; set; }

    public string PortTransitId { get; set; }

    public string PlaceDeliveryId { get; set; }

    public string PickupAddress { get; set; }

    public string DropOffId { get; set; }

    public string PlaceStuffingId { get; set; }

    public string PlaceStuffingText { get; set; }

    public string ContactStuffing { get; set; }

    public DateTime? PlaceStuffingDate { get; set; }

    public DateTime? LadenOnBoardDate { get; set; }

    public DateTime? DocsCutOffDate { get; set; }

    public DateTime? SiCutOffDate { get; set; }

    public DateTime? VgmCutOffDate { get; set; }

    public DateTime? CyCutOffDate { get; set; }

    public DateTime? EmptyReturnDate { get; set; }

    public string FinalDestinationId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? EtdDate { get; set; }

    public DateTime? EtaDate { get; set; }

    public DateTime? EtdTsDate { get; set; }

    public DateTime? EtaTsDate { get; set; }

    public DateTime? ClosingTimeDate { get; set; }

    public decimal? FreightRate { get; set; }

    public decimal? DemFree { get; set; }

    public decimal? DetFree { get; set; }

    public string DeliveryAddress { get; set; }

    public string Remark { get; set; }

    public decimal? RateComfirm { get; set; }

    public string Dimension { get; set; }

    public string PoNo { get; set; }

    public string WareHouseId { get; set; }

    public DateTime? CutOffDate { get; set; }

    public string SpecialRequest { get; set; }

    public string DetailsGoods { get; set; }

    public string CarrierId { get; set; }

    public string PicVendor { get; set; }

    public string ReferenceNo { get; set; }

    public string Vessel { get; set; }

    public string VesselVoy { get; set; }

    public string OceanVessel { get; set; }

    public string OceanVesselVoy { get; set; }

    public string PickupId { get; set; }

    public string PickupAt { get; set; }

    public string DropOffAt { get; set; }

    public string Note { get; set; }

    public int? SeqKey { get; set; }

    public decimal? VolumeWeight { get; set; }

    public decimal? Kgs { get; set; }

    public decimal? CW { get; set; }

    public decimal? GW { get; set; }

    public string TypeMoveId { get; set; }

    public string VendorId { get; set; }

    public string ContPickupAtId { get; set; }

    public string ContReturnAtId { get; set; }

    public string MblNo { get; set; }

    public string MblTypeId { get; set; }

    public string FileNo { get; set; }

    public string RefNo { get; set; }

    public string AttachedFile { get; set; }

    public string Notes { get; set; }

    public string ReceiverIds { get; set; }

    public string CheckedBy { get; set; }

    public string FlightNo { get; set; }

    public string FlightNoConnect { get; set; }

    public string AgentId { get; set; }

    public string InvNo { get; set; }

    public string CustomsId { get; set; }

    public string TruckingTypeId { get; set; }

    public string DeliveryId { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int? VoucherTypeId { get; set; }

    public string Url { get; set; }

    public string FormatChat { get; set; }

    public string UserViewIds { get; set; }

    public string UserApprovedIds { get; set; }

    public string ForwardId { get; set; }

    public string BookingLocalId { get; set; }

    public string BookingNoteId { get; set; }

    public string BookingOrderId { get; set; }

    public bool IsSend { get; set; }

    public bool NoApproved { get; set; }

    public string PlaceReceiptId { get; set; }

    public string ShipmentId { get; set; }

    public string ShipmentDetailId { get; set; }

    public string QuotationId { get; set; }

    public string QuotationCode { get; set; }

    public string FeatureName { get; set; }

    public string DepartmentId { get; set; }

    public int? BookingTypeId { get; set; }

    public virtual ICollection<BookingDetail> BookingDetail { get; set; } = new List<BookingDetail>();

    public virtual Inquiry Quotation { get; set; }

    public virtual ICollection<Shipment> Shipment { get; set; } = new List<Shipment>();
}
