using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Services;
using CoreAPI.Services.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Buffers;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HttpStatusCode = Core.Enums.HttpStatusCode;

namespace Core.Services;

public class UserService
{
    public readonly IHttpContextAccessor _ctx;
    private readonly HttpRequest _request;
    private readonly IConfiguration _cfg;
    private readonly IDistributedCache _cache;
    private readonly IWebHostEnvironment _host;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider iServiceProvider;
    private readonly SendMailService _sendMailService;
    private readonly IConfiguration _configuration;
    public readonly ISqlProvider _sql;
    public readonly ILogger<UserService> _logger;
    public string GroupId { get; set; }
    public string DepartmentId { get; set; }
    public string UserId { get; set; }
    public string FullName { get; set; }
    public string UserName { get; set; }
    public string Avatar { get; set; }
    public string CLogo { get; set; }
    public string CCompanyName { get; set; }
    public string CAddress { get; set; }
    public string CPhoneNumber { get; set; }
    public string CEmail { get; set; }
    public string ConnKey { get; set; }
    public string BranchId { get; set; }
    public List<string> CenterIds { get; set; }
    public string VendorId { get; set; }
    public string Env { get; set; }
    public string TenantCode { get; set; }
    public List<string> RoleIds { get; set; }
    public List<string> RoleNames { get; set; }
    public UserService(IHttpContextAccessor ctx, IConfiguration conf, IDistributedCache cache, IWebHostEnvironment host,
        IHttpClientFactory httpClientFactory, ISqlProvider sql, IConfiguration configuration,
        SendMailService sendMailService, IServiceProvider serviceProvider, ILogger<UserService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cfg = conf ?? throw new ArgumentNullException(nameof(conf));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache)); ;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _request = _ctx.HttpContext.Request;
        _sendMailService = sendMailService;
        iServiceProvider = serviceProvider;
        _logger = logger;
        ExtractMeta();
        _sql = sql;
        SetMetaToSqlProvider(_sql);
    }

    public void SetMetaToSqlProvider(ISqlProvider _sql)
    {
        _sql.TenantCode = TenantCode;
        _sql.Env = Env;
        _sql.UserId = UserId;
        _sql.UserId = UserId;
        _sql.SystemFields = new List<string>
        {
            UserServiceHelpers.IdField, nameof(User.InsertedBy), nameof(User.InsertedDate), nameof(User.UpdatedBy), nameof(User.UpdatedDate)
        }.Select(x => x.ToLower()).ToList();
        if (TenantCode?.ToLower() != "system") _sql.SystemFields.Add("TenantCode");
    }

    private void ExtractMeta()
    {
        var claims = _ctx.HttpContext?.User?.Claims;
        if (claims is null) return;
        BranchId = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.BranchIdClaim)?.Value;
        UserId = claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        FullName = claims.FirstOrDefault(x => x.Type == "FullName")?.Value;
        Avatar = claims.FirstOrDefault(x => x.Type == "Avatar")?.Value;
        GroupId = claims.FirstOrDefault(x => x.Type == "TeamId")?.Value;
        DepartmentId = claims.FirstOrDefault(x => x.Type == "DepartmentId")?.Value;
        UserName = claims.FirstOrDefault(x => x.Type == "UserName")?.Value;
        CenterIds = claims.Where(x => x.Type == nameof(CenterIds)).Select(x => x.Value).Where(x => x != null).ToList();
        RoleIds = claims.Where(x => x.Type == "RoleIds").Select(x => x.Value).Where(x => x != null).ToList();
        RoleNames = claims.Where(x => x.Type == UserServiceHelpers.RoleNameClaim).Select(x => x.Value).Where(x => x != null).ToList();
        VendorId = claims.FirstOrDefault(x => x.Type == "PartnerId")?.Value;
        TenantCode = claims.FirstOrDefault(x => x.Type == UserServiceHelpers.TenantClaim)?.Value.ToUpper();
        CLogo = claims.FirstOrDefault(x => x.Type == "CLogo")?.Value;
        CCompanyName = claims.FirstOrDefault(x => x.Type == "CCompanyName")?.Value;
        CAddress = claims.FirstOrDefault(x => x.Type == "CAddress")?.Value;
        CPhoneNumber = claims.FirstOrDefault(x => x.Type == "CPhoneNumber")?.Value;
        CEmail = claims.FirstOrDefault(x => x.Type == "CEmail")?.Value;
    }

    public async Task<Dictionary<string, object>[]> GetDictionary()
    {
        var query = @$"select * from [Dictionary]";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }
    public async Task<Dictionary<string, object>[]> MyNotification()
    {
        var query = @$"select * from [TaskNotification] where AssignedId = '{UserId}' order by InsertedDate desc";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public async Task<Dictionary<string, object>[]> WebConfig()
    {
        var query = @$"select * from [WebConfig]";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public async Task<Dictionary<string, object>[]> SalesFunction()
    {
        var query = @$"select * from [SaleFunction]";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public async Task<bool> PostUserSetting(UserSetting userSetting)
    {
        var query = @$"select * from [UserSetting] where UserId = '{UserId}' and ComponentId = '{userSetting.ComponentId}' and FeatureId = '{userSetting.FeatureId}'";
        var setting = await _sql.ReadDsAs<UserSetting>(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        if (setting != null)
        {
            setting.Value = userSetting.Value;
            setting.UpdatedBy = UserId;
            setting.UpdatedDate = DateTime.Now;
            var patch = setting.MapToPatch();
            await UpdatePatch(patch);
        }
        else
        {
            setting = new UserSetting();
            setting.Id = Uuid7.Guid().ToString();
            setting.ComponentId = userSetting.ComponentId;
            setting.FeatureId = userSetting.FeatureId;
            setting.UserId = UserId;
            setting.Active = true;
            setting.Value = userSetting.Value;
            setting.InsertedBy = UserId;
            setting.InsertedDate = DateTime.Now;
            var patch = setting.MapToPatch();
            await SavePatch(patch);
        }
        return true;
    }

    public async Task<SqlResult> Go(SqlViewModel sqlViewModel)
    {
        var query = @$"select * from [{sqlViewModel.Table}] where Id in ({sqlViewModel.Id.CombineStrings()})";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return new SqlResult()
        {
            data = ds[0],
            status = 200,
            message = "Select successful"
        };
    }

    public async Task<Dictionary<string, object>[][]> Gos(List<Gos> gos)
    {
        var query = gos.Select(x => @$"select * from [{x.TableName}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds;
    }

    public async Task<SqlResult> GoByName(SqlViewModel sqlViewModel)
    {
        var param = sqlViewModel.Id
        .Select((x, index) => new WhereParamVM()
        {
            FieldName = "@id" + (index + 1),
            Value = x
        }).ToList();
        var query = @$"select * from [{sqlViewModel.Table}] where [{sqlViewModel.Format.Replace("{", "").Replace("}", "")}] in ({param.Select(x => x.FieldName).ToList().Combine()})";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"), false, param);
        return new SqlResult()
        {
            data = ds[0],
            status = 200,
            message = "Select successful"
        };
    }

    public async Task<Conversation> Conversation(Conversation entity)
    {
        var query = @$"select * from [Conversation] where RecordId = '{entity.RecordId}' and EntityId = '{entity.EntityId}'";
        var conversation = await _sql.ReadDsAs<Conversation>(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        if (conversation is null)
        {
            var patch = entity.MapToPatch();
            await SavePatch2(patch);
            conversation = await _sql.ReadDsAs<Conversation>(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
            return conversation;
        }
        else
        {
            conversation.FormatChat = entity.FormatChat;
            conversation.Icon = entity.Icon;
            var patch = conversation.MapToPatch();
            await SavePatch2(patch);
            return conversation;
        }
    }

    public async Task<Dictionary<string, object>[]> GetMenu()
    {
        var query = @$"select * from [Feature] f where IsMenu = 1 and (exists (select Id from FeaturePolicy where FeatureId = f.Id and RoleId in ({RoleIds.CombineStrings()}) and CanRead = 1) or 'ADMIN' in ({RoleIds.CombineStrings()}))";
        var ds = await _sql.ReadDataSet(query, BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return ds[0];
    }

    public Feature GetFeature(string name)
    {
        var feature = GetFeatureFromJson(name, TenantCode) ?? throw new ApiException("Feature not found")
        {
            StatusCode = HttpStatusCode.NotFound
        };
        return feature;
    }

    private static Feature GetFeatureFromJson(string featureName, string t)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload", t, "features", featureName + ".json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Feature>(json);
    }

    public async Task<bool> HardDelete(PatchVM vm)
    {
        if (vm.ComId.IsNullOrWhiteSpace())
        {
            var sql = vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})");
            try
            {
                await _sql.RunSqlCmd(null, sql.Combine(";"));
            }
            catch
            {
                return false;
            }
        }
        else
        {
            var query = @$"select top 1 * from [Component] where Id = '{vm.ComId}'";
            var com = await _sql.ReadDsAs<Component>(query);
            var data = JsonConvert.DeserializeObject<SqlQuery>(com.Query);
            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { "EntityIds", vm.Delete.SelectMany(x=>x.Ids).CombineStrings() },
                { "NewId", vm.NewId }
            };
            if (!data.update.IsNullOrWhiteSpace())
            {
                var qr = Utils.FormatEntity(data.update, dictionary);
                var deletequery = qr + ";" + vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                try
                {
                    await _sql.RunSqlCmd(null, deletequery);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                var sql = vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})");
                try
                {
                    await _sql.RunSqlCmd(null, sql.Combine(";"));
                }
                catch
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void AddDefaultFields(List<PatchDetail> changes, List<PatchDetail> defaultFields)
    {
        foreach (var field in defaultFields)
        {
            var existingField = changes.FirstOrDefault(change => change.Field == field.Field);
            if (existingField != null)
            {
                existingField.Value = field.Value;
            }
            else
            {
                changes.Add(field);
            }
        }
    }

    public async Task<int> SavePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized to write on \"{vm.Table}\"")
        {
            StatusCode = HttpStatusCode.Unauthorized
        };
        var cmd = _sql.GetCreateOrUpdateCmd(vm);
        if (cmd.IsNullOrWhiteSpace()) return 0;
        var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
        if (result == 0) return result;
        return result;
    }

    public async Task<int> UpdatePatch(PatchVM vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn, vm.TenantCode, vm.Env);
        vm.CachedMetaConn ??= _sql.GetConnStrFromKey(vm.MetaConn, vm.TenantCode, vm.Env);
        var canWrite = await HasWritePermission(vm);
        if (!canWrite) throw new ApiException($"Unauthorized to write on \"{vm.Table}\"")
        {
            StatusCode = HttpStatusCode.Unauthorized
        };
        var cmd = _sql.GetUpdateCmd(vm);
        if (cmd.IsNullOrWhiteSpace()) return 0;
        var result = await _sql.RunSqlCmd(vm.CachedDataConn, cmd);
        if (result == 0) return result;
        return result;
    }

    private async Task<(bool, string, Dictionary<string, object>)> CheckDuplicate(PatchVM patch, bool Update = false)
    {
        var table = await _sql.ReadDsAs<TableName>($"Select * from TableName where [Name] = '{patch.Table}'");
        if (table is null)
        {
            return (false, null, null);
        }
        if (!table.Duplicate.IsNullOrWhiteSpace())
        {
            var field = table.Duplicate.Split(",");
            using SqlConnection connection = new(BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
            await connection.OpenAsync();
            try
            {
                using SqlCommand command = new SqlCommand();
                {
                    command.Connection = connection;
                    var wheres = field.Select(x => $"[{x}] = @{x.ToLower()} and [{x}] is not null and @{x.ToLower()} is not null").ToList();
                    if (Update)
                    {
                        wheres.Add($"[Id] != @id");
                    }
                    command.CommandText += $"Select Top 1 * from [{patch.Table}] where {wheres.Combine(" and ")}";
                    foreach (var item in field)
                    {
                        var val = patch.Changes.FirstOrDefault(x => x.Field == item);
                        if (val is not null)
                        {
                            command.Parameters.AddWithValue($"@{item.ToLower()}", val.Value is null ? DBNull.Value : val.Value);
                        }
                        else
                        {
                            command.Parameters.AddWithValue($"@{item.ToLower()}", DBNull.Value);
                        }
                    }
                    if (Update)
                    {
                        var val = patch.Changes.FirstOrDefault(x => x.Field == "Id");
                        if (val is not null)
                        {
                            command.Parameters.AddWithValue($"@id", val.Value is null ? DBNull.Value : val.Value);
                        }
                    }
                    var reader = await command.ExecuteReaderAsync();
                    Dictionary<string, object> lastRow = null;
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            lastRow = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                lastRow[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                        }
                        return (true, table.Description, lastRow);
                    }
                    else
                    {
                        return (false, table.Description, null);
                    }
                }
            }
            catch (Exception)
            {
                return (true, table.Description, null);
            }
        }
        else
        {
            return (false, null, null);
        }
    }

    public async Task<SqlResult> SavePatch2(PatchVM vm)
    {
        var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
        var tableColumns = (await GetTableColumns(vm.Table))[0];
        var filteredChanges = vm.Changes.Where(change => tableColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
        var selectIds = new List<DetailData>();
        var isSend = filteredChanges.Find(x => x.Field == "IsSend");
        var oldIsSend = isSend != null ? isSend.Value : "1";
        var receiverIds = filteredChanges.Find(x => x.Field == "ReceiverIds");
        if (isSend != null)
        {
            isSend.Value = "1";
        }
        if (id.StartsWith("-"))
        {
            var (dup, mess, currentEntity) = await CheckDuplicate(vm);
            if (dup)
            {
                return new SqlResult()
                {
                    updatedItem = null,
                    status = 409,
                    message = Utils.FormatEntity(mess, currentEntity)
                };
            }
            id = id.Substring(1);
            AddDefaultFields(filteredChanges, new List<PatchDetail>()
            {
                new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToISOFormat() },
                new PatchDetail { Field = "InsertedBy", Value = UserId },
                new PatchDetail { Field = "UpdatedDate", Value = null },
                new PatchDetail { Field = "UpdatedBy", Value = null },
                new PatchDetail { Field = "Active", Value = "1" }
            });
            using SqlConnection connection = new SqlConnection(BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
            await connection.OpenAsync();
            SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqlCommand command = new SqlCommand();
                command.Transaction = transaction;
                command.Connection = connection;
                var update = filteredChanges.Select(x => $"@{id.Replace("-", "") + x.Field.ToLower()}");
                var cells = filteredChanges.Select(x => x.Field).ToList();
                if (!vm.Delete.Nothing())
                {
                    command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                }
                command.CommandText += $"INSERT into [{vm.Table}]([{cells.Combine("],[")}]) values({update.Combine()})";
                foreach (var item in filteredChanges)
                {
                    if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                    {
                        item.Value = item.Value.Substring(1);
                    }
                    command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                }
                int index = 1;
                await command.ExecuteNonQueryAsync();
                command.Parameters.Clear();
                command.CommandText = string.Empty;
                if (!vm.Detail.Nothing())
                {
                    foreach (var detailArray in vm.Detail)
                    {
                        foreach (var detail in detailArray)
                        {
                            var tableDetailColumns = (await GetTableColumns(detail.Table))[0];
                            var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                            var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                            if (idDetail.StartsWith("-"))
                            {
                                AddDefaultFields(filteredDetailChanges, new List<PatchDetail>()
                                        {
                                            new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToISOFormat() },
                                            new PatchDetail { Field = "InsertedBy", Value = UserId },
                                            new PatchDetail { Field = "UpdatedDate", Value = null },
                                            new PatchDetail { Field = "UpdatedBy", Value = null },
                                            new PatchDetail { Field = "Active", Value = "1" }
                                        });
                                var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                var cellsDetails = filteredDetailChanges.Select(x => x.Field).ToList();
                                command.CommandText += $";INSERT into [{detail.Table}]([{cellsDetails.Combine("],[")}]) values({updateDetail.Combine()})";
                                foreach (var item in filteredDetailChanges)
                                {
                                    if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                    {
                                        item.Value = item.Value.Substring(1);
                                    }
                                    command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                }
                            }
                            else
                            {
                                AddDefaultFields(filteredDetailChanges, new List<PatchDetail>()
                                        {
                                            new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToISOFormat()},
                                            new PatchDetail { Field = "UpdatedBy", Value = UserId },
                                        });
                                filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                foreach (var item in filteredDetailChanges)
                                {
                                    if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                    {
                                        item.Value = item.Value.Substring(1);
                                    }
                                    command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                }
                            }
                            await command.ExecuteNonQueryAsync();
                            command.Parameters.Clear();
                            command.CommandText = string.Empty;
                        }
                        selectIds.Add(new DetailData()
                        {
                            Index = index,
                            Table = detailArray[0].Table,
                            ComId = detailArray[0].ComId,
                            Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                        });
                        index++;
                    }
                }
                await transaction.CommitAsync();
                await connection.CloseAsync();
                var childs = new List<string>();
                var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                foreach (var item in selectIds)
                {
                    sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
                }
                var entity = await _sql.ReadDataSet(sql);
                selectIds.ForEach(x =>
                {
                    x.Data = entity[x.Index];
                });
                await Notification(vm, id, filteredChanges, oldIsSend, receiverIds);
                return new SqlResult()
                {
                    updatedItem = entity[0],
                    Detail = selectIds,
                    status = 200,
                    message = "create successfull"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var entity = await _sql.ReadDataSet($"SELECT * FROM [{vm.Table}] where Id = '{id}'");
                return new SqlResult()
                {
                    updatedItem = entity[0],
                    status = 500,
                    message = ex.Message
                };
            }
        }
        else
        {
            var (dup, mess, currentEntity) = await CheckDuplicate(vm, true);
            if (dup)
            {
                var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                var entity = await _sql.ReadDataSet(sql);
                return new SqlResult()
                {
                    updatedItem = entity[0],
                    status = 409,
                    message = Utils.FormatEntity(mess, currentEntity)
                };
            }
            AddDefaultFields(filteredChanges, new List<PatchDetail>()
            {
                new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToISOFormat()},
                new PatchDetail { Field = "UpdatedBy", Value = UserId },
            });
            using (SqlConnection connection = new SqlConnection(BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics")))
            {
                await connection.OpenAsync();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        var updates = filteredChanges.Where(x => x.Field != "Id").ToList();
                        var update = updates.Select(x => $"[{x.Field}] = @{id.Replace("-", "") + x.Field.ToLower()}");
                        if (!vm.Delete.Nothing())
                        {
                            command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                        }
                        command.CommandText += $" UPDATE [{vm.Table}] SET {update.Combine()} WHERE Id = '{id}';";
                        foreach (var item in updates)
                        {
                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                            {
                                item.Value = item.Value.Substring(1);
                            }
                            command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                        }
                        var changes = updates.Where(x => !x.HistoryValue.IsNullOrWhiteSpace());
                        if (!changes.Nothing())
                        {
                            var history = changes.Select(x => x.HistoryValue).Combine("\n").Replace("'", "''");
                            if (!history.IsNullOrWhiteSpace())
                            {
                                command.CommandText += $" INSERT INTO [History](Id,TextContent,RecordId,TableName,Active,InsertedDate,InsertedBy) values('{Uuid7.Guid()}',N'{history}','{id}','{vm.Table}',1,'{DateTime.Now.ToISOFormat()}','{UserId}');";
                            }
                        }
                        int index = 1;
                        await command.ExecuteNonQueryAsync();
                        command.Parameters.Clear();
                        command.CommandText = string.Empty;
                        if (!vm.Detail.Nothing())
                        {
                            foreach (var detailArray in vm.Detail)
                            {
                                int j = 1;
                                foreach (var detail in detailArray)
                                {
                                    var tableDetailColumns = (await GetTableColumns(detail.Table))[0];
                                    var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                                    var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                                    if (idDetail.StartsWith("-"))
                                    {
                                        AddDefaultFields(filteredDetailChanges, new List<PatchDetail>()
                                        {
                                            new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToISOFormat() },
                                            new PatchDetail { Field = "InsertedBy", Value = UserId },
                                            new PatchDetail { Field = "UpdatedDate", Value = null },
                                            new PatchDetail { Field = "UpdatedBy", Value = null },
                                            new PatchDetail { Field = "Active", Value = "1" }
                                        });
                                        var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                        var insertDetail = filteredDetailChanges.Select(x => $"[{x.Field}]").ToList();
                                        command.CommandText += $";INSERT into [{detail.Table}]({insertDetail.Combine()}) values({updateDetail.Combine()})";
                                        foreach (var item in filteredDetailChanges)
                                        {
                                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                            {
                                                item.Value = item.Value.Substring(1);
                                            }
                                            command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                        }
                                    }
                                    else
                                    {
                                        AddDefaultFields(filteredDetailChanges, new List<PatchDetail>()
                                        {
                                            new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToISOFormat()},
                                            new PatchDetail { Field = "UpdatedBy", Value = UserId },
                                        });
                                        filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                        var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                        command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                        foreach (var item in filteredDetailChanges)
                                        {
                                            if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                            {
                                                item.Value = item.Value.Substring(1);
                                            }
                                            command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                        }
                                    }
                                    j++;
                                    await command.ExecuteNonQueryAsync();
                                    command.Parameters.Clear();
                                    command.CommandText = string.Empty;
                                }
                                selectIds.Add(new DetailData()
                                {
                                    Index = index,
                                    Table = detailArray[0].Table,
                                    ComId = detailArray[0].ComId,
                                    Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                                });
                                index++;
                            }
                        }
                        await transaction.CommitAsync();
                        await connection.CloseAsync();
                        var sql = $"SELECT * FROM [{vm.Table}] where Id = '{id}'";
                        foreach (var item in selectIds)
                        {
                            sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
                        }
                        var entity = await _sql.ReadDataSet(sql);
                        selectIds.ForEach(x =>
                        {
                            x.Data = entity[x.Index];
                        });
                        await Notification(vm, id, filteredChanges, oldIsSend, receiverIds);
                        return new SqlResult()
                        {
                            updatedItem = entity[0],
                            Detail = selectIds,
                            status = 200,
                            message = "update successfull"
                        };
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var entity = await _sql.ReadDataSet($"SELECT * FROM [{vm.Table}] where Id = '{id}'");
                    return new SqlResult()
                    {
                        updatedItem = entity[0],
                        status = 500,
                        message = ex.Message
                    };
                }
            }
        }
    }

    public async Task<SqlResult> SavePatchs2(List<PatchVM> vms)
    {
        var selectIds = new List<DetailData>();
        using SqlConnection connection = new SqlConnection(BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        await connection.OpenAsync();
        SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            using SqlCommand command = new SqlCommand();
            command.Transaction = transaction;
            command.Connection = connection;
            foreach (var vm in vms)
            {
                var id = vm.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                var tableColumns = (await GetTableColumns(vm.Table))[0];
                var filteredChanges = vm.Changes.Where(change => tableColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                var isSend = filteredChanges.Find(x => x.Field == "IsSend");
                var receiverIds = filteredChanges.Find(x => x.Field == "ReceiverIds");
                if (id.StartsWith("-"))
                {
                    id = id.Substring(1);
                    AddDefaultFields(filteredChanges, new List<PatchDetail>()
                            {
                                new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToISOFormat() },
                                new PatchDetail { Field = "InsertedBy", Value = UserId },
                                new PatchDetail { Field = "UpdatedDate", Value = null },
                                new PatchDetail { Field = "UpdatedBy", Value = null },
                                new PatchDetail { Field = "Active", Value = "1" }
                            });
                    var update = filteredChanges.Select(x => $"@{id.Replace("-", "") + x.Field.ToLower()}");
                    var cells = filteredChanges.Select(x => x.Field).ToList();
                    if (!vm.Delete.Nothing())
                    {
                        command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                    }
                    command.CommandText += $"INSERT into [{vm.Table}]([{cells.Combine("],[")}]) values({update.Combine()})";
                    foreach (var item in filteredChanges)
                    {
                        if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                        {
                            item.Value = item.Value.Substring(1);
                        }
                        command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                    }
                }
                else
                {
                    AddDefaultFields(filteredChanges, new List<PatchDetail>()
                            {
                                new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToISOFormat()},
                                new PatchDetail { Field = "UpdatedBy", Value = UserId },
                            });
                    var updates = filteredChanges.Where(x => x.Field != "Id").ToList();
                    var update = updates.Select(x => $"[{x.Field}] = @{id.Replace("-", "") + x.Field.ToLower()}");
                    if (!vm.Delete.Nothing())
                    {
                        command.CommandText += vm.Delete.Select(x => $"delete from [{x.Table}] where Id in ({x.Ids.CombineStrings()})").Combine(";");
                    }
                    command.CommandText += $" UPDATE [{vm.Table}] SET {update.Combine()} WHERE Id = '{id}';";
                    foreach (var item in updates)
                    {
                        if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                        {
                            item.Value = item.Value.Substring(1);
                        }
                        command.Parameters.AddWithValue($"@{id.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                    }
                }
                int index = 1;
                await command.ExecuteNonQueryAsync();
                command.Parameters.Clear();
                command.CommandText = string.Empty;
                if (!vm.Detail.Nothing())
                {
                    foreach (var detailArray in vm.Detail)
                    {
                        foreach (var detail in detailArray)
                        {
                            var tableDetailColumns = (await GetTableColumns(detail.Table))[0];
                            var idDetail = detail.Changes.FirstOrDefault(x => x.Field == "Id").Value;
                            var filteredDetailChanges = detail.Changes.Where(change => tableDetailColumns.SelectMany(x => x.Values).Contains(change.Field)).ToList();
                            if (idDetail.StartsWith("-"))
                            {
                                AddDefaultFields(filteredDetailChanges, new List<PatchDetail>()
                                        {
                                            new PatchDetail { Field = "InsertedDate", Value = DateTime.Now.ToISOFormat() },
                                            new PatchDetail { Field = "InsertedBy", Value = UserId },
                                            new PatchDetail { Field = "UpdatedDate", Value = null },
                                            new PatchDetail { Field = "UpdatedBy", Value = null },
                                            new PatchDetail { Field = "Active", Value = "1" }
                                        });
                                var updateDetail = filteredDetailChanges.Select(x => $"@{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                var cellsDetails = filteredDetailChanges.Select(x => x.Field).ToList();
                                command.CommandText += $";INSERT into [{detail.Table}]([{cellsDetails.Combine("],[")}]) values({updateDetail.Combine()})";
                                foreach (var item in filteredDetailChanges)
                                {
                                    if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                    {
                                        item.Value = item.Value.Substring(1);
                                    }
                                    command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                }
                            }
                            else
                            {
                                AddDefaultFields(filteredDetailChanges, new List<PatchDetail>()
                                        {
                                            new PatchDetail { Field = "UpdatedDate", Value = DateTime.Now.ToISOFormat()},
                                            new PatchDetail { Field = "UpdatedBy", Value = UserId },
                                        });
                                filteredDetailChanges = filteredDetailChanges.Where(x => x.Field != "Id").ToList();
                                var updateDetail = filteredDetailChanges.Select(x => $"[{x.Field}] = @{idDetail.Replace("-", "") + x.Field.ToLower()}");
                                command.CommandText += $";UPDATE [{detail.Table}] SET {updateDetail.Combine()} WHERE Id = '{idDetail}';";
                                foreach (var item in filteredDetailChanges)
                                {
                                    if ((item.Value != null && item.Value.Contains(id) || item.Field == "Id") && item.Value.StartsWith("-"))
                                    {
                                        item.Value = item.Value.Substring(1);
                                    }
                                    command.Parameters.AddWithValue($"@{idDetail.Replace("-", "") + item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                                }
                            }
                            await command.ExecuteNonQueryAsync();
                            command.Parameters.Clear();
                            command.CommandText = string.Empty;
                        }
                        selectIds.Add(new DetailData()
                        {
                            Index = index,
                            Table = detailArray[0].Table,
                            ComId = detailArray[0].ComId,
                            Ids = detailArray.SelectMany(x => x.Changes).Where(x => x.Field == "Id").Select(x => { return x.Value.StartsWith("-") ? x.Value.Substring(1) : x.Value; }).ToList(),
                        });
                        index++;
                    }
                }
            }
            await transaction.CommitAsync();
            await connection.CloseAsync();
            var sql = $"SELECT * FROM [{vms[0].Table}] where Id = '{vms[0].Changes.FirstOrDefault(x => x.Field == "Id").Value}'";
            foreach (var item in selectIds)
            {
                sql += $";SELECT * FROM [{item.Table}] where Id in ({item.Ids.CombineStrings()})";
            }
            var entity = await _sql.ReadDataSet(sql);
            selectIds.ForEach(x =>
            {
                x.Data = entity[x.Index];
            });
            return new SqlResult()
            {
                updatedItem = entity[0],
                Detail = selectIds,
                status = 200,
                message = "All patches processed successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            var entity = await _sql.ReadDataSet($"SELECT * FROM [{vms[0].Table}] where Id = '{vms[0].Changes.FirstOrDefault(x => x.Field == "Id").Value}'");
            return new SqlResult()
            {
                updatedItem = entity[0],
                status = 500,
                message = ex.Message
            };
        }
    }

    private async Task Notification(PatchVM vm, string id, List<PatchDetail> filteredChanges, string isSend, PatchDetail receiverIds)
    {
        var featureName = vm.Changes.FirstOrDefault(x => x.Field == "FeatureName");
        var voucherTypeId = vm.Changes.FirstOrDefault(x => x.Field == "VoucherTypeId");
        var titLe = vm.Changes.FirstOrDefault(x => x.Field == "FormatChat");
        var recordId = vm.Changes.FirstOrDefault(x => x.Field == "RecordId");
        var featureName2 = vm.Changes.FirstOrDefault(x => x.Field == "FeatureName2");
        var featureName3 = vm.Changes.FirstOrDefault(x => x.Field == "FeatureName3");
        if (isSend == "0" && receiverIds != null && receiverIds.Value != null)
        {
            var userString = receiverIds.Value.Split(",");
            var queryUser = @$"SELECT * FROM [User] where Id in ({userString.CombineStrings()})";
            var users = await _sql.ReadDsAsArr<User>(queryUser);
            var templateMessage = " has sent you an approval request.";
            var f2 = featureName2 is null ? null : featureName2.Value;
            var f3 = featureName3 is null ? null : featureName3.Value;
            if (vm.Table == "Conversation")
            {
                templateMessage = " has invited you to join the conversation.";
                f2 = "chat-editor";
                f3 = "chat-editor";
            }
            var task = users.Select(x => new TaskNotification()
            {
                Id = Uuid7.Guid().ToString(),
                VoucherTypeId = int.Parse(voucherTypeId.Value),
                EntityId = vm.Table,
                Avatar = Avatar,
                FeatureName = featureName is null ? null : featureName.Value,
                FeatureName2 = f2,
                FeatureName3 = f3,
                Title = titLe.Value ?? "",
                Title2 = FullName + templateMessage,
                Icon = "fal fa-smile",
                Description = titLe.Value ?? "",
                InsertedBy = UserId,
                RecordId = recordId is null ? id : recordId.Value,
                InsertedDate = DateTime.Now,
                Active = true,
                AssignedId = x.Id
            }).ToList();
            foreach (var item in task)
            {
                var patch = item.MapToPatch();
                await SavePatch(patch);
            }
        }
    }

    public async Task<Dictionary<string, object>[][]> GetTableColumns(string tableName)
    {
        string query = $@"
        SELECT c.COLUMN_NAME 
        FROM INFORMATION_SCHEMA.COLUMNS c
        JOIN sys.columns sc ON c.COLUMN_NAME = sc.name 
        AND OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME) = sc.object_id
        WHERE c.TABLE_NAME = '{tableName}' 
        AND sc.is_computed = 0";

        return await _sql.ReadDataSet(query);
    }

    private async Task<bool> HasWritePermission(PatchVM vm)
    {
        if (vm.ByPassPerm) return true;
        bool writePerm = false;
        var allRights = vm.ByPassPerm ? [] : await GetEntityPerm(vm.Table, recordId: null, vm.CachedMetaConn);
        var idField = vm.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
        var oldId = idField?.OldVal;
        if (oldId is null)
        {
            writePerm = allRights.Any(x => x.CanWriteAll);
        }
        else
        {
            var origin = @$"select t.* from [{vm.Table}] as t where t.Id = '{oldId}'";
            var ds = await _sql.ReadDataSet(origin, vm.CachedDataConn);
            var originRow = ds.Length > 0 && ds[0].Length > 0 ? ds[0][0] : null;
            var isOwner = Utils.IsOwner(originRow, UserId, RoleIds);
            writePerm = isOwner || allRights.Any(x => x.CanWriteAll);
        }
        return writePerm;
    }

    public async Task<int> SavePatches(PatchVM[] patches)
    {
        if (patches.Nothing()) throw new ArgumentException($"{nameof(patches)} is null or empty");
        patches = patches.Where(x => x.Id is not null).ToArray();
        patches[0].CachedDataConn ??= _sql.GetConnStrFromKey(patches[0].DataConn);
        patches[0].CachedMetaConn ??= _sql.GetConnStrFromKey(patches[0].MetaConn);
        var tables = patches.Select(x => x.Table);
        string rightQuery = @$"select * from [FeaturePolicy] 
            where Active = 1 and (CanWrite = 1 or CanWriteAll = 1) and EntityName in ({tables.CombineStrings()}) and RoleId in ({RoleIds.CombineStrings()})";
        var permissions = await _sql.ReadDsAsArr<FeaturePolicy>(rightQuery, patches[0].CachedMetaConn);
        permissions = permissions.DistinctBy(x => x.TableName).ToArray();
        var lackPerTables = patches.Select(x => x.Table).Except(permissions.Select(x => x.TableName)).ToArray();
        if (lackPerTables.Length > 0)
        {
            throw new ApiException($"All table must have write permission {lackPerTables.CombineStrings()}")
            {
                StatusCode = HttpStatusCode.Unauthorized
            };
        }
        var sql = patches.Select(_sql.GetCreateOrUpdateCmd).Where(x => x is not null).Combine(";\n");
        var result = await _sql.RunSqlCmd(patches[0].CachedDataConn, sql);
        return result;
    }

    public async Task<string[]> DeactivateAsync(SqlViewModel vm)
    {
        vm.CachedDataConn ??= _sql.GetConnStrFromKey(vm.DataConn ?? "default");
        var allRights = await GetEntityPerm(vm.Table, null, vm.CachedDataConn);
        var canDeactivateAll = allRights.Any(x => x.CanDeactivateAll);
        var canDeactivateSelf = allRights.Any(x => x.CanDeactivate);
        var query = $"select * from [{vm.Table}] where Id in ({vm.Id.CombineStrings()})";
        var ds = await _sql.ReadDataSet(query, vm.CachedDataConn);
        var rows = ds.Length > 0 ? ds[0] : null;
        if (rows.Nothing()) return null;
        var canDeactivateRows = rows.Where(x =>
        {
            return canDeactivateAll || canDeactivateSelf && Utils.IsOwner(x, UserId, RoleIds);
        }).Select(x => x.GetValueOrDefault(Utils.IdField)?.ToString()).ToArray();
        if (canDeactivateRows.Nothing()) return null;
        var deactivateCmd = $"update {vm.Table} set Active = 0 where Id in ({canDeactivateRows.CombineStrings()})";
        await _sql.RunSqlCmd(vm.CachedDataConn, deactivateCmd);
        return canDeactivateRows;
    }

    public async Task<SqlComResult> ComQuery(SqlViewModel vm)
    {
        var com = await GetComponent(vm) ?? throw new ApiException("Component not found or not public to the current user")
        {
            StatusCode = HttpStatusCode.NotFound
        };
        var anyInvalid = UserServiceHelpers.FobiddenTerms.Any(term =>
        {
            return vm.Select != null && term.IsMatch(vm.Select.ToLower())
            || vm.Table != null && term.IsMatch(vm.Table.ToLower())
            || vm.GroupBy != null && term.IsMatch(vm.GroupBy.ToLower())
            || vm.Having != null && term.IsMatch(vm.Having.ToLower())
            || vm.OrderBy != null && term.IsMatch(vm.OrderBy.ToLower())
            || vm.Paging != null && term.IsMatch(vm.Paging.ToLower());
        });
        if (anyInvalid)
        {
            throw new ArgumentException("Parameters must NOT contains sql keywords");
        }
        vm.JsScript = com.Query;
        return await RunjsWrap(vm);
    }

    public async Task<Dictionary<string, object>[][]> Report(SqlViewModel vm)
    {
        var com = await GetComponent(vm) ?? throw new ApiException("Component not found or not public to the current user")
        {
            StatusCode = HttpStatusCode.NotFound
        };
        Dictionary<string, object> dictionary = vm.Params.IsNullOrWhiteSpace() ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(vm.Params);
        if (dictionary.GetValueOrDefault("TokenUserId") != null)
        {
            dictionary["TokenUserId"] = UserId;
        }
        else
        {
            dictionary.Add("TokenUserId", UserId);
        }
        if (dictionary.GetValueOrDefault("TokenRoleNames") != null)
        {
            dictionary["TokenRoleNames"] = RoleNames.Combine() ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenRoleNames", RoleNames.Combine());
        }
        if (dictionary.GetValueOrDefault("TokenPartnerId") != null)
        {
            dictionary["TokenPartnerId"] = VendorId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenPartnerId", VendorId);
        }
        if (dictionary.GetValueOrDefault("TokenUserName") != null)
        {
            dictionary["TokenUserName"] = UserName;
        }
        else
        {
            dictionary.Add("TokenUserName", UserName);
        }
        if (dictionary.GetValueOrDefault("TokenGroupId") != null)
        {
            dictionary["TokenGroupId"] = GroupId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenGroupId", GroupId);
        }
        if (com.Query.Contains("ds.InsertedBy = '{TokenUserId}'") && RoleNames.Contains("BOD"))
        {
            com.Query = com.Query.Replace("ds.InsertedBy = '{TokenUserId}'", "ds.InsertedBy = '{TokenUserId}' or '{TokenRoleNames}' like '%BOD%'");
        }
        var query = Utils.FormatEntity(com.Query, dictionary);
        var ds1 = await _sql.ReadDataSet(query);
        return ds1;
    }

    public async Task<Dictionary<string, object>[][]> Sql(SqlViewModel vm)
    {
        var com = await GetComponent(vm) ?? throw new ApiException("Component not found or not public to the current user")
        {
            StatusCode = HttpStatusCode.NotFound
        };
        Dictionary<string, object> dictionary = vm.Params.IsNullOrWhiteSpace() ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(vm.Params);
        if (dictionary.GetValueOrDefault("TokenUserId") != null)
        {
            dictionary["TokenUserId"] = UserId;
        }
        else
        {
            dictionary.Add("TokenUserId", UserId);
        }
        if (dictionary.GetValueOrDefault("TokenRoleNames") != null)
        {
            dictionary["TokenRoleNames"] = RoleNames.Combine() ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenRoleNames", RoleNames.Combine());
        }
        if (dictionary.GetValueOrDefault("TokenPartnerId") != null)
        {
            dictionary["TokenPartnerId"] = VendorId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenPartnerId", VendorId);
        }
        if (dictionary.GetValueOrDefault("TokenGroupId") != null)
        {
            dictionary["TokenGroupId"] = GroupId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenGroupId", GroupId);
        }
        if (dictionary.GetValueOrDefault("TokenUserName") != null)
        {
            dictionary["TokenUserName"] = UserName;
        }
        else
        {
            dictionary.Add("TokenUserName", UserName);
        }
        if (com.Query.Contains("ds.InsertedBy = '{TokenUserId}'") && RoleNames.Contains("BOD"))
        {
            com.Query = com.Query.Replace("ds.InsertedBy = '{TokenUserId}'", "ds.InsertedBy = '{TokenUserId}' or '{TokenRoleNames}' like '%BOD%'");
        }
        var query = Utils.FormatEntity(com.Query, dictionary);
        var ds1 = await _sql.ReadDataSet(query);
        return ds1;
    }

    public async Task<CheckDeleteResult> CheckDelete(CheckDeleteItem item)
    {
        var query = @$"select top 1 * from [Component] where Id = '{item.ComId}'";
        var com = await _sql.ReadDsAs<Component>(query);
        var data = JsonConvert.DeserializeObject<SqlQuery>(com.Query);
        Dictionary<string, object> dictionary = item.Params.IsNullOrWhiteSpace() ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(item.Params);
        dictionary["EntityIds"] = item.EntityIds.CombineStrings();
        var qr = Utils.FormatEntity(data.delete, dictionary);
        var exists = await _sql.ReadDataSet(qr);
        return new CheckDeleteResult()
        {
            status = (exists[0] != null && exists[0].Length > 0) ? 500 : 200,
            message = dictionary["Message"] != null ? dictionary["Message"]?.ToString() : null
        };
    }

    private string CalcFinalQuery(SqlViewModel vm)
    {
        Dictionary<string, object> dictionary = vm.Params.IsNullOrWhiteSpace() ? new Dictionary<string, object>() : JsonConvert.DeserializeObject<Dictionary<string, object>>(vm.Params);
        if (dictionary.GetValueOrDefault("TokenUserId") != null)
        {
            dictionary["TokenUserId"] = UserId;
        }
        else
        {
            dictionary.Add("TokenUserId", UserId);
        }
        if (dictionary.GetValueOrDefault("TokenRoleNames") != null)
        {
            dictionary["TokenRoleNames"] = RoleNames.Combine() ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenRoleNames", RoleNames.Combine());
        }
        if (dictionary.GetValueOrDefault("TokenPartnerId") != null)
        {
            dictionary["TokenPartnerId"] = VendorId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenPartnerId", VendorId);
        }
        if (dictionary.GetValueOrDefault("TokenUserName") != null)
        {
            dictionary["TokenUserName"] = UserName;
        }
        else
        {
            dictionary.Add("TokenUserName", UserName);
        }
        if (dictionary.GetValueOrDefault("TokenGroupId") != null)
        {
            dictionary["TokenGroupId"] = GroupId ?? string.Empty;
        }
        else
        {
            dictionary.Add("TokenGroupId", GroupId);
        }
        if (vm.JsScript.Contains("ds.InsertedBy = '{TokenUserId}'") && RoleNames.Contains("BOD"))
        {
            vm.JsScript = vm.JsScript.Replace("ds.InsertedBy = '{TokenUserId}'", "ds.InsertedBy = '{TokenUserId}' or '{TokenRoleNames}' like '%BOD%'");
        }
        vm.OrderBy = Utils.FormatEntity(vm.OrderBy, dictionary);
        var data = JsonConvert.DeserializeObject<SqlQuery>(vm.JsScript);
        dictionary["Skip"] = vm.Skip;
        dictionary["Top"] = vm.Top;
        data.total = Utils.FormatEntity(data.total, dictionary);
        data.sql = Utils.FormatEntity(data.sql, dictionary);
        var sqlSelect = data.sql;
        var sqlTotal = data.total;
        if (!vm.Where.IsNullOrWhiteSpace())
        {
            if (sqlSelect.ToLower().Contains("where"))
            {
                sqlSelect += $" AND ({vm.Where})";
                sqlTotal += $" AND ({vm.Where})";
            }
            else
            {
                sqlSelect += $" WHERE {vm.Where}";
                sqlTotal += $" WHERE {vm.Where}";
            }
        }
        if (!vm.OrderBy.IsNullOrWhiteSpace() && !data.sql.Contains("order by"))
        {
            sqlSelect += $" ORDER BY {vm.OrderBy}";
        }
        if (vm.Skip != null && !data.sql.Contains("OFFSET"))
        {
            sqlSelect += $" OFFSET {vm.Skip} ROWS";
        }
        if (vm.Top != null && !data.sql.Contains("FETCH NEXT"))
        {
            sqlSelect += $" FETCH NEXT {vm.Top} ROWS ONLY";
        }
        if (vm.Count)
        {
            sqlSelect += $"; {sqlTotal}";
        }
        return sqlSelect;
    }

    private async Task<Component> GetComponent(SqlViewModel vm)
    {
        Component com = null;
        var comKey = nameof(Component) + vm.ComId;
        var cached = _cache.GetString(comKey);
        if (cached != null)
        {
            try
            {
                com = JsonConvert.DeserializeObject<Component>(cached);
            }
            catch
            {

            }
        }
        if (com is null)
        {
            var f = GetFeatureFromJson(vm.Feature, TenantCode);
            com = await FindComponentById(vm, f.Components, f);
            if (com is null) return null;
            await SetStringAsync(comKey, JsonConvert.SerializeObject(com), Utils.CacheTTL);
        }
        return com;
    }

    private async Task<Component> FindComponentById(SqlViewModel vm, IEnumerable<Component> components, Feature feature)
    {
        Component com = null;
        foreach (var c in components)
        {
            if (c.Id == vm.ComId)
            {
                // If component is not private or user is an admin, it's a match.
                if (!c.IsPrivate || RoleIds.Contains("ADMIN"))
                {
                    com = c;
                    break;
                }

                var permissions = feature.FeaturePolicies.Where(x => RoleIds.Contains(x.RoleId) && x.CanRead).ToArray();

                if (permissions.Length > 0)
                {
                    com = c;
                }

                break;
            }
            else if (c.Components != null && c.Components.Count > 0)
            {
                com = await FindComponentById(vm, c.Components, feature);
                if (com != null) break;
            }
        }

        return com;
    }

    private async Task<FeaturePolicy[]> GetEntityPerm(string entityName, string recordId, string connStr,
        Expression<Func<FeaturePolicy, bool>> pre = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        if (RoleIds.Nothing()) return [];
        var permissionName = ((pre?.Body as MemberExpression)?.Member as PropertyInfo)?.Name;
        var key = entityName + "_" + (permissionName ?? "AllRights");
        var permissionByComCache = await GetStringAsync(key);
        FeaturePolicy[] permissions;
        if (!permissionByComCache.IsNullOrWhiteSpace())
        {
            permissions = JsonConvert.DeserializeObject<FeaturePolicy[]>(permissionByComCache);
        }
        else
        {
            var q = @$"select * from [FeaturePolicy] 
            where Active = 1 and EntityName = '{entityName}'
            and (RecordId = '{recordId}' or '{recordId}' = '') and RoleId in ({RoleIds.CombineStrings()})";
            if (pre != null) q += $" and {permissionName} = 1";
            permissions = await _sql.ReadDsAsArr<FeaturePolicy>(q, connStr);
            await SetStringAsync(key, JsonConvert.SerializeObject(permissions), Utils.CacheTTL);
        }

        return permissions;
    }

    public async Task<Dictionary<string, object>> GetMessageActive()
    {
        var users = await _sql.ReadDataSet($"SELECT COUNT(Id) as Total FROM [ConversationRead] WHERE UserId = '{UserId}' and [Read] = 0", BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        return users[0][0];
    }

    public class UserActiveVM
    {
        public string UserId { get; set; }
        public string Ip { get; set; }
    }


    private async Task<SqlComResult> RunjsWrap(SqlViewModel vm)
    {
        var actQuery = CalcFinalQuery(vm);
        var dataParam = new List<WhereParamVM>();
        if (!vm.WhereParams.IsNullOrWhiteSpace())
        {
            dataParam = JsonConvert.DeserializeObject<List<WhereParamVM>>(vm.WhereParams);
        }
        var ds = await _sql.ReadDataSet(actQuery, null, false, dataParam);
        return new SqlComResult()
        {
            count = ds.Length > 1 && ds[1].Length > 0 ? Convert.ToInt32(ds[1][0]["total"]) : null,
            value = ds[0]
        };
    }

    public string ConvertHtmlToPlainText(string htmlContent)
    {
        // Remove HTML tags using regular expression
        string plainText = Regex.Replace(htmlContent, @"<[^>]+>|&nbsp;", "").Trim();

        // Decode HTML entities using regular expression
        plainText = Regex.Replace(plainText, @"&(amp|quot|gt|lt|nbsp);", m => DecodeEntity(m.Groups[1].Value));

        return plainText;
    }

    public string DecodeEntity(string entity) =>
        entity switch
        {
            "amp" => "&",
            "quot" => "\"",
            "gt" => ">",
            "lt" => "<",
            "nbsp" => " ",
            _ => entity,
        };

    public async Task<string> PostImageAsync(IWebHostEnvironment host,
            string name = "Captured", bool reup = false)
    {
        var image = await Utils.ReadRequestBody(_ctx.HttpContext.Request, leaveOpen: false);
        var fileName = $"{Path.GetFileNameWithoutExtension(name)}{Path.GetExtension(name)}";
        var path = GetUploadPath(fileName, host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(image));
        return GetHttpPath(path, host.WebRootPath);
    }

    public async Task<string> PostFileAsync(IFormFile file, bool reup = false)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}{Uuid7.Guid()}{Path.GetExtension(file.FileName)}";
        var path = GetUploadPath(fileName, _host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;
        using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        stream.Close();
        return GetHttpPath(path, _host.WebRootPath);
    }

    public string GetHttpPath(string path, string webRootPath)
    {
        return _ctx.HttpContext.Request.Scheme + "://" + _ctx.HttpContext.Request.Host.Value + path.Replace(webRootPath, string.Empty).Replace("\\", "/");
    }

    public async Task<bool> ImportCsv(List<IFormFile> files, string table, string comId, string connKey)
    {
        if (comId.IsNullOrWhiteSpace() || table.IsNullOrWhiteSpace())
        {
            throw new ApiException("ComId or table cannot be null") { StatusCode = HttpStatusCode.BadRequest };
        }
        if (files.Nothing())
        {
            throw new ApiException("No file uploaded") { StatusCode = HttpStatusCode.BadRequest };
        }
        var com = await GetComponent(new SqlViewModel
        {
            ComId = comId,
            DataConn = connKey
        });
        var connStr = _sql.GetConnStrFromKey(BgExt.GetConnectionString(iServiceProvider, _configuration, "logistics"));
        var tableRights = await GetEntityPerm(table, recordId: null, connStr);
        if (!tableRights.Any(x => x.CanWriteAll))
            throw new UnauthorizedAccessException("Cannot import data due to lack of permission");

        var file = files.FirstOrDefault();
        var path = GetUploadPath(file.FileName, _host.WebRootPath);
        EnsureDirectoryExist(path);
        path = IncreaseFileName(path);
        using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        stream.Close();

        var patches = await ParseCsvFile(path, table);

        using SqlConnection connection = new(connStr);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            using SqlCommand command = new();
            command.Transaction = transaction;
            command.Connection = connection;
            patches.SelectForEach((x, index) =>
            {
                ImportItem(x, command, index);
            });
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return true;
    }

    private void ImportItem(PatchVM vm, SqlCommand command, int index)
    {
        var idField = vm.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
        var oldId = idField?.OldVal;
        var valueFields = vm.Changes.Where(x => !_sql.SystemFields.Contains(x.Field.ToLower())).ToList();
        var update = valueFields.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
        var now = DateTime.Now.ToString(DateTimeExt.DateFormat);

        var fields = valueFields.Combine(x => $"[{x.Field}]");
        var fieldParams = valueFields.Combine(x => $"@{x.Field}{index}");
        command.CommandText += @$"insert into [{vm.Table}] ([Id], [TenantCode], [Active], [InsertedBy], [InsertedDate], {fields}) 
                        values ('{idField.Value}', '{TenantCode}', 1, '{UserId}', '{now}', {fieldParams});";
        foreach (var item in valueFields)
        {
            command.Parameters.AddWithValue($"@{item.Field}{index}", item.Value is null ? DBNull.Value : item.Value);
        }
    }

    public string GetUploadPath(string fileName, string webRootPath)
    {
        return Path.Combine(webRootPath, "upload", TenantCode, "file", $"U{UserId}", fileName);
    }

    public static string IncreaseFileName(string path)
    {
        var uploadedPath = path;
        var index = 0;
        while (File.Exists(path))
        {
            var noExtension = Path.GetFileNameWithoutExtension(uploadedPath);
            var dir = Path.GetDirectoryName(uploadedPath);
            index++;
            path = Path.Combine(dir, noExtension + "_" + index + Path.GetExtension(uploadedPath));
        }

        return path;
    }

    public static void EnsureDirectoryExist(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    protected async Task<List<PatchVM>> ParseCsvFile(string path, string table)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        var tempPath = IncreaseFileName(path);
        using var streamReader = new StreamReader(path);
        using var streamWriter = new StreamWriter(tempPath);
        string currentLine;
        var lineCount = 0;
        string[] headers = null;
        var patches = new List<PatchVM>();
        while ((currentLine = await streamReader.ReadLineAsync()) != null)
        {
            if (lineCount == 0 || currentLine.IsNullOrWhiteSpace())
            {
                lineCount++;
                var firstLine = ParseCsvLine(currentLine, lineCount)
                    ?? throw new ApiException("Header must be the first line of csv")
                    {
                        StatusCode = HttpStatusCode.BadRequest
                    };
                headers = firstLine.Select(x => x.Field).ToArray();
                continue;
            }
            var updatedLine = ParseCsvLine(currentLine, lineCount);
            updatedLine.SelectForEach((x, index) => x.Field = headers[index]);
            patches.Add(new PatchVM { Table = table, Changes = updatedLine });
            lineCount++;
        }
        return patches;
    }

    private static List<PatchDetail> ParseCsvLine(string currentLine, int lineCount, string[] headers = null)
    {
        List<PatchDetail> res;
        string propVal = null;
        try
        {
            var parser = new CsvParser(currentLine);
            var values = parser.ToArray();
            res = new List<PatchDetail>(values.Length);
            for (int index = 0; index < values.Length && index < values.Length; index++)
            {
                res.Add(new PatchDetail
                {
                    Field = headers is null ? values[index] : headers[index],
                    Value = values[index]
                });
            }
        }
        catch
        {
            throw new ApiException($"Struture of line {lineCount}, value {propVal} is not valid")
            {
                StatusCode = HttpStatusCode.BadRequest
            };
        }

        return res;
    }

    public ValueTask<bool> DeleteFile(string path)
    {
        var absolutePath = Path.Combine(_host.WebRootPath, path);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
        return new ValueTask<bool>(true);
    }

    public Task<Dictionary<string, object>[][]> ReadDs
        (string query, string connStr, bool shouldMapToConnStr = false)
        => _sql.ReadDataSet(query, connStr, shouldMapToConnStr);

    public Task<string> GetStringAsync(string key) => _cache.GetStringAsync(key?.ToUpper());
    public Task SetStringAsync(string key, string val, DistributedCacheEntryOptions options) => _cache.SetStringAsync(key?.ToUpper(), val, options);
}