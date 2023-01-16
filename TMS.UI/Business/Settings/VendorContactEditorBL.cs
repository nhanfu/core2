using Core.Components.Extensions;
using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class VendorContactEditorBL : PopupEditor
    {
        public VendorContact vendorContactEntity => Entity as VendorContact;
        public VendorContactEditorBL() : base(nameof(VendorContact))
        {
            Name = "VendorContact Editor";
        }
    }
}