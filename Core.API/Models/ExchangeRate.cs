using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class ExchangeRate
    {
        public string Id { get; set; }
        public string FromCurrencyId { get; set; }
        public string ToCurrencyId { get; set; }
        public decimal ExchangeRate1 { get; set; }
        public string Imageurl { get; set; }
        public decimal BuyCash { get; set; }
        public decimal SellCash { get; set; }
        public decimal BuyBankTransfer { get; set; }
        public decimal SellBankTransfer { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
}
