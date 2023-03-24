using Core.Components.Extensions;
using Core.Components.Forms;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TransportationPlanEditorMobileBL : TabEditor
    {
        public TransportationPlan transportationPlanEntity => Entity as TransportationPlan;
        public TransportationPlanEditorMobileBL() : base(nameof(TransportationPlan))
        {
            Name = "TransportationPlan Editor Mobile";
            DOMContentLoaded += () =>
            {
                if (transportationPlanEntity.IsTransportation)
                {
                    LockUpdateButCancel();
                }
                if (transportationPlanEntity.Id < 0)
                {
                    this.SetShow(false, "btnDelete");
                }
            };
        }

        public override async Task<bool> Save(object entity = null)
        {
            if (!(await IsFormValid()))
            {
                return false;
            }
            transportationPlanEntity.TotalContainerRemain = transportationPlanEntity.TotalContainerRemain ?? transportationPlanEntity.TotalContainer;
            transportationPlanEntity.TotalContainerUsing = transportationPlanEntity.TotalContainerUsing ?? 0;
            var rs = await base.Save(entity);
            Dispose();
            return rs;
        }
    }
}