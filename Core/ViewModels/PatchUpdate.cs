using Core.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Core.ViewModels
{
    public class PatchUpdate
    {
        public string FeatureId { get; set; }
        public string ComId { get; set; }
        public string Table { get; set; }
        public string ConnKey { get; set; }
        public List<PatchUpdateDetail> Changes { get; set; }
        public string EntityId => Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
    }

    public class PatchUpdateDetail
    {
        public string Field { get; set; }
        public string Label { get; set; }
        public string OldVal { get; set; }
        public string Value { get; set; }
        public bool JustHistory { get; set; }
    }
}
