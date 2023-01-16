using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;
using TMS.UI.Business.Manage;

namespace TMS.UI.Business.Accountant
{
    public class PaymentAdvanceVoucherEditBL : PopupEditor
    {
        public GridView gridView;
        public Ledger ledgerEntity => Entity as Ledger;
        public List<Ledger> ledgerParentList = new List<Ledger>();
        public PaymentAdvanceVoucherEditBL() : base(nameof(Ledger))
        {
            Name = "PaymentAdvanceVoucher Editor";
        }
    }
}
