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
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
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
        public string index { get; set; }
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
        public string created { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
        public List<Choice> choices { get; set; }
    }

    public class Usage
    {
        public string prompt_tokens { get; set; }
        public string completion_tokens { get; set; }
        public string total_tokens { get; set; }
    }

    public class TranGroupVM
    {
        public string Month { get; set; }
        public string Year { get; set; }
        public string RouteId { get; set; }
        public string BrandShipId { get; set; }
        public string ExportListId { get; set; }
        public string ShipId { get; set; }
        public string LineId { get; set; }
        public string SocId { get; set; }
        public string Trip { get; set; }
        public DateTimeOffset? StartShip { get; set; }
        public string ContainerTypeId { get; set; }
        public string PolicyId { get; set; }
        public string Count { get; set; }
        public decimal ShipUnitPrice { get; set; }
        public decimal ShipPrice { get; set; }
        public decimal ShipPolicyPrice { get; set; }
    }
}
