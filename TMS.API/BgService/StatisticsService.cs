using Core.Enums;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Net.WebSockets;
using System.Text;
using TMS.API.Models;
using TMS.API.Websocket;

namespace TMS.API.BgService
{
    public class StatisticsService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        protected readonly EntityService _entitySvc;
        protected readonly IConfiguration _configuration;
        private readonly string FCM_API_KEY;
        private readonly string FCM_SENDER_ID;
        protected Websocket.ConnectionManager WebSocketConnectionManager { get; set; }

        public StatisticsService(IServiceProvider serviceProvider, EntityService entityService, IConfiguration configuration, Websocket.ConnectionManager webSocketConnectionManage)
        {
            WebSocketConnectionManager = webSocketConnectionManage;
            _serviceProvider = serviceProvider;
            _entitySvc = entityService;
            FCM_API_KEY = configuration["FCM_API_KEY"];
            FCM_SENDER_ID = configuration["FCM_SENDER_ID"];
            _configuration = configuration;
        }

        public async Task NotifyAsync(IEnumerable<TaskNotification> entities)
        {
            await entities
                .Where(x => x.AssignedId.HasValue)
                .Select(x => new WebSocketResponse<TaskNotification>
                {
                    EntityId = _entitySvc.GetEntity(nameof(TaskNotification))?.Id ?? 0,
                    Data = x
                })
                .ForEachAsync(SendMessageToUser);
        }

        private async Task SendMessageToUser(WebSocketResponse<TaskNotification> task)
        {
            var fcm = new FCMWrapper
            {
                To = $"/topics/DongAU{task.Data.AssignedId:0000000}",
                Data = new FCMData
                {
                    Title = task.Data.Title,
                    Body = task.Data.Description,
                },
                Notification = new FCMNotification
                {
                    Title = task.Data.Title,
                    Body = task.Data.Description,
                    ClickAction = "com.softek.tms.push.background.MESSAGING_EVENT"
                }
            };
            await SendMessageToUsersAsync(new List<int>() { task.Data.AssignedId.Value }, task.ToJson(), fcm.ToJson());
        }

        public Task SendMessageToUsersAsync(List<int> userIds, string message, string fcm = null)
        {
            var userGroup = WebSocketConnectionManager.GetAll()
                .Where(x => userIds.Contains(x.Key.Split("/").FirstOrDefault().TryParseInt() ?? 0));
            return NotifyUserGroup(message, userGroup, fcm);
        }

