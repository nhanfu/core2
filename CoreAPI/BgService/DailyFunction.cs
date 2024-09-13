using Core.Extensions;
using Core.Models;
using Core.Services;
using CoreAPI.Models;
using System.Data;

namespace CoreAPI.BgService
{
    public class DailyFunction
    {
        IConfiguration _config;
        WebSocketService _socket;
        public string Env { get; set; }
        public DailyFunction(IConfiguration configuration, WebSocketService webSocketService)
        {
            _config = configuration;
            _socket = webSocketService;
        }

        public async Task StatisticsProcesses()
        {
            var connect = _config.GetConnectionString("Default");
            await DoWorkNextTimeAsync(connect);
            await DoWorkDailyAsync(connect);
            await DoWorkWeeklyAsync(connect);
            await DoWorkMonthlyAsync(connect);
            await DoWorkYearlyAsync(connect);
        }

        private async Task DoWorkNextTimeAsync(string connect)
        {
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>(
                $@"select [PartnerCare].*,Partner.Name as CustomerName 
                from [PartnerCare] 
                left join Partner on PartnerCare.PartnerId = Partner.Id 
                where DATEADD(DAY, -isnull(NotificationNumber,0), NextDate) = '{DateTime.Now:yyyy-MM-dd}' and ReminderSettingId is null", connect);
            var tasks = partnerCare.Select(item =>
            {
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "PartnerCare",
                    Title = $"{item.CustomerName} activities",
                    Icon = "fal fa-phone-alt",
                    Description = item.TaskName,
                    InsertedBy = "-1",
                    RecordId = item.Id,
                    InsertedDate = DateTime.Now,
                    Active = true,
                    AssignedId = item.AssigneeId ?? item.InsertedBy
                };
            });
            foreach (var item in tasks)
            {
                var patch = item.MapToPatch();
                await BgExt.SavePatch2(patch, connect);
            }
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket);
        }

