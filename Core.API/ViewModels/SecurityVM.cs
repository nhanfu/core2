using Core.Models;
using System.Collections.Generic;

namespace Core.ViewModels
{
    public class SecurityVM : FeaturePolicy
    {
        public bool AllPermission { get; set; }
        public string[] RecordIds { get; set; }
        public string StrRecordIds => string.Join(",", RecordIds);
        public List<FeaturePolicy> FeaturePolicy { get; set; }
    }
}
