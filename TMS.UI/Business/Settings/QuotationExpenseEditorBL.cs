using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationExpenseEditorBL : PopupEditor
    {
        public QuotationExpense QuotationExpenseEntity => Entity as QuotationExpense;
        public QuotationExpenseEditorBL() : base(nameof(QuotationExpense))
        {
            Name = "QuotationExpense Editor";
        }
    }
}