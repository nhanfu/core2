using Core.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Core.ViewModels
{
    public class PatchVM
    {
        public string FeatureId { get; set; }
        public string ComId { get; set; }
        public string Table { get; set; }
        public bool Delete { get; set; }
        public string QueueName { get; set; }
        public string CacheName { get; set; }
        public string ConnKey { get; set; }
        public List<PatchDetail> Changes { get; set; }
        public string EntityId => Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
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
