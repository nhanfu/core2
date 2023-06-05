using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class BankAccount
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string BankNo { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public int? AccountantId { get; set; }
        public int? ObjectId { get; set; }
        public string SwiftCode { get; set; }
        public int? AccountNo { get; set; }
        public string Notes { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
