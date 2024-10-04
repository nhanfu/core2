using Core.Extensions;
using Core.Models;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Text.Json.Serialization;

namespace Core.ViewModels
{
    public class SqlViewModel
    {
        public string SvcId { get; set; }
        public string ComId { get; set; }
        public string Action { get; set; }
        public string Params { get; set; }
        public string Table { get; set; }
        public string QueueName { get; set; }
        public string[] Id { get; set; }
        public Component Component { get; set; }
        public string AnnonymousTenant { get; set; }
        public string AnnonymousEnv { get; set; }
        public string JsScript { get; internal set; }
        public string Select { get; set; } = "*";
        public string Where { get; set; }
        public string WhereParams { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public string OrderBy { get; set; }
        public string Paging { get; set; }
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public bool Count { get; set; }
        public bool WrapQuery { get; set; } = true;
        public string[] FieldName { get; set; }
        public bool SkipXQuery { get; set; }
        public string DataConn { get; set; } = Utils.ConnKey;
        public string MetaConn { get; set; } = Utils.ConnKey;
        [JsonIgnore]
        public string CachedDataConn { get; internal set; }
        [JsonIgnore]
        public string CachedMetaConn { get; internal set; }
    }

    public class SqlQueryResult
    {
        public object Result { get; set; }
        public string Query { get; set; }
        public string DataConn { get; set; }
        public string XQuery { get; set; }
        public string MetaConn { get; set; }
        internal string DataQuery { get; set; }
        internal string MetaQuery { get; set; }
        internal bool SameContext { get; set; }
    }

    public class SqlResult
    {
        public string message { get; set; }
        public int status { get; set; }
        public Dictionary<string, object>[] updatedItem { get; set; }
        public List<DetailData> Detail { get; set; }
        public Dictionary<string, object>[] data { get; set; }
    }

    public class SqlComResult
    {
        public int? count { get; set; }
        public Dictionary<string, object>[] value { get; set; }
    }

    public class DetailData
    {
        public int Index { get; set; }
        public string Table { get; set; }
        public List<string> Ids { get; set; }
        public string ComId { get; set; }
        public Dictionary<string, object>[] Data { get; set; }
    }

    public class SqlQuery
    {
        public string sql { get; set; }
        public string total { get; set; }
        public string delete { get; set; }
        public string update { get; set; }
    }

    public class WhereParamVM
    {
        public string FieldName { get; set; }
        public string Value { get; set; }
    }

    public class ServiceVM
    {
        public string Name { get; set; }
    }
}
