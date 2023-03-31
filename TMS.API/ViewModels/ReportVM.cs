using System;
using System.Collections.Generic;

namespace TMS.API.ViewModels
{
    public class ReportGroupVM
    {
        public bool Boss { get; set; }
        public bool ContainerType { get; set; }
        public bool Combination { get; set; }
        public bool Commodity { get; set; }
        public bool Closing { get; set; }
        public bool Route { get; set; }
        public bool Ship { get; set; }
        public bool ExportList { get; set; }
        public bool StartShip { get; set; }
        public bool BrandShip { get; set; }
        public bool User { get; set; }
        public bool Return { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class UserChatVM
    {
        public string FullName { get; set; }
        public string Token { get; set; }
        public string FromName { get; set; }
        public string ToName { get; set; }
        public int FromId { get; set; }
        public int ToId { get; set; }
        public List<Chat> Chats { get; set; }
    }

    public class Chat
    {
        public string Context { get; set; }
        public bool IsSeft { get; set; }
        public DateTime InsertedDate { get; set; }
        public bool IsSeen { get; set; }
    }

    public class TranGroupVM
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? RouteId { get; set; }
        public int? BrandShipId { get; set; }
        public int? ExportListId { get; set; }
        public int? ShipId { get; set; }
        public int? LineId { get; set; }
        public int? SocId { get; set; }
        public string Trip { get; set; }
        public DateTime? StartShip { get; set; }
        public int? ContainerTypeId { get; set; }
        public int? PolicyId { get; set; }
        public int? Count { get; set; }
        public decimal ShipUnitPrice { get; set; }
        public decimal ShipPrice { get; set; }
        public decimal ShipPolicyPrice { get; set; }
    }
}
