using Bridge.Html5;
using Core.Components;
using Core.Components.Extensions;
using Core.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class ReturnTransportationPlanEditorBL : PopupEditor
    {
        public Transportation TransportationEntity => Entity as Transportation;
        public GridView gridView;
        public ReturnTransportationPlanEditorBL() : base(nameof(Transportation))
        {
            Name = "Return TransportationPlan Editor";
        }

        public void Search()
        {

        }
    }
}