using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class ExchangeRate
    {
        public int Id { get; set; }
        public int? FromCurrencyId { get; set; }
        public int? ToCurrencyId { get; set; }
        public decimal ExchangeRate1 { get; set; }
        public string Imageurl { get; set; }
        public decimal BuyCash { get; set; }
        public decimal SellCash { get; set; }
        public decimal BuyBankTransfer { get; set; }
        public decimal SellBankTransfer { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