        public async Task SendFCMNotfication(string message)
        {
            if (message is null)
            {
                return;
            }

            var client = new HttpClient();
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, new Uri("https://fcm.googleapis.com/fcm/send"));
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + FCM_API_KEY);
            request.Headers.TryAddWithoutValidation("Sender", "id=" + FCM_SENDER_ID);
            request.Content = new StringContent(message, Encoding.UTF8, "application/json");
            var res = await client.SendAsync(request);
            await res.Content.ReadAsStringAsync();
        }

        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(buffer: new ArraySegment<byte>(
                    array: bytes,
                    offset: 0,
                    count: bytes.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
        }

        private async Task NotifyUserGroup(string message, IEnumerable<KeyValuePair<string, WebSocket>> userGroup, string fcm = null)
        {
            var fcmTask = SendFCMNotfication(fcm);
            var realtimeTasks = userGroup.Select(pair =>
            {
                if (pair.Value.State != WebSocketState.Open)
                {
                    return Task.CompletedTask;
                }

                return SendMessageAsync(pair.Value, message);
            }).ToList();
            realtimeTasks.Add(fcmTask);
            await Task.WhenAll(realtimeTasks);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var waitTime = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0) - now;
                if (waitTime < TimeSpan.Zero)
                {
                    waitTime = waitTime.Add(TimeSpan.FromDays(1));
                }
                await Task.Delay(waitTime, stoppingToken);
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TMSContext>();
                    var rs1 = await CheckVendorLv1(dbContext);
                    var rs = await CheckVendorLv2(dbContext);
                    dbContext.AddRange(rs);
                    dbContext.AddRange(rs1);
                    await dbContext.SaveChangesAsync();
                    await NotifyAsync(rs);
                    await NotifyAsync(rs1);
                }
            }
        }

        private async Task<IEnumerable<TaskNotification>> CheckVendorLv1(TMSContext dbContext)
        {
            var currentDate = DateTime.UtcNow.Date.AddMonths(-3);
            var vendors = await dbContext.Vendor.Where(o => !o.IsSeft && o.StateId == 1 && o.TypeId == 7551 && ((o.LastOrderState < currentDate && o.LastOrderState != null) || (o.InsertedDate < currentDate && o.LastOrderState == null))).ToListAsync();
            if (vendors.Count == 0)
            {
                return new List<TaskNotification>();
            }
            vendors.ForEach(x => x.StateId = 2);
            var listUser = await dbContext.UserRole.Where(x => x.RoleId == 43).Select(x => x.UserId).Distinct().ToListAsync();
            var tasks = listUser.Select(user => new TaskNotification
            {
                Title = $"Chuyển chủ hàng về Đông Á vì lý do 3 tháng chưa phát sinh đơn hàng",
                Description = $"{vendors.Select(x => x.Name).Combine(", ")}",
                EntityId = _entitySvc.GetEntity(typeof(Vendor).Name).Id,
                RecordId = null,
                Attachment = "fal fa-users-slash",
                AssignedId = user,
                StatusId = (int)TaskStateEnum.UnreadStatus,
                RemindBefore = 540,
                Deadline = DateTime.Now,
                InsertedBy = 1,
                InsertedDate = DateTime.Now
            });
            return tasks;
        }

        private async Task<IEnumerable<TaskNotification>> CheckVendorLv2(TMSContext dbContext)
        {
            var currentDate = DateTime.UtcNow.Date.AddMonths(-6);
            var vendors = await dbContext.Vendor.Where(o => !o.IsSeft && o.StateId == 2 && o.TypeId == 7551 && ((o.LastOrderState < currentDate && o.LastOrderState != null) || (o.InsertedDate < currentDate && o.LastOrderState == null))).ToListAsync();
            if (vendors.Count == 0)
            {
                return new List<TaskNotification>();
            }
            vendors.ForEach(x => x.StateId = 3);
            var listUser = await dbContext.UserRole.Where(x => x.RoleId == 43).Select(x => x.UserId).Distinct().ToListAsync();
            var tasks = listUser.Select(user => new TaskNotification
            {
                Title = $"Chuyển chủ hàng về chung vì lý do 6 tháng chưa phát sinh đơn hàng",
                Description = $"{vendors.Select(x => x.Name).Combine(", ")}",
                EntityId = _entitySvc.GetEntity(typeof(Vendor).Name).Id,
                RecordId = null,
                Attachment = "fal fa-users-slash",
                AssignedId = user,
                StatusId = (int)TaskStateEnum.UnreadStatus,
                RemindBefore = 540,
                Deadline = DateTime.Now,
                InsertedBy = 1,
                InsertedDate = DateTime.Now
            });
            return tasks;
        }

        public void KillBlockedProcesses()
        {
            var connectionString = _configuration["Default"];
            var serverConnection = new ServerConnection();
            serverConnection.ConnectionString = connectionString;
            var server = new Server(serverConnection);
            server.ConnectionContext.Connect();
            var query = "SELECT blocking_session_id FROM sys.dm_exec_requests WHERE blocking_session_id <> 0";
            var results = server.ConnectionContext.ExecuteWithResults(query);
            for (int i = 0; i < results.Tables[0].Rows.Count; i++)
            {
                var blockingSessionId = int.Parse(results.Tables[0].Rows[i]["blocking_session_id"].ToString());
                server.KillProcess(blockingSessionId);
            }
        }
    }
}
