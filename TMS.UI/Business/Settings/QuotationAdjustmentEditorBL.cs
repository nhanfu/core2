using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.MVVM;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class QuotationAdjustmentEditorBL : PopupEditor
    {
        public QuotationAdjustmentEditorBL() : base(nameof(Quotation))
        {
            Name = "QuotationAdjustment Editor";
        }
    }
}