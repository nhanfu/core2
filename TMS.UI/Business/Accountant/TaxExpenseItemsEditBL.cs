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
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.UI.Business.Accountant
{
    public class TaxExpenseItemsEditBL : PopupEditor
    {
        public TaxExpenseItemsEditBL() : base(nameof(MasterData))
        {
            Name = "TaxExpenseItem Editor";
        }
    }
}
