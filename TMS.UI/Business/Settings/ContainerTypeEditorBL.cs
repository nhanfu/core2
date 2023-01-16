using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Settings
{
    public class ContainerTypeEditorBL : PopupEditor
    {
        public ContainerTypeEditorBL() : base(nameof(MasterData))
        {
            Name = "ContainerType Editor";
        }
    }
}