using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Dictionary
    {
        public int Id { get; set; }
        public string LangCode { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool Active { get; set; }
        public int InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
