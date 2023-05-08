using Bridge.Html5;
using Core.Clients;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using Core.Enums;
using Core.Extensions;
using Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanChangeEditorMobileBL : TabEditor
    {
        public TransportationPlan transportationPlanEntity => Entity as TransportationPlan;
        public TransportationPlanChangeEditorMobileBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan Change Editor Mobile";
        }

        public async Task ActionRequest()
        {
            var res = await RequestApprove(transportationPlanEntity);
            if (res && Client.Token.RoleIds.Contains((int)RoleEnum.Driver_Truck))
            {
                await Approve();
            }
            else
            {
                ProcessEnumMessage(res);
            }
        }
    }
}