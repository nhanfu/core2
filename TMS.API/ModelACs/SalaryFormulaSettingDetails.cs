using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class SalaryFormulaSettingDetails
    {
        public int Id { get; set; }
        public int? SalaryFormulaSettingsId { get; set; }
        public decimal? FromValue { get; set; }
        public decimal? ToValue { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? Deductions { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
