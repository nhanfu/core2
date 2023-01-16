using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderDetail = new List<OrderDetail>();
        }

        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalQuantity { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public List<OrderDetail> OrderDetail { get; set; }
    }
}
