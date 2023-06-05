using System;
using System.Collections.Generic;

namespace TMS.API.ModelACs
{
    public partial class ImageEntity
    {
        public int Id { get; set; }
        public int? TypeId { get; set; }
        public int? Order { get; set; }
        public int? EntityId { get; set; }
        public int? RecordId { get; set; }
        public string Url { get; set; }
        public string Link { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
