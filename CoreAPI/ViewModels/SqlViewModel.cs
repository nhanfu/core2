using Core.Extensions;
using Core.Models;
using System.Text.Json.Serialization;

namespace Core.ViewModels
{
    public class SqlViewModel
    {
        public bool IsStream { get; set; }
        public string SvcId { get; set; }
        public string ComId { get; set; }
        public string Action { get; set; }
        public string Params { get; set; }
        public string Table { get; set; }
        public string QueueName { get; set; }
        public string[] Ids { get; set; }
        public Component Component { get; set; }
        public string AnnonymousTenant { get; set; }
        public string AnnonymousEnv { get; set; }
        public string JsScript { get; internal set; }
        public string Select { get; set; } = "*";
        public string Where { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public string OrderBy { get; set; }
        public string Paging { get; set; }
        public bool Count { get; set; }
        public string[] FieldName { get; set; }
        public bool SkipXQuery { get; set; }
        public string ConnKey { get; set; } = Utils.ConnKey;
        [JsonIgnore]
        public string CachedConnStr { get; internal set; }
    }

    public class SqlQueryResult
    {
        public object Result { get; set; }
        public string Query { get; set; }
        public string XQuery { get; set; }
    }
}
