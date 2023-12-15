using Core.Extensions;

namespace Core.ViewModels
{
    public class PatchVM
    {
        public bool ByPassPerm { get; set; } = true;
        public string FeatureId { get; set; }
        public string ComId { get; set; }
        public string QueueName { get; set; }
        public string Table { get; set; }
        public string TenantCode { get; set; }
        public string ConnKey { get; set; } = Utils.ConnKey;
        public string CachedConnStr { get; internal set; }
        public List<PatchDetail> Changes { get; set; } = [];
    }

    public class PatchDetail
    {
        public string Field { get; set; }
        public string Label { get; set; }
        public string OldVal { get; set; }
        public string Value { get; set; }
        public bool JustHistory { get; set; }
    }
}
