using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class CommodityValueOfCompanyEditorBL : PopupEditor
    {
        public CommodityValueOfCompanyEditorBL() : base(nameof(SettingPolicy))
        {
            Name = "CommodityValueOfCompany Editor";
        }
    }
}
