using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ShipmentDO
{
    public string Id { get; set; }

    public string ShipmentId { get; set; }

    public int? TypeId { get; set; }

    public string Code { get; set; }

    public string DoNo { get; set; }

    public DateTime? DoDate { get; set; }

    public DateTime? TimeDeliveryDate { get; set; }

    public string DeliveryPlaceId { get; set; }

    public string ShipperId { get; set; }

    public string ShipperIdText { get; set; }

    public string ConsigneeId { get; set; }

    public string ConsigneeIdText { get; set; }

    public string ShipperDes { get; set; }

    public string ConsigneeDes { get; set; }

    public string Representative1 { get; set; }

    public string Representative2 { get; set; }

    public string Position1 { get; set; }

    public string Position2 { get; set; }

    public string IndentityCard1 { get; set; }

    public string IndentityCard2 { get; set; }

    public string Tel1 { get; set; }

    public string Tel2 { get; set; }

    public string Reason { get; set; }

    public string Evidence { get; set; }

    public string SpecialNotes { get; set; }

    public string ConditionGoods { get; set; }

    public string DescriptionGoods { get; set; }

    public int? SeqKey { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
