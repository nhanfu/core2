using Core.Extensions;
using Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Core.ViewModels
{
    public class SecurityVM : FeaturePolicy
    {
        public bool AllPermission { get; set; }
        public string[] RecordIds { get; set; }
        public string StrRecordIds => RecordIds.Select(x => $"\"{x}\"").Combine();
        public List<FeaturePolicy> FeaturePolicy { get; set; }
    }
}
