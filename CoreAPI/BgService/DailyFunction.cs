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
            var connect = _config.GetConnectionString("logistics");
            await DoWorkNextTimeAsync(connect);
            await DoWorkDailyAsync(connect);
            await DoWorkWeeklyAsync(connect);
            await DoWorkMonthlyAsync(connect);
            await DoWorkYearlyAsync(connect);
        }

        private async Task DoWorkNextTimeAsync(string connect)
        {
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>(
                $@"select [PartnerCare].*,Partner.Name as CustomerName ,Partner.TypeId, Partner.ServiceId
                from [PartnerCare] 
                left join Partner on PartnerCare.PartnerId = Partner.Id 
                where DATEADD(DAY, -isnull(NotificationNumber,0), NextDate) = '{DateTime.Now:yyyy-MM-dd}' and ReminderSettingId is null", connect);
            var tasks = partnerCare.Select(item =>
            {
                var featureName = "customer";
                var featureName2 = "customer-editor";
                var featureName3 = "customer-editor";
                if (item.TypeId == 2 && item.ServiceId == 1)
                {
                    featureName = "prospect";
                    featureName2 = "prospect-editor";
                    featureName3 = "prospect-editor";
                }
                if (item.ServiceId == 2)
                {
                    featureName = "partner";
                    featureName2 = "partner-editor";
                    featureName3 = "partner-editor";
                }
                if (item.ServiceId == 3)
                {
                    featureName = "provider";
                    featureName2 = "provider-editor";
                    featureName3 = "provider-editor";
                }
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "Partner",
                    Title = $"{item.CustomerName} activities",
                    Title2 = $"{item.CustomerName} activities",
                    Icon = "fal fa-phone-alt",
                    Description = item.TaskName,
                    FeatureName = featureName,
                    FeatureName2 = featureName2,
                    FeatureName3 = featureName3,
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
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket, "dev");
        }

        private async Task DoWorkDailyAsync(string connect)
        {
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"select [PartnerCare].*,Partner.Name as CustomerName ,Partner.TypeId, Partner.ServiceId
            from [PartnerCare] 
            left join Partner on PartnerCare.PartnerId = Partner.Id 
            where NextDate >= '{DateTime.Now:yyyy-MM-dd}' and Deadline <= '{DateTime.Now:yyyy-MM-dd}' and ReminderSettingId = 1", connect);
            if (partnerCare.Nothing())
            {
                return;
            }
            var tasks = partnerCare.Select(item =>
            {
                var featureName = "customer";
                var featureName2 = "customer-editor";
                var featureName3 = "customer-editor";
                if (item.TypeId == 2 && item.ServiceId == 1)
                {
                    featureName = "prospect";
                    featureName2 = "prospect-editor";
                    featureName3 = "prospect-editor";
                }
                if (item.ServiceId == 2)
                {
                    featureName = "partner";
                    featureName2 = "partner-editor";
                    featureName3 = "partner-editor";
                }
                if (item.ServiceId == 3)
                {
                    featureName = "provider";
                    featureName2 = "provider-editor";
                    featureName3 = "provider-editor";
                }
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "Partner",
                    Title = $"{item.CustomerName} daily notification",
                    Title2 = $"{item.CustomerName} daily notification",
                    Icon = "fal fa-phone-alt",
                    Description = item.TaskName,
                    FeatureName = featureName,
                    FeatureName2 = featureName2,
                    FeatureName3 = featureName3,
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
                var item1 = partnerCare.FirstOrDefault(x => x.Id == item.RecordId);
                item1.LastNotificationDate = DateTime.Now.Date;
                var patch1 = item1.MapToPatch();
                await BgExt.SavePatch2(patch1, connect);
            }
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket, "dev");
        }

        private async Task DoWorkWeeklyAsync(string connect)
        {
            var startOfWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7).AddSeconds(-1);
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"
        SELECT [PartnerCare].*, Partner.Name as CustomerName  ,Partner.TypeId, Partner.ServiceId
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
                var featureName = "customer";
                var featureName2 = "customer-editor";
                var featureName3 = "customer-editor";
                if (item.TypeId == 2 && item.ServiceId == 1)
                {
                    featureName = "prospect";
                    featureName2 = "prospect-editor";
                    featureName3 = "prospect-editor";
                }
                if (item.ServiceId == 2)
                {
                    featureName = "partner";
                    featureName2 = "partner-editor";
                    featureName3 = "partner-editor";
                }
                if (item.ServiceId == 3)
                {
                    featureName = "provider";
                    featureName2 = "provider-editor";
                    featureName3 = "provider-editor";
                }
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "Partner",
                    Title = $"{item.CustomerName} weekly notification",
                    Title2 = $"{item.CustomerName} weekly notification",
                    Icon = "fal fa-phone-alt",
                    Description = item.TaskName,
                    FeatureName = featureName,
                    FeatureName2 = featureName2,
                    FeatureName3 = featureName3,
                    InsertedBy = "-1",
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
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket, "dev");
        }

        private async Task DoWorkMonthlyAsync(string connect)
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"
        SELECT [PartnerCare].*, Partner.Name as CustomerName  ,Partner.TypeId, Partner.ServiceId
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
                var featureName = "customer";
                var featureName2 = "customer-editor";
                var featureName3 = "customer-editor";
                if (item.TypeId == 2 && item.ServiceId == 1)
                {
                    featureName = "prospect";
                    featureName2 = "prospect-editor";
                    featureName3 = "prospect-editor";
                }
                if (item.ServiceId == 2)
                {
                    featureName = "partner";
                    featureName2 = "partner-editor";
                    featureName3 = "partner-editor";
                }
                if (item.ServiceId == 3)
                {
                    featureName = "provider";
                    featureName2 = "provider-editor";
                    featureName3 = "provider-editor";
                }
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "Partner",
                    Title = $"{item.CustomerName} monthly notification",
                    Title2 = $"{item.CustomerName} monthly notification",
                    Icon = "fal fa-phone-alt",
                    Description = item.TaskName,
                    FeatureName = featureName,
                    FeatureName2 = featureName2,
                    FeatureName3 = featureName3,
                    InsertedBy = "-1",
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
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket, "dev");
        }

        private async Task DoWorkYearlyAsync(string connect)
        {
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
            var endOfYear = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);
            var partnerCare = await BgExt.ReadDsAsArr<PartnerCare>($@"
        SELECT [PartnerCare].*, Partner.Name as CustomerName  ,Partner.TypeId, Partner.ServiceId
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
                var featureName = "customer";
                var featureName2 = "customer-editor";
                var featureName3 = "customer-editor";
                if (item.TypeId == 2 && item.ServiceId == 1)
                {
                    featureName = "prospect";
                    featureName2 = "prospect-editor";
                    featureName3 = "prospect-editor";
                }
                if (item.ServiceId == 2)
                {
                    featureName = "partner";
                    featureName2 = "partner-editor";
                    featureName3 = "partner-editor";
                }
                if (item.ServiceId == 3)
                {
                    featureName = "provider";
                    featureName2 = "provider-editor";
                    featureName3 = "provider-editor";
                }
                return new TaskNotification()
                {
                    Id = "-" + Uuid7.Guid().ToString(),
                    EntityId = "Partner",
                    Title = $"{item.CustomerName} yearly notification",
                    Title2 = $"{item.CustomerName} yearly notification",
                    Icon = "fal fa-phone-alt",
                    Description = $"{item.TaskName}",
                    FeatureName = featureName,
                    FeatureName2 = featureName2,
                    FeatureName3 = featureName3,
                    InsertedBy = "-1",
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
            await BgExt.NotifyDevices(tasks, "MessageNotification", _socket, "dev");
        }
    }
}
