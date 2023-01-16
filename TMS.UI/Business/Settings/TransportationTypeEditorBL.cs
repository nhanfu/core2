using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Threading.Tasks;
using TMS.API.Enums;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class TransportationTypeEditorBL : PopupEditor
    {
        public TransportationTypeEditorBL() : base(nameof(SettingPolicy))
        {
            Name = "TransportationType Editor";
        }
    }
}
