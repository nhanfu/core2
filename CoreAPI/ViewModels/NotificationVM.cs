using Core.Models;

namespace CoreAPI.ViewModels
{
    public class NotificationVM
    {
        public TaskNotification Entity { get; set; }
        public List<string> Rule { get; set; }
    }
}
