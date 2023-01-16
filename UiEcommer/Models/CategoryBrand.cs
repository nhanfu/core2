using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class CategoryBrand
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
