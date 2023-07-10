using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class SalaryFormulaSettings
    {
        public int Id { get; set; }
        public decimal? SocialInsurance { get; set; }
        public decimal? HealthInsurance { get; set; }
        public decimal? AccidentInsurance { get; set; }
        public decimal? UnionDues { get; set; }
        public decimal? SocialInsuranceAbatement { get; set; }
        public decimal? HealthInsuranceAbatement { get; set; }
        public decimal? AccidentInsuranceAbatement { get; set; }
        public decimal? UnionDuesAbatement { get; set; }
        public decimal? DeductionsIndividual { get; set; }
        public decimal? DeductionsDependent { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
