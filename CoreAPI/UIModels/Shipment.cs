using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Shipment
{
    public string Id { get; set; }

    public string ParentId { get; set; }

    public int? TypeId { get; set; }

    public string Code { get; set; }

    public DateTime? ShipmentDate { get; set; }

    public string PodId { get; set; }

    public string PolId { get; set; }

    public DateTime? EtdDate { get; set; }

    public DateTime? EtaDate { get; set; }

    public string Vessel { get; set; }

    public string VesselVoy { get; set; }

    public string OceanVessel { get; set; }

    public string OceanVesselVoy { get; set; }

    public string CommodityId { get; set; }

    public string PoNo { get; set; }

    public string VendorId { get; set; }

    public string AgentId { get; set; }

    public string BookingNo { get; set; }

    public decimal? CW { get; set; }

    public decimal? GW { get; set; }

    public decimal? CBM { get; set; }

    public string KGS { get; set; }

    public string CarrierId { get; set; }

    public DateTime? AtdDate { get; set; }

    public DateTime? AtaDate { get; set; }

    public string OtherRefno { get; set; }

    public string Notes { get; set; }

    public string DeliveryTermId { get; set; }

    public string CyId { get; set; }

    public string IncotermId { get; set; }

    public string Service { get; set; }

    public string SubscribersIds { get; set; }

    public decimal? Quantity { get; set; }

    public string UnitId { get; set; }

    public decimal? TotalCW { get; set; }

    public decimal? TotalCBM { get; set; }

    public string ForwardId { get; set; }

    public string ReceiverIds { get; set; }

    public string UserViewIds { get; set; }

    public string UserApprovedIds { get; set; }

    public int? StatusId { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }

    public string CustomerId { get; set; }

    public string HblNo { get; set; }

    public string HblTypeId { get; set; }

    public string NumberOriginalIdText { get; set; }

    public string NumberOriginalId { get; set; }

    public string MblNo { get; set; }

    public string MblTypeId { get; set; }

    public string ExportReferences { get; set; }

    public string SeriBillNo { get; set; }

    public DateTime? Deadline { get; set; }

    public string SaleId { get; set; }

    public string QuotationId { get; set; }

    public string HSCode { get; set; }

    public DateTime? SaillingDate { get; set; }

    public string PlaceReceiptId { get; set; }

    public string PortTransitId { get; set; }

    public string PortTransit2Id { get; set; }

    public string PortTransit3Id { get; set; }

    public string PlaceDeliveryId { get; set; }

    public string FinalDestinationId { get; set; }

    public DateTime? ClosingDate { get; set; }

    public string ShipperId { get; set; }

    public string ConsigneeId { get; set; }

    public string NotifyPartyId { get; set; }

    public string ForDeliveryGoodsId { get; set; }

    public string ShipperDes { get; set; }

    public string ConsigneeDes { get; set; }

    public string NotifyPartyDes { get; set; }

    public string ForDeliveryGoodsDes { get; set; }

    public string IssuingId { get; set; }

    public string IssuingDes { get; set; }

    public string DetailsGoods { get; set; }

    public string AgentCode { get; set; }

    public string AccountNo { get; set; }

    public string AccountingInformation { get; set; }

    public string ShippingMark { get; set; }

    public string TypeMoveId { get; set; }

    public string PlaceIssueId { get; set; }

    public DateTime? IssueDate { get; set; }

    public string PaymentTermId { get; set; }

    public decimal? DemFree { get; set; }

    public decimal? DetFree { get; set; }

    public string FreightPayableId { get; set; }

    public string FreightChargeId { get; set; }

    public string PrepaidId { get; set; }

    public string TotalPrepaidId { get; set; }

    public decimal? Storage { get; set; }

    public decimal? Freetime { get; set; }

    public string ClauseId { get; set; }

    public string WarehouseId { get; set; }

    public decimal? Measuament { get; set; }

    public string CargoType { get; set; }

    public DateTime? PickupDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int? ShipmentTypeId { get; set; }

    public string InWords { get; set; }

    public string NominationParty { get; set; }

    public int? SeqKey { get; set; }

    public DateTime? SeqDate { get; set; }

    public string FreightId { get; set; }

    public DateTime? LockDate { get; set; }

    public bool IsDirectMaster { get; set; }

    public decimal? NoPieces { get; set; }

    public string KgsLgl { get; set; }

    public string RateClass { get; set; }

    public string CommodityItemNo { get; set; }

    public decimal? ChargeableWeight { get; set; }

    public decimal? RateChange { get; set; }

    public decimal? Total { get; set; }

    public string NatureQuantityGoods { get; set; }

    public string OtherCharges { get; set; }

    public string CurrencyRatesText { get; set; }

    public string CCCharges { get; set; }

    public DateTime? ExecutedDate { get; set; }

    public string Place { get; set; }

    public string AirportDeparture { get; set; }

    public string ReferenceNumber { get; set; }

    public string OptinalShippingInformation { get; set; }

    public DateTime? FlightsDate { get; set; }

    public string CFlights1 { get; set; }

    public DateTime? CFlights1Date { get; set; }

    public string CFlights2 { get; set; }

    public DateTime? CFlights2Date { get; set; }

    public string To1 { get; set; }

    public string ByFirstCarrier { get; set; }

    public string To2 { get; set; }

    public string By1 { get; set; }

    public string To3 { get; set; }

    public string By2 { get; set; }

    public string Currency { get; set; }

    public string CHGSCode { get; set; }

    public string WTVALId { get; set; }

    public string OtherId { get; set; }

    public string DeclaredCarriage { get; set; }

    public string DeclaredCustoms { get; set; }

    public string SpecialHandling { get; set; }

    public string AmountInsurance { get; set; }

    public string SCI { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string ServiceId { get; set; }

    public string PartyContact { get; set; }

    public string ContainerText { get; set; }

    public string ShipmentId { get; set; }

    public string ClearanceNo { get; set; }

    public DateTime? ClearanceDate { get; set; }

    public string CdsTypeId { get; set; }

    public string ThreadingCdsId { get; set; }

    public string CustomsOfficeId { get; set; }

    public string CommercialInvoiceNo { get; set; }

    public string HblSeaNo { get; set; }

    public decimal? CsdEdit { get; set; }

    public string SubService { get; set; }

    public string CoFormId { get; set; }

    public decimal? CoQuantity { get; set; }

    public string ContainerNo { get; set; }

    public string TruckerId { get; set; }

    public string TruckNo { get; set; }

    public string Driver { get; set; }

    public string PickupId { get; set; }

    public string PickupAddress { get; set; }

    public string PickupContact { get; set; }

    public string DeliveryId { get; set; }

    public string DeliveryAddress { get; set; }

    public string DeliveryContact { get; set; }

    public string EmtyPickupId { get; set; }

    public string CommodityText { get; set; }

    public decimal? Packages { get; set; }

    public DateTime? TruckStorageDate { get; set; }

    public decimal? NumberDaysTruck { get; set; }

    public DateTime? MoocStorageDate { get; set; }

    public decimal? NumberDaysMooc { get; set; }

    public DateTime? ClosingTime { get; set; }

    public string ContainerTypeId { get; set; }

    public string ShipmentDOId { get; set; }

    public string ShipmentSIId { get; set; }

    public bool NoApproved { get; set; }

    public decimal? VolumeWeight { get; set; }

    public string AirportDepartureId { get; set; }

    public string PlaceId { get; set; }

    public string ToId { get; set; }

    public string SaleIds { get; set; }

    public string DocsNo { get; set; }

    public DateTime? DocsDate { get; set; }

    public DateTime? MblDate { get; set; }

    public string BookingLocalId { get; set; }

    public string QuotationCode { get; set; }
}
