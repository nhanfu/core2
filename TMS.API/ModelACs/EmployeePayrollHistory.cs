using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class EmployeePayrollHistory
    {
        public int Id { get; set; }
        public string MonthText { get; set; }
        public string YearText { get; set; }
        public string RecordIds { get; set; }
        public int? StatusId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
