using System;
using System.Collections.Generic;

namespace Core.Models
{
    public partial class Webhook
    {
        public int Id { get; set; }
        public int? EntityId { get; set; }
        public string SubName { get; set; }
        public string SubUrl { get; set; }
        public string SubUsername { get; set; }
        public string SubPassword { get; set; }
        public int AuthVersionId { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int EventTypeId { get; set; }
        public string Method { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeyHeader { get; set; }
        public string LoginUrl { get; set; }
        public string UsernameKey { get; set; }
        public string PasswordKey { get; set; }
        public string AccessTokenField { get; set; }
        public string TokenPrefix { get; set; }
        public string SavedToken { get; set; }
    }
}
