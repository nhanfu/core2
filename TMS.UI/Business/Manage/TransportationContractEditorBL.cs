using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TransportationContractEditorBL : PopupEditor
    {

        public TransportationContractEditorBL() : base(nameof(TransportationContract))
        {
            Name = "TransportationContract Editor";
        }
    }
}