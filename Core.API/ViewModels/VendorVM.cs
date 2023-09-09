using Core.Models;
using System.Collections.Generic;

namespace Core.ViewModels
{
    public class VendorConnStrVM
    {
        public string Name { get; set; }
        public string ConStr { get; set; }
    }

    public class SyncConfigVM
    {
        public Component Component { get; set; }
        public ComponentGroup ComponentGroup { get; set; }
        public Models.Feature Feature { get; set; }
        public GridPolicy GridPolicy { get; set; }
        public bool SyncChildren { get; set; }
        public List<int> VendorId { get; set; }
    }
}
