using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class Asset
{
    public string Id { get; set; }

    public string Code { get; set; }

    public string UnitId { get; set; }

    public string Name { get; set; }

    public string AccGroupId { get; set; }

    public string GroupCode { get; set; }

    public string CountryId { get; set; }

    public int? Year { get; set; }

    public string TypeText { get; set; }

    public string DepartmentId { get; set; }

    public string ReasonChange { get; set; }

    public DateTime? ChangeDate { get; set; }

    public string AccAmoutId { get; set; }

    public decimal? Amount { get; set; }

    public string AccCostId { get; set; }

    public decimal? DepAmount { get; set; }

    public DateTime? DepFromDate { get; set; }

    public decimal? UseMonth { get; set; }

    public decimal? RemainMonth { get; set; }

    public DateTime? DepToDate { get; set; }

    public string AccDepId { get; set; }

    public decimal? DepAmountMonth { get; set; }

    public string DescriptionId { get; set; }

    public string DescriptionIdText { get; set; }

    public decimal? Accumulated { get; set; }

    public decimal? DepMonth { get; set; }

    public decimal? DepreciatedAmount { get; set; }

    public decimal? RemainAmount { get; set; }

    public bool IsDisposal { get; set; }

    public decimal? WarrantyPeriod { get; set; }

    public string PartnerId { get; set; }

    public string AttachedFile { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
