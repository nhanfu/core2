using System;
using System.Collections.Generic;

namespace UiEcommer.Models
{
    public partial class Promo
    {
        public Promo()
        {
            PromoDetail = new HashSet<PromoDetail>();
        }

        public int Id { get; set; }
        public int? TypeId { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Icon { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool IsLoop { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual ICollection<PromoDetail> PromoDetail { get; set; }
    }
}
