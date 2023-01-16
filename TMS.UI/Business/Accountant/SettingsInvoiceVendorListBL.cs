using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class SettingsInvoiceVendorListBL : TabEditor
    {
        public SettingsInvoiceVendorListBL() : base(nameof(Ledger))
        {
            Name = "SettingsInvoiceVendor List";
        }

        public void BeforeCreateLedger(Ledger ledger)
        {
            ledger.TypeId = 2;
        }
    }
}
