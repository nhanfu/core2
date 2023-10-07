using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class Conversation
    {
        public string Id { get; set; }

        public string FromName { get; set; }

        public string ToName { get; set; }

        public string FromId { get; set; }

        public string ToId { get; set; }

        public string LastContext { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }
    }
}