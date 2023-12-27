namespace Core.ViewModels;
public class MQEvent
{
    public string DeviceKey { get; set; }
    public string QueueName { get; set; }
    public string Action { get; set; }
    public string Id { get; set; }
    public string PrevId { get; set; }
    public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;
    public dynamic Message { get; set; }
}
