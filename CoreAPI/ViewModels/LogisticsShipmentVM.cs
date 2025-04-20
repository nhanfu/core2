using CoreAPI.UIModels;

namespace CoreAPI.ViewModels
{
    public class TruckingShipmentVM
    {
        public Dictionary<string,object> Shipment { get; set; }
        public Dictionary<string, object> ShipmentDetail { get; set; }
        public List<Dictionary<string, object>> EntityContainer { get; set; }
        public List<Dictionary<string, object>> ShipmentFee { get; set; }
        public List<Dictionary<string, object>> ShipmentFee2 { get; set; }
        public List<Dictionary<string, object>> ShipmentFee3 { get; set; }
    }
}