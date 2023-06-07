using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class RequestLog
    {
        public int Id { get; set; }
        public string RequestBody { get; set; }
        public string HttpMethod { get; set; }
        public string Path { get; set; }
        public string ResponseBody { get; set; }
        public int? StatusCode { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
