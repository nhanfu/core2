using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class Attribute
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public int? ParentId { get; set; }
        public int? AttributeId { get; set; }
        public string Code { get; set; }
        public string Unit { get; set; }
        public int? Order { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
