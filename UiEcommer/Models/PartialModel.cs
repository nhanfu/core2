using System.ComponentModel.DataAnnotations.Schema;

namespace UiEcommer.Models
{
    public partial class PromoDetail
    {
        [NotMapped]
        public int Percent => (int)(100 - ((UnitPricePromo / UnitPrice) * 100));
        [NotMapped]
        public int PercentQty => (int)(100 - ((RemainQuantity / Quantity) * 100));
    }

    public partial class Product
    {
        [NotMapped]
        public int Percent => (int)(100 - ((Promo / UnitPrice) * 100));
        [NotMapped]
        public int PercentQty => (int)(100 - ((Promo / UnitPrice) * 100));
    }
}
