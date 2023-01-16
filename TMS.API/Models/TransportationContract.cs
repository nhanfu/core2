using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class TransportationContract
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string ContractName { get; set; }
        public string ContractNo { get; set; }
        public int? BossId { get; set; }
        public string CompanyName { get; set; }
        public int? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? SignDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string SystemNotes { get; set; }
        public string Notes { get; set; }
        public string Files { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
