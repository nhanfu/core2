using Core.Models;
using Core.Components.Forms;

namespace Core.Fw.Authentication
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
