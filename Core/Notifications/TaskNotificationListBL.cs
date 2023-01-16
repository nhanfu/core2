using System.Threading.Tasks;
using Core.Models;
using Core.Components.Forms;

namespace Core.Notifications
{
    public class TaskNotificationListBL : TabEditor
    {
        public TaskNotificationListBL() : base(nameof(TaskNotification))
        {
            Name = "TaskNotification List";
            Entity = new TaskNotification();
        }

        public async Task EditNotification(TaskNotification taskNotification)
        {
            await NotificationBL.Instance.OpenNotification(taskNotification);
        }
    }
}
