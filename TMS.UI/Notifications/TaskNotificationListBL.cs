using System.Threading.Tasks;
using TMS.API.Models;
using Core.Components.Forms;

namespace TMS.UI.Notifications
{
    public class TaskNotificationListBL : TabEditor
    {
        public TaskNotificationListBL() : base(nameof(TaskNotification))
        {
            Name = "TaskNotification List";
            Entity = new TaskNotification();
        }
    }
}
