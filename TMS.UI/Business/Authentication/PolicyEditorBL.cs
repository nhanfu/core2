using TMS.API.Models;
using Core.Components.Forms;

namespace TMS.UI.Business.Authentication
{
    public class PolicyEditorBL : PopupEditor
    {
        public PolicyEditorBL() : base(nameof(FeaturePolicy))
        {
            Name = "PolicyEditor";
            Title = "Policy detail";
        }
    }
}
