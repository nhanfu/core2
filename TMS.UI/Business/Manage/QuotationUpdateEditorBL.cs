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
        public QuotationUpdateEditorBL() : base(nameof(QuotationUpdate))
        {
            Name = "Quotation Update Editor";
        }

        public async Task SaveChanges()
        {
            if (!await IsFormValid())
            {
                return;
            }
            var confirmDialog = new ConfirmDialog
            {
                Content = "Bạn có chắc chắn muốn chiều chỉnh giá?"
            };
            confirmDialog.YesConfirmed += async () =>
            {
                var rs = await new Client(nameof(QuotationUpdate)).PostAsync<bool>(QuotationUpdateEntity, $"QuotationUpdate");
                if (rs)
                {
                    Toast.Success("Cập nhật giá thành công");
                }
            };
            AddChild(confirmDialog);
        }
    }
}