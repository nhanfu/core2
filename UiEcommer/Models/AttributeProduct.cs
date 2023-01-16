using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class AttributeProduct
    {
        public int Id { get; set; }
        public int? AttributeId { get; set; }
        public int? ProductId { get; set; }
        public string Value { get; set; }
        public int? Order { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
