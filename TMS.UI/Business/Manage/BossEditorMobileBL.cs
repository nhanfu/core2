using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class BossEditorMobileBL : TabEditor
    {
        public BossEditorMobileBL() : base(nameof(Vendor))
        {
            Name = "Vendor Editor Mobile";
            DOMContentLoaded += () =>
            {
                Entity.SetPropValue(nameof(Vendor.TypeId), 7551);
            };
        }

        public override async Task<bool> Save(object entity = null)
        {
            if (!(await IsFormValid()))
            {
                return false;
            }
            var rs = await base.Save(entity);
            Dispose();
            return rs;
        }
    }
}