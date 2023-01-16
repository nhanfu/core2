using Core.Components.Extensions;
using Core.Components.Forms;
using System.Threading.Tasks;
using Core.Models;

namespace TMS.UI
{
    public class PortalBL : EditForm
    {
        public PortalBL() : base(nameof(User))
        {
            Name = "Home";
            Entity = new User();
        }

        public override void Render()
        {
            Task.Run(RenderLandingPage);
        }

        private async Task RenderLandingPage()
        {
            if (Feature is null)
            {
                Feature = await ComponentExt.LoadFeatureByName(Name, true);
            }
            await RenderAsync();
        }
    }
}
