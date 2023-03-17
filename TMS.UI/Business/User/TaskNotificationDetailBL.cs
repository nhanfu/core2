using Core.Clients;
using Core.Components.Forms;
using Core.Extensions;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.UI.Business.User
{
    public class TaskNotificationDetailBL : PopupEditor
    {
        public TaskNotificationDetailBL() : base(nameof(TaskNotification))
        {
            Name = "Create TaskNotification";
        }

        public override async Task<bool> Save(object entity = null)
        {
            if(! await IsFormValid())
            {
                return false;
            }
            var rs = await new Client(nameof(TaskNotification)).PostAsync<bool>(Entity, "SendRequest");
            if (rs)
            {
                Toast.Success("Thêm mới thành công");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}