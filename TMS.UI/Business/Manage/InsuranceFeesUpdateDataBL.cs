using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using Retyped.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using static Retyped.dom.Literals.Types;
using Number = Core.Components.Number;

namespace TMS.UI.Business.Manage
{
    public class InsuranceFeesUpdateDataBL : PopupEditor
    {
        public Expense expenseEntity => Entity as Expense;
        public InsuranceFeesUpdateDataBL() : base(nameof(Expense))
        {
            Name = "InsuranceFees Update Data";
        }

        public async Task UpdateDataFromTransportation()
        {
            if (expenseEntity.FromDate is null)
            {
                Toast.Warning("Vui lòng nhập từ ngày");
                return;
            }
            if (expenseEntity.ToDate is null)
            {
                Toast.Warning("Vui lòng nhập đến ngày");
                return;
            }
            var res = await new Client(nameof(Expense)).PostAsync<bool>(Entity, "UpdateDataFromTransportation");
            if (res)
            {
                Toast.Success("Đã cập nhật thành công.");
            }
            else
            {
                Toast.Warning("Đã có lỗi xảy ra trong quá trình xử lý.");
            }
            Dirty = false;
            this.Dispose();
        }

        public override void Cancel()
        {
            this.Dispose();
            base.Cancel();
        }

        public override void CancelWithoutAsk()
        {
            this.Dispose();
            base.CancelWithoutAsk();
        }
    }
}