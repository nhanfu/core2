using System;
using System.Collections.Generic;

namespace Core.SMSModels
{
    public partial class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ConnDebug { get; set; }
        public string ConnProd { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientID { get; set; }
        public int? System { get; set; }
        public bool? Active { get; set; }
        public int? InsertedBy { get; set; }
        public DateTimeOffset? InsertedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string SystemName { get; set; }
        public string FtpUrl { get; set; }
        public string FtpUid { get; set; }
        public string FtpPwd { get; set; }
        public string ClientInbox { get; set; }
        public string ClientInboxDebug { get; set; }
        public string Outbox { get; set; }
        public string OutboxDebug { get; set; }
        public string TaxCode { get; set; }

        public virtual System SystemNavigation { get; set; }
    }
}
