using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class UserSeq
    {
        public string Id { get; set; }
        public string EntityId { get; set; }
        public bool IsMonthlyRecycle { get; set; }
        public string LastKey { get; set; }
        public bool Active { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}
