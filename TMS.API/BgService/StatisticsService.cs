using Core.Enums;
using Core.Extensions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Data.SqlClient;
using System.Net.WebSockets;
using System.Text;
using TMS.API.Models;
using TMS.API.Websocket;

namespace TMS.API.BgService
{
    public class StatisticsService
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

        [Obsolete]
        public void ScheduleJob()
        {
            RecurringJob.AddOrUpdate<StatisticsService>(x => x.StatisticsProcesses(), Cron.Daily(0, 0));
        }

        public async Task StatisticsProcesses()
        {
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
                await UpdateInsuranceFeesDataFromTransportation(dbContext);
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
        #region UpdateInsuranceFees
        public async Task UpdateInsuranceFeesDataFromTransportation(TMSContext db)
        {
            var expenseTypes = await db.MasterData.Where(x => x.Active && x.ParentId == 7577 && (x.Name.Contains("Bảo hiểm") || x.Name.Contains("BH SOC"))).ToListAsync();
            var expenseTypeIds = expenseTypes.Select(x => x.Id).ToList();
            var now = DateTime.Now.Date;
            var fromDate = new DateTime(now.Year, now.Month - 2, 1);
            var toDate = new DateTime(now.Year, now.Month + 1, 1).AddMonths(1).AddDays(-1);
            var trans = await db.Transportation.AsNoTracking().Where(x => ((x.ClosingDate >= fromDate && x.ClosingDate <= toDate) || (x.StartShip >= fromDate && x.StartShip <= toDate)) && x.Active).ToListAsync();
            if (trans == null)
            {
                return;
            }

            var containerTypes = await db.MasterData.Where(x => x.ParentId == 7565).ToListAsync();
            var containerTypeIds = containerTypes.ToDictionary(x => x.Id);
            var containers = await db.MasterData.Where(x => x.Name.Contains("40HC") || x.Name.Contains("20DC") || x.Name.Contains("45HC") || x.Name.Contains("50DC")).ToListAsync();

            var bossIds = trans.Select(x => x.BossId).ToList();
            var commodityIds = trans.Select(x => x.CommodityId).ToList();
            var commodityValues = await db.CommodityValue.Where(x => bossIds.Contains(x.BossId) && commodityIds.Contains(x.CommodityId) && x.Active).ToListAsync();
            var commodityValuesSOC = await db.CommodityValue.Where(x => x.CommodityId == 15764 && x.Active).ToListAsync();
            var commodityValueOfTrans = new Dictionary<int, CommodityValue>();

            var transportationTypes = await db.MasterData.Where(x => x.ParentId == 11670).ToListAsync();
            var routes = await db.Route.Where(x => trans.Select(y => y.RouteId).ToList().Contains(x.Id)).ToListAsync();
            var vendors = await db.Vendor.Where(x => trans.Select(y => y.ClosingId).ToList().Contains(x.Id)).ToListAsync();
            foreach (var item in trans)
            {
                MasterData container = null;
                if (item.ContainerTypeId != null)
                {
                    container = containerTypeIds.GetValueOrDefault((int)item.ContainerTypeId);
                    if (container == null)
                    {
                        continue;
                    }
                    var containerId = 0;
                    if (container.Description.Contains("Cont 20"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("20DC")).Id;
                    }
                    else if (container.Description.Contains("Cont 40"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("40HC")).Id;
                    }
                    else if (container.Description.Contains("Cont 45"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("45HC")).Id;
                    }
                    else if (container.Description.Contains("Cont 50"))
                    {
                        containerId = containers.Find(x => x.Name.Contains("50DC")).Id;
                    }
                    var commodityValue = commodityValues.Where(x => x.BossId == item.BossId && x.CommodityId == item.CommodityId && x.ContainerId == containerId && (x.StartDate >= item.StartShip || item.StartShip == null)).FirstOrDefault();
                    if (commodityValue != null) { commodityValueOfTrans.Add(item.Id, commodityValue); }
                }
            }

            var tranIds = trans.Select(x => x.Id).ToList();
            var expenses = await db.Expense.Where(x => tranIds.Contains((int)x.TransportationId) && expenseTypeIds.Contains((int)x.ExpenseTypeId) && x.RequestChangeId == null && x.Active).ToListAsync();
            if (expenses == null)
            {
                return;
            }
            var expenseIds = expenses.Select(x => x.Id).ToList();
            var checkRequests = await db.Expense.Where(x => expenseIds.Contains((int)x.RequestChangeId) && x.StatusId == (int)ApprovalStatusEnum.Approving && x.Active).ToListAsync();

            var extraInsuranceFeesRateDB = await db.MasterData.Where(x => x.Active == true && x.ParentId == 25374).ToListAsync();
            var insuranceFeesRateColdDB = await db.MasterData.Where(x => x.Id == 25391).FirstOrDefaultAsync();
            var containerTypeIdExpenses = expenses.Select(x => x.ContainerTypeId).ToList();
            var containerTypeExpenses = await db.MasterData.Where(x => containerTypeIdExpenses.Contains(x.Id)).ToListAsync();
            var containerTypeOfExpenses = new Dictionary<int, MasterData>();

            var insuranceFeesRates = await db.InsuranceFeesRate.Where(x => x.Active).ToListAsync();
            foreach (var item in expenses)
            {
                var container = containerTypeExpenses.Where(x => x.Id == item.ContainerTypeId).FirstOrDefault();
                containerTypeOfExpenses.Add(item.Id, container);
            }
            var querys = "";
            var queryExs = "";
            foreach (var tran in trans)
            {
                if (tran.RouteId != null || tran.ClosingId != null)
                {
                    var route = routes.Where(x => x.Id == tran.RouteId).FirstOrDefault();
                    var vendor = vendors.Where(x => x.Id == tran.ClosingId).FirstOrDefault();
                    if (route != null || vendor != null)
                    {
                        if (vendor != null && vendor.Name.ToLower().Contains("sà lan"))
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("sà lan")).FirstOrDefault().Id;
                        }
                        else if (route != null && route.Name.ToLower().Contains("sắt"))
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("sắt")).FirstOrDefault().Id;
                        }
                        else if (route != null && (route.Name.ToLower().Contains("bộ") || route.Name.ToLower().Contains("trucking vtqt")))
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("bộ")).FirstOrDefault().Id;
                        }
                        else
                        {
                            tran.TransportationTypeId = transportationTypes.Where(x => x.Name.Trim().ToLower().Contains("tàu")).FirstOrDefault().Id;
                        }
                    }
                }
                var expensesByTran = expenses.Where(x => x.TransportationId == tran.Id).ToList();
                foreach (var ex in expensesByTran)
                {
                    var check = checkRequests.Where(x => x.RequestChangeId == ex.Id).Any();
                    if (((tran.RouteId != ex.RouteId) ||
                        (tran.ShipId != ex.ShipId) ||
                        (tran.BossId != ex.BossId) ||
                        (tran.ReceivedId != ex.ReceivedId) ||
                        (tran.CommodityId != ex.CommodityId && ex.ExpenseTypeId != 15981) ||
                        (tran.TransportationTypeId != ex.TransportationTypeId) ||
                        (tran.ContainerTypeId != ex.ContainerTypeId) ||
                        (tran.Trip?.Trim() != ex.Trip?.Trim()) ||
                        (tran.ContainerNo?.Trim() != ex.ContainerNo?.Trim()) ||
                        (tran.SealNo?.Trim() != ex.SealNo?.Trim()) ||
                        (tran.Cont20 != ex.Cont20) ||
                        (tran.Cont40 != ex.Cont40) ||
                        (tran.Note2?.Trim() != ex.Notes?.Trim()) ||
                        (tran.ClosingDate != ex.StartShip && (ex.JourneyId == 12114 || ex.JourneyId == 16001)) ||
                        (tran.StartShip != ex.StartShip && (ex.JourneyId != 12114 && ex.JourneyId != 16001))) && check == false)
                    {
                        if (ex.IsPurchasedInsurance)
                        {
                            var history = new Expense();
                            history.CopyPropFrom(ex);
                            history.Id = 0;
                            history.StatusId = (int)ApprovalStatusEnum.New;
                            history.RequestChangeId = ex.Id;
                            ex.IsHasChange = true;

                            var stringPropNames = history.GetType().GetProperties().Where(x =>
                                    x.Name != nameof(Expense.Id) &&
                                    x.Name != nameof(Expense.FromDate) &&
                                    x.Name != nameof(Expense.ToDate) &&
                                    x.Name != nameof(Expense.TransportationIds) &&
                                    x.Name != nameof(Expense.Allotment) &&
                                    x.Name != nameof(Expense.Transportation)).Select(x => x.Name).ToList();
                            var queryIn = $"INSERT INTO {nameof(Expense)}({stringPropNames.Combine()}";
                            queryIn += $") VALUES (";
                            foreach (var prop in history.GetType().GetProperties())
                            {
                                if (prop.Name != nameof(Expense.Id) &&
                                   prop.Name != nameof(Expense.FromDate) &&
                                   prop.Name != nameof(Expense.ToDate) &&
                                   prop.Name != nameof(Expense.TransportationIds) &&
                                   prop.Name != nameof(Expense.Allotment) &&
                                   prop.Name != nameof(Expense.Transportation))
                                {
                                    if (prop.PropertyType.Name == nameof(DateTime) || prop.PropertyType.FullName.Contains(nameof(DateTime)))
                                    {
                                        queryIn += prop.GetValue(history) != null ? $"'{DateTime.Parse(prop.GetValue(history).ToString()).ToString("yyyy-MM-dd")}', " : "NULL, ";
                                    }
                                    else
                                    {
                                        queryIn += prop.GetValue(history) != null ? $"'{prop.GetValue(history).ToString()}', " : "NULL, ";
                                    }
                                }
                            }
                            queryIn = queryIn.Remove(queryIn.Length - 2);
                            queryIn += "); ";
                            queryExs += queryIn;
                        }
                        if ((tran.BossId != ex.BossId ||
                            tran.CommodityId != ex.CommodityId ||
                            tran.ContainerTypeId != ex.ContainerTypeId ||
                            tran.TransportationTypeId != ex.TransportationTypeId) && ex.IsPurchasedInsurance == false)
                        {
                            var containerExpense = containerTypeOfExpenses.GetValueOrDefault(ex.Id);
                            CommodityValue commodityValue = null;
                            if (ex.ExpenseTypeId == 15939)
                            {
                                commodityValue = commodityValueOfTrans.GetValueOrDefault(tran.Id);
                                if (commodityValue != null)
                                {
                                    ex.CommodityValue = commodityValue.TotalPrice;
                                    ex.JourneyId = commodityValue.JourneyId;
                                    ex.IsBought = commodityValue.IsBought;
                                    ex.IsWet = commodityValue.IsWet;
                                    ex.SteamingTerms = commodityValue.SteamingTerms;
                                    ex.BreakTerms = commodityValue.BreakTerms;
                                    ex.CustomerTypeId = commodityValue.CustomerTypeId;
                                    ex.CommodityValueNotes = commodityValue.Notes;
                                    CalcInsuranceFees(ex, false, insuranceFeesRates, extraInsuranceFeesRateDB, containerExpense, insuranceFeesRateColdDB);
                                }
                            }
                            else if (ex.ExpenseTypeId == 15981)
                            {
                                commodityValue = commodityValuesSOC.Where(x => x.ContainerId == containerTypeOfExpenses.GetValueOrDefault(ex.Id).Id).FirstOrDefault();
                                if (commodityValue != null)
                                {
                                    ex.CommodityValue = commodityValue.TotalPrice;
                                    ex.CustomerTypeId = commodityValue.CustomerTypeId;
                                    ex.CommodityValueNotes = commodityValue.Notes;
                                    CalcInsuranceFees(ex, true, insuranceFeesRates, extraInsuranceFeesRateDB, containerExpense, insuranceFeesRateColdDB);
                                }
                            }
                        }
                        if (tran.TransportationTypeId != ex.TransportationTypeId) { ex.TransportationTypeId = tran.TransportationTypeId; }
                        if (tran.BossId != ex.BossId) { ex.BossId = tran.BossId; }
                        if (tran.CommodityId != ex.CommodityId && ex.ExpenseTypeId != 15981) { ex.CommodityId = tran.CommodityId; }
                        if (tran.ContainerTypeId != ex.ContainerTypeId) { ex.ContainerTypeId = tran.ContainerTypeId; }
                        if (tran.RouteId != ex.RouteId) { ex.RouteId = tran.RouteId; }
                        if (tran.ShipId != ex.ShipId) { ex.ShipId = tran.ShipId; }
                        if (tran.ReceivedId != ex.ReceivedId) { ex.ReceivedId = tran.ReceivedId; }
                        if (tran.Trip?.Trim() != ex.Trip?.Trim()) { ex.Trip = tran.Trip; }
                        if (tran.ContainerNo?.Trim() != ex.ContainerNo?.Trim()) { ex.ContainerNo = tran.ContainerNo; }
                        if (tran.SealNo?.Trim() != ex.SealNo?.Trim()) { ex.SealNo = tran.SealNo; }
                        if (tran.Cont20 != ex.Cont20) { ex.Cont20 = tran.Cont20; }
                        if (tran.Cont40 != ex.Cont40) { ex.Cont40 = tran.Cont40; }
                        if (tran.Note2?.Trim() != ex.Notes?.Trim()) { ex.Notes = tran.Note2; }
                        if (tran.ClosingDate != ex.StartShip && (ex.JourneyId == 12114 || ex.JourneyId == 16001)) { ex.StartShip = tran.ClosingDate; }
                        if (tran.StartShip != ex.StartShip && (ex.JourneyId != 12114 && ex.JourneyId != 16001)) { ex.StartShip = tran.StartShip; }

                        var query = $"Update {nameof(Expense)} set ";
                        foreach (var prop in ex.GetType().GetProperties())
                        {
                            if (prop.Name != nameof(Expense.Id) &&
                               prop.Name != nameof(Expense.FromDate) &&
                               prop.Name != nameof(Expense.ToDate) &&
                               prop.Name != nameof(Expense.TransportationIds) &&
                               prop.Name != nameof(Expense.Allotment) &&
                               prop.Name != nameof(Expense.Transportation))
                            {
                                query += $"{prop.Name} = ";
                                if (prop.PropertyType.Name == nameof(DateTime) || prop.PropertyType.FullName.Contains(nameof(DateTime)))
                                {
                                    query += prop.GetValue(ex) != null ? $"'{DateTime.Parse(prop.GetValue(ex).ToString()).ToString("yyyy-MM-dd")}', " : "NULL, ";
                                }
                                else
                                {
                                    query += prop.GetValue(ex) != null ? $"'{prop.GetValue(ex).ToString()}', " : "NULL, ";
                                }
                            }
                        }
                        query = query.Remove(query.Length - 2);
                        query += $" where Id = {ex.Id}; ";
                        queryExs += query;
                    }
                }
                var insuranceFee = expenses.Where(x => x.TransportationId == tran.Id && x.IsPurchasedInsurance).ToList().Sum(x => x.TotalPriceAfterTax);
                if (tran.InsuranceFee != insuranceFee)
                {
                    var query = $"Update {nameof(Transportation)} set InsuranceFee = {insuranceFee} where Id = {tran.Id}; ";
                    querys += query;
                }
            }
            if (queryExs != null && queryExs != "") { await ExecSql(queryExs, "DISABLE TRIGGER ALL ON Expense;", "ENABLE TRIGGER ALL ON Expense;"); }
            if (querys != null && querys != "") { await ExecSql(querys, "DISABLE TRIGGER ALL ON Transportation;", "ENABLE TRIGGER ALL ON Transportation;"); }
        }

        private void CalcInsuranceFees(Expense expense, bool isSOC, List<InsuranceFeesRate> insuranceFeesRates, List<MasterData> extraInsuranceFeesRateDB, MasterData containerExpense, MasterData insuranceFeesRateColdDB)
        {
            if (expense.IsPurchasedInsurance == false && expense.RequestChangeId == null)
            {
                bool isSubRatio = false;
                if (((expense.IsWet || expense.SteamingTerms || expense.BreakTerms) && expense.IsBought == false) || (expense.IsBought && expense.IsWet))
                {
                    isSubRatio = true;
                }
                InsuranceFeesRate insuranceFeesRateDB = null;
                if (expense.IsBought)
                {
                    insuranceFeesRateDB = insuranceFeesRates.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSOC == isSOC && x.IsSubRatio == isSubRatio).FirstOrDefault();
                }
                else
                {
                    insuranceFeesRateDB = insuranceFeesRates.Where(x => x.TransportationTypeId == expense.TransportationTypeId && x.JourneyId == expense.JourneyId && x.IsBought == expense.IsBought && x.IsSOC == isSOC).FirstOrDefault();
                }
                if (insuranceFeesRateDB != null)
                {
                    if (containerExpense != null && containerExpense.Description.ToLower().Contains("lạnh") && insuranceFeesRateDB.TransportationTypeId == 11673 && insuranceFeesRateDB.JourneyId == 12114)
                    {
                        expense.InsuranceFeeRate = insuranceFeesRateColdDB != null ? decimal.Parse(insuranceFeesRateColdDB.Name) : 0;
                    }
                    else
                    {
                        expense.InsuranceFeeRate = insuranceFeesRateDB.Rate;
                    }
                    if (insuranceFeesRateDB.IsSubRatio && expense.IsBought == false)
                    {
                        extraInsuranceFeesRateDB.ForEach(x =>
                        {
                            var prop = expense.GetType().GetProperties().Where(y => y.Name == x.Name && bool.Parse(y.GetValue(expense, null).ToString())).FirstOrDefault();
                            if (prop != null)
                            {
                                expense.InsuranceFeeRate += decimal.Parse(x.Code);
                            }
                        });
                    }
                }
                else
                {
                    expense.InsuranceFeeRate = 0;
                    expense.TotalPriceBeforeTax = 0;
                    expense.TotalPriceAfterTax = 0;
                }
                if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == true)
                {
                    expense.TotalPriceAfterTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
                    expense.TotalPriceBeforeTax = Math.Round(expense.TotalPriceAfterTax / (decimal)1.1, 0);
                }
                else if (insuranceFeesRateDB != null && insuranceFeesRateDB.IsVAT == false)
                {
                    expense.TotalPriceBeforeTax = (decimal)expense.InsuranceFeeRate * (decimal)expense.CommodityValue / 100;
                    expense.TotalPriceAfterTax = expense.TotalPriceBeforeTax + Math.Round(expense.TotalPriceBeforeTax * expense.Vat / 100, 0);
                }
            }
        }

        public async Task ExecSql(string sql, string disableTrigger, string enableTrigger)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        command.CommandText += disableTrigger;
                        command.CommandText += sql;
                        command.CommandText += enableTrigger;
                        await command.ExecuteNonQueryAsync();
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
        }
        #endregion
    }
}
