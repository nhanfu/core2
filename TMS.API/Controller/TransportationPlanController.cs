using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using TMS.API.Models;
using TMS.API.ViewModels;
using FileIO = System.IO.File;

namespace TMS.API.Controllers
{
    public class TransportationPlanController : TMSController<TransportationPlan>
    {
        private readonly HistoryContext hdb;
        public TransportationPlanController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor, HistoryContext historyContext) : base(context, entityService, httpContextAccessor)
        {
            hdb = historyContext;
        }

        [HttpPost("api/TransportationPlan/ImportExcel")]
        public async Task<List<TransportationPlan>> ImportExcel([FromServices] IWebHostEnvironment host, List<IFormFile> fileImport)
        {
            var formFile = fileImport.FirstOrDefault();
            if (formFile == null || formFile.Length <= 0)
            {
                return null;
            }

            if (!Path.GetExtension(formFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = GetUploadPath(formFile.FileName, host.WebRootPath);
            EnsureDirectoryExist(path);
            path = IncreaseFileName(path);
            using var stream = FileIO.Create(path);
            await formFile.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var currentSheet = package.Workbook.Worksheets;
            var worksheet = currentSheet.First();
            var noOfCol = worksheet.Dimension.End.Column;
            var noOfRow = worksheet.Dimension.End.Row;
            var list = new List<ImportTransportationPlan>();
            for (int row = 2; row <= noOfRow; row++)
            {
                if ((worksheet.Cells[row, 1].Value == null || worksheet.Cells[row, 1].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 2].Value == null || worksheet.Cells[row, 2].Value?.ToString() == "") &&
                    (worksheet.Cells[row, 3].Value == null || worksheet.Cells[row, 3].Value?.ToString() == ""))
                {
                    continue;
                }
                var route = worksheet.Cells[row, 4].Value?.ToString().Trim();
                var boss = worksheet.Cells[row, 5].Value?.ToString().Trim();
                var commodity = worksheet.Cells[row, 6].Value?.ToString().Trim();
                var container = worksheet.Cells[row, 7].Value?.ToString().Trim();
                var received = worksheet.Cells[row, 10].Value?.ToString().Trim();
                var transportationPlan = new ImportTransportationPlan()
                {
                    PlanDate = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                    UserText = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                    RouteText = ConvertTextVn(route),
                    RouteTextEn = ConvertTextEn(route),
                    BossText = ConvertTextVn(boss),
                    BossTextEn = ConvertTextEn(boss),
                    CommodityText = ConvertTextVn(commodity),
                    CommodityTextEn = ConvertTextEn(commodity),
                    ContainerText = ConvertTextVn(container),
                    ContainerTextEn = ConvertTextEn(container),
                    ContractText = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                    ClosingDate = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                    ReceivedText = ConvertTextVn(received),
                    ReceivedTextEn = ConvertTextEn(received),
                    TotalContainer = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                    TotalContainerUsing = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                    Notes = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                    Name = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                    User = worksheet.Cells[row, 18].Value?.ToString().Trim(),
                };
                list.Add(transportationPlan);
            }
            var listRouteCodes = list.Select(x => x.RouteTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listBossCodes = list.Select(x => x.BossTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listCommodityCodes = list.Select(x => x.CommodityTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listContainerCodes = list.Select(x => x.ContainerTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var listLocationCodes = list.Select(x => x.ReceivedTextEn).Where(x => x != null && x != "").Distinct().ToList();
            var rsRoute = await db.Route.ToListAsync();
            var routeDB = rsRoute.Where(x => listRouteCodes.Contains(ConvertTextEn(x.Name))).ToDictionary(x => ConvertTextEn(x.Name));
            var rsVendor = await db.Vendor.ToListAsync();
            var vendorDB1 = rsVendor.Where(x => listBossCodes.Contains(ConvertTextEn(x.Name)) && x.TypeId == 7551).ToList();
            var vendorDB = vendorDB1.ToDictionary(x => ConvertTextEn(x.Name));
            var rsMasterData = await db.MasterData.ToListAsync();
            var commodityDB1 = rsMasterData.Where(x => listCommodityCodes.Contains(ConvertTextEn(x.Description)) && x.Path.Contains(@"\7651\") && x.ParentId != 7651).ToList();
            var commodityDB = commodityDB1.ToDictionary(x => ConvertTextEn(x.Description));
            var containerDB = rsMasterData.Where(x => listContainerCodes.Contains(ConvertTextEn(x.Description)) && x.ParentId == 7565).ToDictionary(x => ConvertTextEn(x.Description));
            var rsLocation = await db.Location.ToListAsync();
            var locationDB1 = rsLocation.Where(x => listLocationCodes.Contains(ConvertTextEn(x.Description))).ToList();
            var locationDB = locationDB1.ToDictionary(x => ConvertTextEn(x.Description));
            var listSaleCodes = list.Select(x => x.UserText).Where(x => x != null && x != "").Distinct().ToList();
            var listUserCodes = list.Select(x => x.User).Where(x => x != null && x != "").Distinct().ToList();
            var userDB = await db.User.Where(x => listUserCodes.Contains(x.UserName) || listSaleCodes.Contains(x.UserName)).ToDictionaryAsync(x => x.UserName.ToLower());
            var vendorIds = vendorDB1.Select(x => x.Id).ToList();
            var locationIds = locationDB1.Select(x => x.Id).ToList();
            var vendorLocation = await db.VendorLocation.Where(x => vendorIds.Contains(x.VendorId ?? 0) && locationIds.Contains(x.LocationId ?? 0)).ToDictionaryAsync(x => $"{x.VendorId}-{x.LocationId}");
            foreach (var item in list)
            {
                User user = null;
                if (item.User != null && item.User != "")
                {
                    user = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.User.ToLower());
                }
                if (user is null && item.User != null && item.User != "")
                {
                    user = new User()
                    {
                        FullName = item.User,
                        UserName = item.User,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    user.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    user.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + user.Salt);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    userDB.Add(user.UserName.ToLower(), user);
                }
                User sale = null;
                if (item.UserText != null && item.UserText != "")
                {
                    sale = userDB.Count == 0 ? null : userDB.GetValueOrDefault(item.UserText.ToLower());
                }
                if (sale is null && item.UserText != null && item.UserText != "")
                {
                    sale = new User()
                    {
                        FullName = item.UserText,
                        UserName = item.UserText,
                        VendorId = 65,
                        HasVerifiedEmail = true,
                        GenderId = 390,
                        Active = true,
                        InsertedBy = 1,
                        InsertedDate = DateTime.Now
                    };
                    sale.Salt = _userSvc.GenerateRandomToken();
                    var randomPassword = "123";
                    sale.Password = _userSvc.GetHash(UserUtils.sHA256, randomPassword + sale.Salt);
                    db.Add(sale);
                    await db.SaveChangesAsync();
                    userDB.Add(sale.UserName.ToLower(), sale);
                }
                Models.Route route = null;
                if (item.RouteText != null && item.RouteText != "")
                {
                    route = routeDB.Count == 0 ? null : routeDB.GetValueOrDefault(item.RouteTextEn);
                }
                if (route is null && item.RouteText != null && item.RouteText != "")
                {
                    route = new TMS.API.Models.Route()
                    {
                        Name = item.RouteText,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(route);
                    await db.SaveChangesAsync();
                    routeDB.Add(ConvertTextEn(route.Name), route);
                }
                Vendor vendor = null;
                if (item.BossText != null && item.BossText != "")
                {
                    vendor = vendorDB.Count == 0 ? null : vendorDB.GetValueOrDefault(item.BossTextEn);
                }
                if (vendor is null && item.BossText != null && item.BossText != "")
                {
                    vendor = new Vendor()
                    {
                        Name = item.BossText,
                        UserId = sale.Id,
                        TypeId = 7551,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendor);
                    await db.SaveChangesAsync();
                    vendorDB.Add(ConvertTextEn(vendor.Name), vendor);
                }

                MasterData commodity = null;
                if (item.CommodityText != null && item.CommodityText != "")
                {
                    commodity = commodityDB.Count == 0 ? null : commodityDB.GetValueOrDefault(item.CommodityTextEn);
                }
                if (commodity is null && item.CommodityText != null && item.CommodityText != "")
                {
                    commodity = new MasterData()
                    {
                        Description = item.CommodityText,
                        Name = item.CommodityText,
                        ParentId = 7651, //CommodityType
                        Path = @"\7651\",
                        Level = 1,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(commodity);
                    await db.SaveChangesAsync();
                    commodityDB.Add(ConvertTextEn(commodity.Description), commodity);
                }
                MasterData container = null;
                if (item.ContainerText != null && item.ContainerText != "")
                {
                    container = containerDB.Count == 0 ? null : containerDB.GetValueOrDefault(item.ContainerTextEn);
                }
                if (container is null && item.ContainerText != null && item.ContainerText != "")
                {
                    container = new MasterData()
                    {
                        Description = item.ContainerText,
                        Name = item.ContainerText,
                        ParentId = 7565, //ExpenseType
                        Path = @"\7565\",
                        Level = 1,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(container);
                    await db.SaveChangesAsync();
                    containerDB.Add(ConvertTextEn(container.Description), container);
                }
                Location rece = null;
                if (item.ReceivedText != null && item.ReceivedText != "")
                {
                    rece = locationDB.Count == 0 ? null : locationDB.GetValueOrDefault(item.ReceivedTextEn);
                }
                if (rece is null && item.ReceivedText != null && item.ReceivedText != "")
                {
                    rece = new Location()
                    {
                        Description = item.ReceivedText,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    rece.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7581,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    });
                    rece.LocationService.Add(new LocationService()
                    {
                        ServiceId = 7582,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    });
                    db.Add(rece);
                    await db.SaveChangesAsync();
                    locationDB.Add(ConvertTextEn(rece.Description), rece);
                }
                VendorLocation vendorLocation1 = null;
                if (vendor != null && rece != null)
                {
                    vendorLocation1 = vendorLocation.Count == 0 ? null : vendorLocation.GetValueOrDefault($"{vendor.Id}-{rece.Id}");
                }
                if (vendorLocation1 is null && vendor != null && rece != null)
                {
                    vendorLocation1 = new VendorLocation()
                    {
                        VendorId = vendor.Id,
                        LocationId = rece.Id,
                        Active = true,
                        InsertedBy = user is null ? 1 : user.Id,
                        InsertedDate = DateTime.Now
                    };
                    db.Add(vendorLocation1);
                    await db.SaveChangesAsync();
                    vendorLocation.Add($"{vendor.Id}-{rece.Id}", vendorLocation1);
                }
                var tranp = new TransportationPlan()
                {
                    PlanDate = item.PlanDate is null ? null : DateTime.Parse(item.PlanDate),
                    UserId = sale is null ? null : sale.Id,
                    RouteId = route is null ? null : route.Id,
                    BossId = vendor is null ? null : vendor.Id,
                    CommodityId = commodity is null ? null : commodity.Id,
                    ContainerTypeId = container is null ? null : container.Id,
                    IsContract = item.ContractText == "1" ? true : false,
                    ClosingDate = item.ClosingDate is null ? null : DateTime.Parse(item.ClosingDate),
                    ReceivedId = rece is null ? null : rece.Id,
                    TotalContainer = int.Parse((item.TotalContainer == "" || item.TotalContainer is null) ? "0" : item.TotalContainer),
                    TotalContainerUsing = int.Parse((item.TotalContainerUsing == "" || item.TotalContainerUsing is null) ? "0" : item.TotalContainerUsing),
                    TotalContainerRemain = int.Parse((item.TotalContainer == "" || item.TotalContainer is null) ? "0" : item.TotalContainer) - int.Parse((item.TotalContainerUsing == "" || item.TotalContainerUsing is null) ? "0" : item.TotalContainerUsing),
                    Notes = item.Notes,
                    NotesContract = item.Notes1,
                    Active = true,
                    InsertedBy = user is null ? 1 : user.Id,
                    InsertedDate = DateTime.Now,
                    Name = item.Name
                };
                db.Add(tranp);
            }
            await db.SaveChangesAsync();
            return null;
        }

        protected override IQueryable<TransportationPlan> GetQuery()
        {
            var qr = base.GetQuery();
            if (RoleIds.Contains(10))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId
                    select tranp;
            }
            else if (RoleIds.Contains(17))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId || (tranp.UserId == 78 && policy != null)
                    select tranp;
            }
            else if (RoleIds.Contains(43))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId || tranp.UserId == 78
                    select tranp;
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(27))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId && route.TypeId == 25045
                     select tr;
            }
            return qr;
        }

        [HttpGet("api/[Controller]/GetByRole")]
        public Task<OdataResult<TransportationPlan>> UserClick(ODataQueryOptions<TransportationPlan> options)
        {
            var sql = string.Empty;
            sql += @$"
                    select *
                    from [{typeof(TransportationPlan).Name}]
                    where 1 = 1 and RequestChangeId is null";
            if (RoleIds.Contains(10))
            {
                sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
            }
            if (RoleIds.Contains(17))
            {
                sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
            }
            if (RoleIds.Contains(43))
            {
                sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
            }
            if (RoleIds.Contains(25) || RoleIds.Contains(27))
            {
                sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
            }
            var qr = db.TransportationPlan.AsNoTracking();
            if (RoleIds.Contains(10))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId
                    select tranp;
            }
            else if (RoleIds.Contains(17))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId || (tranp.UserId == 78 && policy != null)
                    select tranp;
            }
            else if (RoleIds.Contains(43))
            {
                qr =
                    from tranp in qr
                    from policy in db.FeaturePolicy
                        .Where(x => x.RecordId == tranp.BossId && x.EntityId == _entitySvc.GetEntity(nameof(Vendor)).Id && x.CanRead)
                        .Where(x => x.UserId == _userSvc.UserId || _userSvc.AllRoleIds.Contains(x.RoleId.Value))
                        .DefaultIfEmpty()
                    where tranp.InsertedBy == UserId
                        || policy != null || tranp.Id == _userSvc.VendorId || tranp.UserId == UserId || tranp.UserId == 78
                    select tranp;
            }
            else if (RoleIds.Contains(25) || RoleIds.Contains(27))
            {
                qr = from tr in qr
                     join route in db.UserRoute.AsNoTracking()
                     on tr.RouteId equals route.RouteId
                     where route.UserId == UserId && route.TypeId == 25045
                     select tr;
            }
            return ApplyQuery(options, qr, sql: sql);
        }

        public override async Task<ActionResult<TransportationPlan>> PatchAsync([FromQuery] ODataQueryOptions<TransportationPlan> options, [FromBody] PatchUpdate patch, [FromQuery] bool disableTrigger = false)
        {
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            var idInt = id.TryParseInt() ?? 0;
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("Default")))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        var updates = patch.Changes.Where(x => x.Field != IdField).ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                        if (disableTrigger)
                        {
                            command.CommandText += $" DISABLE TRIGGER ALL ON [TransportationPlan];";
                        }
                        else
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [TransportationPlan];";
                        }
                        command.CommandText += $" UPDATE [TransportationPlan] SET {update.Combine()} WHERE Id = {idInt};";
                        if (disableTrigger)
                        {
                            command.CommandText += $" ENABLE TRIGGER ALL ON [TransportationPlan];";
                        }
                        foreach (var item in updates)
                        {
                            command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        var entity = await db.TransportationPlan.FindAsync(idInt);
                        if (!disableTrigger)
                        {
                            await db.Entry(entity).ReloadAsync();
                        }
                        return entity;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var entity = await db.TransportationPlan.FindAsync(idInt);
                    return entity;
                }
            }
        }

        public static string ConvertTextEn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.ToLower().Trim(), @"\s+", " ");
        }

        public static string ConvertTextVn(string text)
        {
            return text is null || text == "" ? "" : Regex.Replace(text.Trim(), @"\s+", " ");
        }

        public override async Task<ActionResult<bool>> Approve([FromBody] TransportationPlan entity, string reasonOfChange = "")
        {
            var rs = await base.Approve(entity, reasonOfChange);
            await db.Entry(entity).ReloadAsync();
            var oldEntity = await db.TransportationPlan.FindAsync(entity.RequestChangeId);
            oldEntity.CopyPropFrom(entity, nameof(TransportationPlan.Id), nameof(TransportationPlan.RequestChangeId), nameof(TransportationPlan.InsertedDate), nameof(TransportationPlan.InsertedBy));
            var user = await db.User.FindAsync(UserId);
            var taskNotification = new TaskNotification
            {
                Title = $"{user.FullName}",
                Description = $"Đã duyệt yêu cầu chỉnh sửa ạ",
                EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                RecordId = oldEntity.Id,
                Attachment = "fal fa-check",
                AssignedId = entity.InsertedBy,
                StatusId = (int)TaskStateEnum.UnreadStatus,
                RemindBefore = 540,
                Deadline = DateTime.Now,
            };
            SetAuditInfo(taskNotification);
            db.AddRange(taskNotification);
            await db.SaveChangesAsync();
            await _taskService.NotifyAsync(new List<TaskNotification> { taskNotification });
            await db.Entry(oldEntity).ReloadAsync();
            RealTimeUpdateUser(oldEntity);
            return rs;
        }

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> SubTotal([FromServices] IServiceProvider serviceProvider
            , [FromServices] IConfiguration config
            , [FromBody] string sum
            , [FromQuery] string group
            , [FromQuery] string tablename
            , [FromQuery] string refname
            , [FromQuery] string formatsumary
            , [FromQuery] string orderby
            , [FromQuery] string sql
            , [FromQuery] bool showNull
            , [FromQuery] string datetimeField
            , [FromQuery] string where)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery += $@"select {sum}
                                  from ({sql}) as [{tablename}] where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                if (RoleIds.Contains(10))
                {
                    sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                }
                if (RoleIds.Contains(17))
                {
                    sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                }
                if (RoleIds.Contains(43))
                {
                    sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                }
                if (RoleIds.Contains(25) || RoleIds.Contains(27))
                {
                    sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
                }
            }
            var sqlCmd = new SqlCommand(reportQuery, con)
            {
                CommandType = CommandType.Text
            };
            con.Open();
            var tables = new List<List<Dictionary<string, object>>>();
            using (var reader = await sqlCmd.ExecuteReaderAsync())
            {
                do
                {
                    var table = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        table.Add(Read(reader));
                    }
                    tables.Add(table);
                } while (reader.NextResult());
            }
            return tables;
        }

        public override async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ViewSumary([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromBody] string sum, [FromQuery] string group, [FromQuery] string tablename, [FromQuery] string refname, [FromQuery] string formatsumary, [FromQuery] string orderby, [FromQuery] string sql, [FromQuery] string where)
        {
            var connectionStr = _config.GetConnectionString("Default");
            using var con = new SqlConnection(connectionStr);
            var reportQuery = string.Empty;
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery += $@"select {group},{formatsumary} as TotalRecord,{sum}
                                  from ({sql})   as [{tablename}] where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                reportQuery += $@"select {group},{formatsumary} as TotalRecord,{sum}
                                 from [{tablename}]
                                 where RequestChangeId is null and 1 = 1 {(where.IsNullOrWhiteSpace() ? $"" : $"and {where}")}";
                if (RoleIds.Contains(10))
                {
                    sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                }
                if (RoleIds.Contains(17))
                {
                    sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                }
                if (RoleIds.Contains(43))
                {
                    sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                }
                if (RoleIds.Contains(25) || RoleIds.Contains(27))
                {
                    sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
                }
            }
            reportQuery += $@" group by {group}
                                 order by {formatsumary} {orderby}";
            if (!refname.IsNullOrEmpty())
            {
                if (!sql.IsNullOrWhiteSpace())
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select distinct {group}
                                 from ({sql})  as [{tablename}] where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")})";
                }
                else
                {
                    reportQuery += $@" select *
                                 from [{refname}] 
                                 where Id in (select {group}
                                              from [{tablename}]
                                 where RequestChangeId is null {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
                    if (RoleIds.Contains(10))
                    {
                        sql += @$" and ((UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                    }
                    if (RoleIds.Contains(17))
                    {
                        sql += @$" and ((UserId = 78 and BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})) or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId})";
                    }
                    if (RoleIds.Contains(43))
                    {
                        sql += @$" and (UserId = 78 or UserId = {UserId} or InsertedBy = {UserId}) or BossId in (select RecordId from FeaturePolicy where EntityId = {_entitySvc.GetEntity(nameof(Vendor)).Id} and CanRead = 1 and UserId = {UserId}))";
                    }
                    if (RoleIds.Contains(25) || RoleIds.Contains(27))
                    {
                        sql += @$" and (RouteId in (select RouteId from UserRoute where UserId = {UserId} and TypeId = 25045))";
                    }
                }
            }
            var sqlCmd = new SqlCommand(reportQuery, con)
            {
                CommandType = CommandType.Text
            };
            con.Open();
            var tables = new List<List<Dictionary<string, object>>>();
            using (var reader = await sqlCmd.ExecuteReaderAsync())
            {
                do
                {
                    var table = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        table.Add(Read(reader));
                    }
                    tables.Add(table);
                } while (reader.NextResult());
            }
            return tables;
        }

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<int> ids)
        {
            if (ids.Nothing())
            {
                return true;
            }
            ids = ids.Where(x => x > 0).ToList();
            if (ids.Nothing())
            {
                return true;
            }
            var check = await db.TransportationPlan.AnyAsync(x => ids.Contains(x.Id) && x.TotalContainerUsing > 0);
            if (check)
            {
                throw new ApiException("Dữ liệu đã được lấy qua danh sách, bạn không thể xóa!") { StatusCode = HttpStatusCode.BadRequest };
            }
            try
            {
                var deleteCommand = $"delete from [{typeof(TransportationPlan).Name}] where Id in ({string.Join(",", ids)})";
                await ctx.Database.ExecuteSqlRawAsync(deleteCommand);
                return true;
            }
            catch
            {
                throw new ApiException("Không thể xóa dữ liệu!") { StatusCode = HttpStatusCode.BadRequest };
            }
        }

        public override async Task<ActionResult<bool>> RequestApprove([FromBody] TransportationPlan entity)
        {
            var id = entity.GetPropValue(nameof(GridPolicy.Id)) as int?;
            var (statusField, value) = entity.GetComplexProp("StatusId");
            if (statusField)
            {
                entity.SetPropValue("StatusId", (int)ApprovalStatusEnum.Approving);
            }

            if (id <= 0)
            {
                await CreateAsync(entity);
            }
            var approvalConfig = await GetApprovalConfig(entity);
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == 1);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            await Approving(entity);
            var oldEntity = await db.TransportationPlan.FindAsync(entity.RequestChangeId);
            await db.Entry(oldEntity).ReloadAsync();
            var listUser = await GetApprovalUsers(entity, matchApprovalConfig);
            if (listUser.HasElement())
            {
                var currentUser = await db.User.FirstOrDefaultAsync(x => x.Id == UserId);
                var tasks = listUser.Select(user => new TaskNotification
                {
                    Title = $"{currentUser.FullName}",
                    Description = $"Đã gửi yêu chỉnh sửa ạ ",
                    EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                    RecordId = oldEntity.Id,
                    Attachment = "fal fa-paper-plane",
                    AssignedId = user.Id,
                    StatusId = (int)TaskStateEnum.UnreadStatus,
                    RemindBefore = 540,
                    Deadline = DateTime.Now,
                });
                SetAuditInfo(tasks);
                db.AddRange(tasks);
                await db.SaveChangesAsync();
                await _taskService.NotifyAsync(tasks);
                RealTimeUpdateUser(oldEntity);
            }
            return true;
        }

        private void RealTimeUpdateUser(TransportationPlan entity)
        {
            var thead = new Thread(async () =>
            {
                try
                {
                    await _taskService.SendMessageAllUser(new WebSocketResponse<TransportationPlan>
                    {
                        EntityId = _entitySvc.GetEntity(typeof(TransportationPlan).Name).Id,
                        Data = entity
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("RealtimeUpdate error at {0}: {1} {2}", DateTimeOffset.Now, ex.Message, ex.StackTrace);
                }
            });
            thead.Start();
        }
    }
}