        private async Task DoWorkDailyAsync(string connect)
        {
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"select [PartnerCare].*,Partner.Name as CustomerName 
from [PartnerCare] 
left join Partner on PartnerCare.PartnerId = Partner.Id 
where NextDate >= '{DateTime.Now:yyyy-MM-dd}' and ReminderSettingId = 1", connect);
            if (partnerCare.Nothing())
            {
                return;
            }
            var tasks = partnerCare.Select(item =>
            {
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "PartnerCare",
                    Title = $"{item.CustomerName} daily notification",
                    Description = item.TaskName,
                    InsertedBy = "-1",
                    RecordId = item.Id,
                    Icon = "fal fa-phone-alt",
                    InsertedDate = DateTime.Now,
                    Active = true,
                    AssignedId = item.AssigneeId ?? item.InsertedBy
                };
            });
            foreach (var item in tasks)
            {
                var patch = item.MapToPatch();
                await BgExt.SavePatch2(patch, connect);
                var item1 = partnerCare.FirstOrDefault(x => x.Id == item.RecordId);
                item1.LastNotificationDate = DateTime.Now.Date;
                var patch1 = item1.MapToPatch();
                await BgExt.SavePatch2(patch1, connect);
            }
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket);
        }

        private async Task DoWorkWeeklyAsync(string connect)
        {
            var startOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7).AddSeconds(-1);
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"
        SELECT [PartnerCare].*, Partner.Name as CustomerName 
        FROM [PartnerCare]
        LEFT JOIN Partner ON PartnerCare.PartnerId = Partner.Id
        WHERE DATEADD(DAY, -isnull(NotificationNumber,0), isnull(LastNotificationDate,NextDate)) >= '{startOfWeek:yyyy-MM-dd}' 
        AND DATEADD(DAY, -isnull(NotificationNumber,0), isnull(LastNotificationDate,NextDate)) <= '{endOfWeek:yyyy-MM-dd}'
        AND ReminderSettingId = 2", connect);
            if (partnerCare.Nothing())
            {
                return;
            }

            var tasks = partnerCare.Select(item =>
            {
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "PartnerCare",
                    Title = $"{item.CustomerName} weekly notification",
                    Description = item.TaskName,
                    InsertedBy = "-1",
                    RecordId = item.Id,
                    Icon = "fal fa-phone-alt",
                    InsertedDate = DateTime.Now,
                    Active = true,
                    AssignedId = item.AssigneeId ?? item.InsertedBy
                };
            }).ToList();
            foreach (var item in tasks)
            {
                var patch = item.MapToPatch();
                await BgExt.SavePatch2(patch, connect);
                var item1 = partnerCare.FirstOrDefault(x => x.Id == item.RecordId);
                item1.LastNotificationDate = DateTime.Now.Date;
                var patch1 = item1.MapToPatch();
                await BgExt.SavePatch2(patch1, connect);
            }
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket);
        }

        private async Task DoWorkMonthlyAsync(string connect)
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"
        SELECT [PartnerCare].*, Partner.Name as CustomerName 
        FROM [PartnerCare]
        LEFT JOIN Partner ON PartnerCare.PartnerId = Partner.Id
        WHERE DATEADD(DAY, -isnull(NotificationNumber,0), isnull(LastNotificationDate,NextDate)) >= '{startOfMonth:yyyy-MM-dd}' 
        AND DATEADD(DAY, -isnull(NotificationNumber,0), isnull(LastNotificationDate,NextDate)) <= '{endOfMonth:yyyy-MM-dd}'
        AND ReminderSettingId = 3", connect);
            if (partnerCare.Nothing())
            {
                return;
            }

            var tasks = partnerCare.Select(item =>
            {
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "PartnerCare",
                    Title = $"{item.CustomerName} monthly notification",
                    Description = item.TaskName,
                    InsertedBy = "-1",
                    RecordId = item.Id,
                    InsertedDate = DateTime.Now,
                    Icon = "fal fa-phone-alt",
                    Active = true,
                    AssignedId = item.AssigneeId ?? item.InsertedBy
                };
            }).ToList();
            foreach (var item in tasks)
            {
                var patch = item.MapToPatch();
                await BgExt.SavePatch2(patch, connect);
                var item1 = partnerCare.FirstOrDefault(x => x.Id == item.RecordId);
                item1.LastNotificationDate = DateTime.Now.Date;
                var patch1 = item1.MapToPatch();
                await BgExt.SavePatch2(patch1, connect);
            }
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket);
        }

        private async Task DoWorkYearlyAsync(string connect)
        {
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
            var endOfYear = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"
        SELECT [PartnerCare].*, Partner.Name as CustomerName 
        FROM [PartnerCare]
        LEFT JOIN Partner ON PartnerCare.PartnerId = Partner.Id
        WHERE DATEADD(DAY, -isnull(NotificationNumber,0), isnull(LastNotificationDate,NextDate)) >= '{startOfYear:yyyy-MM-dd}' 
        AND DATEADD(DAY, -isnull(NotificationNumber,0), isnull(LastNotificationDate,NextDate)) <= '{endOfYear:yyyy-MM-dd}'
        AND ReminderSettingId = 4", connect);
            if (partnerCare.Nothing())
            {
                return;
            }

            var tasks = partnerCare.Select(item =>
            {
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "PartnerCare",
                    Title = $"{item.CustomerName} yearly notification",
                    Description = item.TaskName,
                    InsertedBy = "-1",
                    Icon = "fal fa-phone-alt",
                    RecordId = item.Id,
                    InsertedDate = DateTime.Now,
                    Active = true,
                    AssignedId = item.AssigneeId ?? item.InsertedBy
                };
            }).ToList();
            foreach (var item in tasks)
            {
                var patch = item.MapToPatch();
                await BgExt.SavePatch2(patch, connect);
                var item1 = partnerCare.FirstOrDefault(x => x.Id == item.RecordId);
                item1.LastNotificationDate = DateTime.Now.Date;
                var patch1 = item1.MapToPatch();
                await BgExt.SavePatch2(patch1, connect);
            }
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket);
        }
    }
}
