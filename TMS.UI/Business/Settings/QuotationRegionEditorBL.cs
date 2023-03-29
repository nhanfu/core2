using Core.Components.Forms;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationRegionEditorBL : PopupEditor
    {
        public Quotation QEntity => Entity as Quotation;
        public QuotationRegionEditorBL() : base(nameof(Quotation))
        {
            Name = "Quotation Region Editor";
        }

        public void BeforeCreatedQuotationRegion(Quotation quotation)
        {
            quotation.PackingId = QEntity.PackingId;
            quotation.ContainerTypeId = QEntity.ContainerTypeId;
            quotation.RegionId = QEntity.RegionId;
            quotation.StartDate = QEntity.StartDate;
            quotation.TypeId = QEntity.TypeId;
        }

        public async Task AddQuotation()
        {
            QEntity.Id = 0;
            await Save(QEntity);
            Dispose();
        }
    }
}