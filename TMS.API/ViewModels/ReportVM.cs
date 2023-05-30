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
        public bool Maintenance { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class ChatGptVM
    {
        public string model { get; set; }
        public List<ChatGptMessVM> messages { get; set; }
    }


    public class ChatGptMessVM
    {
        public string role { get; set; }
        public string content { get; set; }
        public string name { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
        public string finish_reason { get; set; }
        public int index { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class RsChatGpt
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
        public List<Choice> choices { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
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
