using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class EmployeePayroll
    {
        public int Id { get; set; }
        public int? StaffId { get; set; }
        public string EMPCode { get; set; }
        public string FullName { get; set; }
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public string AccountNum { get; set; }
        public decimal? Salary { get; set; }
        public int? WorkdaysOfMonth { get; set; }
        public int? NumOfWorkdays { get; set; }
        public decimal? SalaryByNumOfWorkdays { get; set; }
        public decimal? OrtherSalary { get; set; }
        public decimal? FoodAllowance { get; set; }
        public decimal? ClothesAllowance { get; set; }
        public decimal? OrtherAllowance { get; set; }
        public decimal? TotalSalary { get; set; }
        public decimal? InsuranceDeduction { get; set; }
        public decimal? UnionDuesDeduction { get; set; }
        public decimal? PersonalIncomeDeduction { get; set; }
        public decimal? OrtherDeduction { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? ActualSalary { get; set; }
        public decimal? AdvanceSalaryPay { get; set; }
        public decimal? EndMonthSalaryPay { get; set; }
        public string Notes { get; set; }
        public decimal? SocialInsuranceSalary { get; set; }
        public decimal? SocialInsurance { get; set; }
        public decimal? HealthInsurance { get; set; }
        public decimal? AccidentInsurance { get; set; }
        public decimal? UnionDues { get; set; }
        public decimal? TotalProvisioning { get; set; }
        public decimal? SocialInsuranceAbatement { get; set; }
        public decimal? HealthInsuranceAbatement { get; set; }
        public decimal? AccidentInsuranceAbatement { get; set; }
        public decimal? UnionDuesAbatement { get; set; }
        public decimal? TotalAbatement { get; set; }
        public decimal? TotalInsurance { get; set; }
        public decimal? TotalUnionDues { get; set; }
        public string NotesInsurance { get; set; }
        public int? Dependent { get; set; }
        public decimal? FamilyCircumstanceDeductions { get; set; }
        public decimal? TaxableIncome { get; set; }
        public string MonthText { get; set; }
        public string YearText { get; set; }
        public int? Count { get; set; }
        public bool IsLocked { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
