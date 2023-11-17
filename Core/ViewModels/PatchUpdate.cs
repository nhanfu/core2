using System.Collections.Generic;

namespace Core.ViewModels
{
    public class PatchUpdate
    {
        public string Table { get; set; }
        public string ConnKey { get; set; }
        public string Checksum { get; set; }
        public List<PatchUpdateDetail> Changes { get; set; }
    }

    public class PatchUpdateDetail
    {
        public string Field { get; set; }
        public string OldVal { get; set; }
        public string Value { get; set; }
    }
}
