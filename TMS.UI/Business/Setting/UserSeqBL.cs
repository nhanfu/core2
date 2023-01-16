using Core.Components.Forms;
using TMS.API.Models;

namespace TMS.UI.Business.Setting
{
    public class UserSeqBL : TabEditor
    {
        public UserSeqBL() : base(nameof(UserSeq))
        {
            Name = "User Seq";
            Title = Name;
        }
    }
}
