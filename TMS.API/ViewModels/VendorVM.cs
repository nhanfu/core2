using System.Collections.Generic;
using TMS.API.Models;

namespace TMS.API.ViewModels
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
        public Feature Feature { get; set; }
        public GridPolicy GridPolicy { get; set; }
        public bool SyncChildren { get; set; }
        public List<int> VendorId { get; set; }
    }
}