using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class QuotationUpdateEditorBL : PopupEditor
    {
        public QuotationUpdate QuotationUpdateEntity => Entity as QuotationUpdate;
        public GridView gridView;
        public List<Quotation> Quotations;
        public QuotationUpdateEditorBL() : base(nameof(QuotationUpdate))
        {
            Name = "Quotation Update Editor";
        }

        public async Task LoadQuotation(QuotationUpdate quotationUpdate)
        {
            if(QuotationUpdateEntity.TypeId is null)
            {
                return;
            }
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            Quotations = await new Client(nameof(Quotation)).GetRawList<Quotation>($"/GetCurrentQuotation?$filter=Active eq true and Location/RegionId eq {quotationUpdate.RegionId} and TypeId eq {QuotationUpdateEntity.TypeId}");
            var quos = Quotations.Select(quotation =>
            {
                var quo = new Quotation();
                quo.CopyPropFrom(quotation);
                quo.Id = 0;
                quo.StartDate = DateTime.Now;
                return quo;
            }).ToList();
            QuotationUpdateEntity.Quotation= quos;
        }

        public void ChangeUnitPrice()
        {
            gridView = gridView ?? this.FindActiveComponent<GridView>().FirstOrDefault();
            var quos = Quotations.Select(quotation =>
            {
                var quo = new Quotation();
                quo.CopyPropFrom(quotation);
                quo.Id = 0;
                quo.StartDate = DateTime.Now;
                quo.UnitPrice = QuotationUpdateEntity.IsAdd ? quo.UnitPrice + QuotationUpdateEntity.UnitPrice : quo.UnitPrice - QuotationUpdateEntity.UnitPrice;
                return quo;
            }).ToList();
            QuotationUpdateEntity.Quotation = quos;
        }

        public override async Task<bool> Save(object entity)
        {
            var rs = await base.Save(entity);
            if (rs)
            {
                Quotations.ForEach(x => x.Active = false);
                await new Client(nameof(Quotation)).BulkUpdateAsync(Quotations);
            }
            return rs;
        }
    }
}