using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class SpecialCustomerEditorBL : PopupEditor
    {
        public SpecialCustomerEditorBL() : base(nameof(Vendor))
        {
            Name = "SpecialCustomer Editor";
        }
    }
}