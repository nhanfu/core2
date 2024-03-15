using Core.Extensions;
using System.Text.Json.Serialization;

namespace Core.ViewModels
{
    public class PatchVM
    {
        public bool ByPassPerm { get; set; } = true;
        public string FeatureId { get; set; }
        public string ComId { get; set; }
        public string QueueName { get; set; }
        public string CacheName { get; set; }
        public string Table { get; set; }
        public string[] DeletedIds { get; set; }
        public string TenantCode { get; set; }
        public string Env { get; set; }
        public string MetaConn { get; set; } = Utils.ConnKey;
        public string DataConn { get; set; } = Utils.ConnKey;
        [JsonIgnore]
        public string CachedDataConn { get; internal set; }
        public string CachedMetaConn { get; internal set; }
        public List<PatchDetail> Changes { get; set; } = [];
        public PatchDetail Id => Changes.FirstOrDefault(x => x.Field == Utils.IdField);
    }

    public class PatchDetail
    {
        public string Field { get; set; }
        public string Label { get; set; }
        public string OldVal { get; set; }
        public string Value { get; set; }
        public string HistoryValue { get; set; }
    }
}
