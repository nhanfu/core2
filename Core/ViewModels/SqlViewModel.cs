using Core.Clients;

namespace Core.ViewModels
{
    public class SqlViewModel
    {
        public string SvcId { get; set; }
        public string ComId { get; set; }
        public string Action { get; set; }
        public string Params { get; set; }
        public string[] Ids { get; set; }
        public string AnnonymousTenant { get; set; } = Client.Tenant;
        public string AnnonymousEnv { get; set; } = Client.Env;
        public string Paging { get; set; }
        public string Select { get; set; }
        public string Where { get; set; }
        public string OrderBy { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public bool Count { get; set; }
        public string[] FieldName { get; set; }
        public bool SkipXQuery { get; set; }
        public string ConnKey { get; set; } = Client.ConnKey;
        public string Table { get; set; }
    }
}
