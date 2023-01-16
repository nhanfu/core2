using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Manage
{
    public class TeusEditorBL : PopupEditor
    {
        public Teus teus => Entity as Teus;
        public TeusEditorBL() : base(nameof(Teus))
        {
            Name = "Teus Editor";
        }
    }
}