using Core.Components;
using Core.Components.Forms;
using TMS.API.Models;
using Core.MVVM;
using Core.Enums;
using Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using Core.Components.Extensions;
using System.Threading.Tasks;

namespace TMS.UI.Business.Settings
{
    public class QuotationEditorBL : PopupEditor
    {

        public GridView gridView;
        public Quotation QuotationEntity => Entity as Quotation;
        public QuotationEditorBL() : base(nameof(Quotation))
        {
            Name = "Quotation Editor";
        }

        public void CheckQuotation(QuotationExpense quotationExpense, MasterData masterData)
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();

            if (QuotationEntity.QuotationExpense.Any(x => x.Id != quotationExpense.Id && x.ExpenseTypeId == masterData.Id))
            {
                Toast.Warning("Loại phí này đã được chọn !!!");
                gridView.RemoveRow(quotationExpense);
            }
        }

        public async Task AddQuotation()
        {
            QuotationEntity.Id = 0;
            QuotationEntity.InverseParent = null;
            await Save(QuotationEntity);
            Dispose();
        }
    }
}