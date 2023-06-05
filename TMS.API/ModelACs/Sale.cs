using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class Sale
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? GroupId { get; set; }
        public int? SaleId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
