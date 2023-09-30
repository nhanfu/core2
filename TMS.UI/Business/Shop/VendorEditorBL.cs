using Core.Components.Extensions;
using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Shop
{
    public class VendorEditorBL : PopupEditor
    {
        public Vendor vendorEntity => Entity as Vendor;
        public VendorEditorBL() : base(nameof(Vendor))
        {
            Name = "Vendor Editor";
            DOMContentLoaded += () =>
            {
                
            };
        }
    }
}
