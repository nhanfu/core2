using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class InsuranceFeesRateEditorBL : PopupEditor
    {
        public InsuranceFeesRateEditorBL() : base(nameof(InsuranceFeesRate))
        {
            Name = "InsuranceFeesRate Editor";
        }
    }
}