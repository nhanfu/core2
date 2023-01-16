using Core.Components;
using Core.Components.Forms;
using TMS.API.Models;
using Core.Enums;
using Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using Core.Components.Extensions;

namespace TMS.UI.Business.Settings
{
    public class GetOrderEditorBL : PopupEditor
    {
        public Vendor VendorEntity => Entity as Vendor;
        
        public GetOrderEditorBL() : base(nameof(Vendor))
        {
            Name = "GetOrder Editor";
        }
    }
}