using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class UserSeq
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public bool IsMonthlyRecycle { get; set; }
        public int LastKey { get; set; }
        public bool Active { get; set; }
        public int InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
