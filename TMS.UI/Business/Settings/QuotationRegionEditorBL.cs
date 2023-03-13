using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationRegionEditorBL : PopupEditor
    {
        public QuotationRegionEditorBL() : base(nameof(Quotation))
        {
            Name = "Quotation Region Editor";
        }
    }
}