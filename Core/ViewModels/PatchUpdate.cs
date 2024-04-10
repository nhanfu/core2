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
        public string[] DeletedIds { get; set; }
        public string QueueName { get; set; }
        public string CacheName { get; set; }
        public string MetaConn { get; set; }
        public string DataConn { get; set; }
        public List<PatchDetail> Changes { get; set; }
        public string EntityId => Changes?.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
        public string OldId
        {
            set
            {
                if (Changes is null) Changes = new List<PatchDetail> { };
                var idChange = Changes.FirstOrDefault(x => x.Field == Utils.IdField);
                if (idChange != null) idChange.OldVal = value;
                else Changes.Add(new PatchDetail { OldVal = value, Field = Utils.IdField });
            }
        }
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
