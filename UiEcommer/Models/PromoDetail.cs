using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class PromoDetail
    {
        public int Id { get; set; }
        public int? PromoId { get; set; }
        public int? ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitPricePromo { get; set; }
        public decimal Quantity { get; set; }
        public decimal UsingQuantity { get; set; }
        public decimal RemainQuantity { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Product Product { get; set; }
        public virtual Promo Promo { get; set; }
    }
}
