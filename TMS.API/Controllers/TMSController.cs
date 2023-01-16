using Core.Enums;
using Core.ViewModels;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;
using ApprovalStatusEnum = Core.Enums.ApprovalStatusEnum;
using AuthVerEnum = Core.Enums.AuthVerEnum;
using EntityActionEnum = Core.Enums.EntityActionEnum;
using FileIO = System.IO.File;
using HttpMethod = Core.Enums.HttpMethod;
using ResponseApproveEnum = Core.Enums.ResponseApproveEnum;
using SystemHttpMethod = System.Net.Http.HttpMethod;
using Microsoft.AspNetCore.Hosting;
using Aspose.Cells;
using System.Drawing;
using System.Data.SqlClient;
using System.Data;

namespace TMS.API.Controllers
{
    [Authorize]
    public class TMSController<T> : GenericController<T> where T : class
    {
        protected readonly TMSContext db;
        protected readonly ILogger<TMSController<T>> _logger;
        protected HttpClient _client;
        protected string PathField = "Path";
        protected string ParentIdField = "ParentId";
        protected string ChildrenField = "InverseParent";
        private string _address;

        public TMSController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
            db = context;
            _logger = (ILogger<TMSController<T>>)httpContextAccessor.HttpContext.RequestServices.GetService(typeof(ILogger<TMSController<T>>));
            _config = _httpContext.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            _address = _config["ServiceAddress"];
        }

        [HttpPost("api/[Controller]", Order = 0)]
        public async Task<ActionResult<T>> CreateAndWebhookAsync([FromBody] T entity)
        {
            var res = await CreateAsync(entity);
            WebhookActionWrapper(EntityActionEnum.Create, entity);
            return res;
        }

        [HttpPut("api/[Controller]", Order = 0)]
        public async Task<ActionResult<T>> UpdateAndWebhookAsync([FromBody] T entity, string reasonOfChange = "")
        {
            var res = await UpdateAsync(entity, reasonOfChange);
            WebhookActionWrapper(EntityActionEnum.Update, entity);
            return res;
        }

        [HttpPost("api/[Controller]/Delete", Order = 0)]
        public async Task<ActionResult<bool>> DeactivateAndWebhookAsync([FromRoute] int id)
        {
            var res = await DeactivateAsync(new List<int> { id });
            WebhookActionWrapper(EntityActionEnum.Deactivate, id);
            return res;
        }

        [HttpPost("api/[Controller]/HardDelete", Order = 0)]
        public async Task<ActionResult<bool>> HardDeleteAndWebhookAsync([FromBody] List<int> ids)
        {
            var res = await HardDeleteAsync(ids);
            WebhookActionWrapper(EntityActionEnum.Delete, ids);
            return res;
        }

        [HttpPost("api/[Controller]/Cmd")]
        public virtual async Task<object> ExecuteSqlCmd([FromBody] SqlViewModel sqlCmd)
        {
            if (sqlCmd is null)
            {
                return BadRequest("Cmd arg is null");
            }
            var sv = await db.Services.FirstOrDefaultAsync(x => x.ComId == sqlCmd.CmdId && x.CmdType == sqlCmd.CmdType);
            if (sv is null)
            {
                throw new ApiException($"Service {sqlCmd.CmdType} not found");
            }

            try
            {
                var client = new HttpClient();
                var res = await client.PostAsJsonAsync(sv.Address ?? _address, new { path = sv.Path ?? "./" + sv.CmdType + ".js", entity = sqlCmd.Entity });
                var result = await res.Content.ReadAsStringAsync();
                var sqlQuery = JsonConvert.DeserializeObject<SqlQueryResult>(result);
                if (sqlQuery != null && sqlQuery.Query.HasAnyChar())
                {
                    var dataSet = await ReportDataSet(sqlQuery.Query, sqlQuery.System);
                    return dataSet;
                }
                return sqlQuery.Result ?? sqlQuery.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError("Node service ran into error at {0} {1} {2}", DateTimeOffset.Now, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public static Task<string> RunNodeFunction(string process, string args, string fileName, bool shouldGen = false)
        {
            var tcs = new TaskCompletionSource<string>();
            Task.Run(async () =>
            {
                if (shouldGen)
                {
                    EnsureDirectoryExist(fileName);
                    await FileIO.WriteAllTextAsync(fileName, args);
                }
                var p = new Process();
                p.StartInfo.FileName = process;
                p.StartInfo.Arguments = string.Concat(fileName, " ", args);
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = false;
                p.OutputDataReceived += (a, b) => tcs.TrySetResult(b.Data);
                p.ErrorDataReceived += (object a, DataReceivedEventArgs b) => tcs.TrySetException(new Exception(b.Data));
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                await p.WaitForExitAsync();
            });
            return tcs.Task;

        }

        public void WebhookActionWrapper<K>(EntityActionEnum action, K entity)
        {
            var serviceProvider = _httpContext.HttpContext.RequestServices.GetService(typeof(IServiceProvider)) as IServiceProvider;
            var configuration = _httpContext.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var connStr = configuration.GetConnectionString("Default");
            var db = Startup.GetTMSContext(connStr);
            var thead = new Thread(async () =>
            {
                try
                {
                    _client = new HttpClient();
                    await WebhookAction(db, action, entity);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Webhook error at {0}: {1} {2}", DateTimeOffset.Now, ex.Message, ex.StackTrace);
                }
            });
            thead.Start();
        }

        private async Task WebhookAction<K>(TMSContext db, EntityActionEnum action, K entity)
        {
            var entityId = _entitySvc.GetEntity(typeof(K).Name).Id;
            var actions = await db.Webhook
                .Where(x => x.Active && x.EntityId == entityId)
                .Where(x => x.EventTypeId == (int)action)
                .ToListAsync();
            if (actions.Nothing())
            {
                return;
            }
            var tokenLoaded = await Task.WhenAll(actions.Select(action => CreateRequest(action, entity)));
            if (tokenLoaded.Any(x => x == true))
            {
                await db.SaveChangesAsync();
            }
        }

        private async Task<bool> CreateRequest<K>(Webhook action, K entity)
        {
            entity.ClearReferences();
            JwtSecurityToken jwtSecurity = null;
            bool shouldLoadToken = false;
            var actionMethod = Enum.Parse<HttpMethod>(action.Method.ToUpper());
            var method = actionMethod switch
            {
                HttpMethod.POST => SystemHttpMethod.Post,
                HttpMethod.PUT => SystemHttpMethod.Put,
                HttpMethod.DELETE => SystemHttpMethod.Delete,
                HttpMethod.GET => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
            var uri = action.SubUrl;
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = method,
                Content = new StringContent(JsonConvert.SerializeObject(entity), Encoding.UTF8, Utils.ApplicationJson),
            };
            if (action.AuthVersionId == (int)AuthVerEnum.ApiKey)
            {
                request.Headers.Add(action.ApiKeyHeader, action.ApiKey);
            }
            else if (action.AuthVersionId == (int)AuthVerEnum.Simple)
            {
                request.Headers.Add(action.UsernameKey, action.SubUsername);
                request.Headers.Add(action.PasswordKey, action.SubPassword);
            }
            else if (action.AuthVersionId == (int)AuthVerEnum.OAuth2)
            {
                if (action.SavedToken.HasAnyChar())
                {
                    jwtSecurity = TryReadSavedToken(action, jwtSecurity);
                }
                if (jwtSecurity is not null && jwtSecurity.ValidTo > DateTime.Now.AddMinutes(1))
                {
                    request.Headers.Add(action.ApiKeyHeader, action.TokenPrefix + action.SavedToken);
                }
                else
                {
                    var token = await GetNewToken(action);
                    if (token.IsNullOrEmpty())
                    {
                        return false;
                    }
                    action.SavedToken = token;
                    shouldLoadToken = true;
                    request.Headers.Add(action.ApiKeyHeader, action.TokenPrefix + token);
                }
                request.RequestUri = new Uri($"{action.SubUrl}/?{action.ApiKeyHeader}={action.ApiKey}");
            }
            var res = await _client.SendAsync(request);
            return shouldLoadToken;
        }

        private async Task<string> GetNewToken(Webhook action)
        {
            try
            {
                var json = $"{{\"{action.UsernameKey}\": \"{action.SubUsername}\", \"{action.PasswordKey}\": \"{action.SubPassword}\", \"{action.ApiKeyHeader}\": \"{action.ApiKey}\"}}";
                var tokenRequest = await _client.PostAsync(action.LoginUrl, new StringContent(json, Encoding.UTF8, Utils.ApplicationJson));
                var tokenStr = await tokenRequest.Content.ReadAsStringAsync();
                JObject tokenJson = JObject.Parse(tokenStr);
                var token = action.AccessTokenField.HasAnyChar() ? tokenJson.SelectToken(action.AccessTokenField)?.ToString() : tokenStr;
                return token;
            }
            catch
            {
                return null;
            }
        }

        private static JwtSecurityToken TryReadSavedToken(Webhook action, JwtSecurityToken jwtSecurity)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                jwtSecurity = tokenHandler.ReadJwtToken(action.SavedToken);
            }
            catch
            {
            }

            return jwtSecurity;
        }

        [HttpPost("api/listener/[Controller]")]
        public virtual Task<ActionResult<T>> CreateListenerAsync([FromBody] T entity)
        {
            return CreateAsync(entity);
        }

        [HttpPut("api/listener/[Controller]")]
        public virtual Task<ActionResult<T>> UpdateListenerAsync([FromBody] T entity)
        {
            return UpdateAsync(entity);
        }

        [HttpDelete("api/listener/[Controller]/{id}")]
        public virtual Task<ActionResult<bool>> HardDeleteListenerAsync([FromRoute] int id)
        {
            return HardDeleteAsync(id);
        }

        protected async Task<bool> HasSystemRole()
        {
            return await db.Role.AnyAsync(x => AllRoleIds.Contains(x.Id) && x.RoleName.Contains("system"));
        }


        [HttpPost("api/[Controller]/RequestApprove")]
        public virtual async Task<ActionResult<bool>> RequestApprove([FromBody] T entity)
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
            await NotifyApprovalLevel(entity, approvalConfig, 1);
            await Approving(entity);
            return true;
        }

        protected virtual async Task Approving(T entity)
        {
            var oldEntity = await db.Set<T>().FindAsync((int)entity.GetComplexPropValue(IdField));
            oldEntity.CopyPropFrom(entity);
            await db.SaveChangesAsync();
        }

        protected virtual Task<List<TaskNotification>> InitTaskNotification(T record, IEnumerable<User> users)
        {
            return Task.FromResult(Enumerable.Empty<TaskNotification>().ToList());
        }

        protected async Task<List<User>> GetApprovalUsers(T entity, ApprovalConfig approvalConfig)
        {
            if (entity is null)
            {
                return null;
            }
            if (approvalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var users = await (
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                join role in db.Role on userRole.RoleId equals role.Id
                where userRole.RoleId == approvalConfig.RoleId
                select user
            ).ToListAsync();
            return users;
        }

        protected virtual async Task<List<ApprovalConfig>> GetApprovalConfig(T entity)
        {
            var entityType = _entitySvc.GetEntity(typeof(T).Name);
            return await db.ApprovalConfig.AsNoTracking().OrderBy(x => x.Level)
                .Where(x => x.Active && x.EntityId == entityType.Id).ToListAsync();
        }

        [HttpPost("api/[Controller]/Approve/")]
        public virtual async Task<ActionResult<bool>> Approve([FromBody] T entity, string reasonOfChange = "")
        {
            entity.ClearReferences();
            var id = (int)entity.GetPropValue(IdField);
            var approvalConfig = await GetApprovalConfig(entity);
            if (approvalConfig.Nothing())
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var maxLevel = approvalConfig.Max(x => x.Level);
            var entityType = _entitySvc.GetEntity(typeof(T).Name);
            var approvements = await db.Approvement
                .Where(x => x.EntityId == entityType.Id && x.RecordId == id && x.Active)
                .OrderByDescending(x => x.CurrentLevel).ToListAsync();
            if (approvements.Any(x => x.CurrentLevel == maxLevel && x.Approved))
            {
                await EndApproval(entity, reasonOfChange);
                await db.SaveChangesAsync();
                return true;
            }
            var currentLevel = approvements.FirstOrDefault()?.CurrentLevel ?? 1;
            if (currentLevel <= 1)
            {
                currentLevel = 1;
            }
            else
            {
                currentLevel++;
            }
            var currentConfig = approvalConfig.FirstOrDefault(x => x.Level == currentLevel);
            var hasRoleLevel = currentConfig.IsSupervisor || currentConfig.RoleId.HasValue && AllRoleIds.Contains(currentConfig.RoleId.Value);
            if (!hasRoleLevel)
            {
                throw new ApiException(ResponseApproveEnum.NonRole.GetEnumDescription());
            }
            var approvalUserIds = await GetApprovalUsers(entity, currentConfig);
            SetApproved(entity, currentConfig);
            if (approvalConfig.Where(x => x.Level == currentLevel + 1).Nothing())
            {
                await EndApproval(entity, reasonOfChange);
            }
            else
            {
                await NotifyApprovalLevel(entity, approvalConfig, currentLevel + 1);
            }
            db.Set<T>().Update(entity);
            await db.SaveChangesAsync();
            entity.ClearReferences();
            return true;
        }

        protected virtual async Task EndApproval(T entity, string reasonOfChange)
        {
            entity.SetPropValue("StatusId", (int)ApprovalStatusEnum.Approved);
            var user = await db.User.FindAsync((int)entity.GetPropValue(InsertedByField));
            var tasks = await InitTaskNotification(entity, new List<User>() { user });
            tasks.ForEach(x =>
            {
                x.Title ??= "Thông báo đã duyệt";
                x.Description ??= "Duyệt thành công yêu cầu";
            });
            await _taskService.NotifyAsync(tasks);
            tasks.ForEach(SetAuditInfo);
            db.AddRange(tasks);
            db.Set<T>().Update(entity);
            await db.SaveChangesAsync();
        }

        [HttpPost("api/[Controller]/Reject/")]
        public virtual async Task<bool> Reject([FromBody] T entity, string reasonOfChange)
        {
            entity.ClearReferences();
            var id = (int)entity.GetPropValue(nameof(Role.Id));
            var insertedBy = (int)entity.GetPropValue(nameof(Role.InsertedBy));
            var type = typeof(T);
            var entityType = _entitySvc.GetEntity(typeof(T).Name);
            entity.SetPropValue("StatusId", (int)ApprovalStatusEnum.Rejected);
            db.Set<T>().Update(entity);
            var approved = await db.Approvement.Where(x => x.EntityId == entityType.Id && x.RecordId == id).ToListAsync();
            var taskNotification = new TaskNotification
            {
                Title = $"Trả về {entityType.Name}",
                Description = $"{entityType.Name} đã trả về. Lý do trả về: {reasonOfChange}",
                EntityId = entityType.Id,
                Attachment = "fas fa-cart-plus",
                AssignedId = insertedBy,
                StatusId = (int)TaskStateEnum.UnreadStatus,
                RemindBefore = 540,
                Deadline = DateTime.Now,
            };
            await _taskService.NotifyAsync(new List<TaskNotification>() { taskNotification });
            SetAuditInfo(taskNotification);
            db.Add(taskNotification);
            await db.SaveChangesAsync();
            return true;
        }

        private async Task NotifyApprovalLevel(T entity, List<ApprovalConfig> approvalConfig, int currentLevel)
        {
            var matchApprovalConfig = approvalConfig.FirstOrDefault(x => x.Level == currentLevel);
            if (matchApprovalConfig is null)
            {
                throw new ApiException("Quy trình duyệt chưa được cấu hình");
            }
            var id = (int)entity.GetPropValue(IdField);
            if (matchApprovalConfig is null)
            {
                return;
            }
            var listUser = await GetApprovalUsers(entity, matchApprovalConfig);
            if (listUser.HasElement())
            {
                var tasks = await InitTaskNotification(entity, listUser);
                await _taskService.NotifyAsync(tasks);
                tasks.ForEach(SetAuditInfo);
                db.AddRange(tasks);
            }
        }

        protected virtual void SetApproved(T entity, ApprovalConfig currentConfig)
        {
            var _entityEnum = _entitySvc.GetEntity(typeof(T).Name);
            var id = (int)entity.GetPropValue(IdField);
            var currentLevel = currentConfig.Level;
            var approval = new Approvement
            {
                Approved = true,
                CurrentLevel = currentLevel,
                NextLevel = currentLevel + 1,
                EntityId = _entityEnum.Id,
                RecordId = id,
                LevelName = currentConfig.Description,
                StatusId = (int)ApprovalStatusEnum.Approved,
                UserApproveId = UserId,
                ApprovedBy = UserId,
                ApprovedDate = DateTime.Now,
            };
            SetAuditInfo(approval);
            db.Add(approval);
        }

        public async Task<ActionResult<T>> UpdateTreeNodeAsync([FromBody] T entity, string reasonOfChange = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var res = await base.UpdateAsync(entity, reasonOfChange);
            var parentId = (int?)entity.GetPropValue(ParentIdField);
            var id = (int)entity.GetPropValue(IdField);
            var path = entity.GetPropValue(PathField) as string;
            var entityChildren = entity.GetPropValue(ChildrenField) as ICollection<T>;
            if (parentId != null)
            {
                var parentEntity = await db.Set<T>().FindAsync(parentId);
                var pathParent = parentEntity.GetPropValue(nameof(MasterData.Path));
                entity.SetPropValue(PathField, @$"\{pathParent}\{parentId}\".Replace(@"\\", @"\"));
            }
            else
            {
                entity.SetPropValue(PathField, null);
            }
            SetLevel(entity);
            var folderPath = "\\" + id + "\\";
            var query = $"select * from [{typeof(T).Name}] where charindex('{folderPath}', [Path]) >= 1 or Id = {id} or {ParentIdField} = {id} order by [Path] desc";
            var allNodes = await db.Set<T>().FromSqlRaw(query).ToListAsync();
            if (allNodes.Nothing())
            {
                return res;
            }
            var nodeMap = BuildTreeNode(allNodes);
            var directChildren = nodeMap.Values.Where(x => (int?)x.GetPropValue(ParentIdField) == id);
            SetChildrenPath(directChildren, nodeMap, new HashSet<T>() { entity });
            await db.SaveChangesAsync();
            return res;
        }

        private Dictionary<int, T> BuildTreeNode(List<T> allNode)
        {
            var nodeMap = allNode.Where(x => x != null).DistinctBy(x => (int)x.GetPropValue(IdField)).ToDictionary(x => (int)x.GetPropValue(IdField));
            nodeMap.Values.ForEach(x =>
            {
                var parentId = (int?)x.GetPropValue(ParentIdField);
                var id = (int)x.GetPropValue(IdField);
                var path = (string)x.GetPropValue(PathField);
                if (parentId is null || parentId is null || !nodeMap.ContainsKey(parentId.Value))
                {
                    return;
                }
                var parent = nodeMap[parentId.Value];
                x.SetPropValue(ParentIdField, parent.GetPropValue(IdField));
            });
            return nodeMap;
        }

        protected void SetChildrenPath(IEnumerable<T> directChildren, Dictionary<int, T> nodeMap, HashSet<T> visited)
        {
            if (directChildren.Nothing())
            {
                return;
            }
            directChildren.ForEach(child =>
            {
                var id = (int)child.GetPropValue(IdField);
                var parentId = child.GetPropValue(ParentIdField) as int?;
                if (parentId is not null && nodeMap.ContainsKey(parentId.Value))
                {
                    var parent = nodeMap[parentId.Value];
                    var parentPath = parent.GetPropValue(PathField) as string;
                    child.SetPropValue(PathField, @$"\{parentPath}\{parentId}\".Replace(@"\\", @"\"));
                }
                SetLevel(child);
                SetChildrenPath(nodeMap.Values.Where(x => (int?)x.GetPropValue(ParentIdField) == id), nodeMap, visited);
            });
        }

        protected void SetLevel(T entity)
        {
            var path = entity.GetPropValue(PathField) as string;
            entity.SetPropValue("Level", path.IsNullOrWhiteSpace() ? 0 : path.Split(@"\").Where(x => !x.IsNullOrWhiteSpace()).Count());
        }

        [HttpGet("api/[Controller]/ExportExcel")]
        public async Task<string> ExportExcel([FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config, [FromQuery] int componentId, [FromQuery] string sql, [FromQuery] string where, [FromQuery] bool custom, [FromQuery] int featureId, [FromQuery] string order)
        {
            var component = await db.Component.FindAsync(componentId);
            var userSetting = await db.UserSetting.FirstOrDefaultAsync(x => x.Name == $"{(custom ? "Export" : "ListView")}-" + componentId && x.UserId == UserId);
            var gridPolicySys = JsonConvert.DeserializeObject<List<GridPolicy>>(userSetting.Value);
            var gridPolicy = await db.GridPolicy.Where(x => x.EntityId == component.ReferenceId && x.FeatureId == featureId && x.Active && !x.Hidden).ToListAsync();
            var specificComponent = gridPolicy.Any(x => x.ComponentId == component.Id);
            if (specificComponent)
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == component.Id).ToList();
            }
            else
            {
                gridPolicy = gridPolicy.Where(x => x.ComponentId == null).ToList();
            }
            if (gridPolicySys != null)
            {
                var gridPolicys = new List<GridPolicy>();
                var userSettings = gridPolicySys.ToDictionary(x => x.Id);
                gridPolicy.ForEach(x =>
                {
                    var current = userSettings.GetValueOrDefault(x.Id);
                    if (current != null)
                    {
                        x.IsExport = current.IsExport;
                        x.Order = current.Order;
                    }
                });
            }
            gridPolicy = gridPolicy.Where(x => x.ComponentType != "Button" && !x.ShortDesc.IsNullOrWhiteSpace() && ((custom && x.IsExport) || !custom)).OrderBy(x => x.Order).ToList().ToList();
            var reportQuery = string.Empty;
            var pros = typeof(T).GetProperties().Where(x => x.CanRead && x.PropertyType.IsSimple()).Select(x => x.Name).ToList();
            var selects = gridPolicy.Where(x => x.ComponentType == "Dropdown" && pros.Contains(x.FieldName)).ToList().Select(x =>
            {
                var format = x.FormatCell.Split("}")[0].Replace("{", "");
                var objField = x.FieldName.Substring(0, x.FieldName.Length - 2);
                return $"[{objField}].[{format}] as [{objField}]";
            });
            var joins = gridPolicy.Where(x => x.ComponentType == "Dropdown").ToList().Select(x =>
            {
                var format = x.FormatCell.Split("}")[0].Replace("{", "");
                var objField = x.FieldName.Substring(0, x.FieldName.Length - 2);
                return $"left join [{x.RefName}] as [{objField}] on [{objField}].Id = [{component.RefName}].{x.FieldName}";
            }).Distinct().ToList();
            var select1s = gridPolicy.Where(x => x.ComponentType != "Dropdown").Distinct().ToList().Select(x => $"[{component.RefName}].[{x.FieldName}]").Distinct().ToList();
            if (!sql.IsNullOrWhiteSpace())
            {
                reportQuery = $@"select {select1s.Combine()}{(selects.Nothing() ? "" : $",{selects.Combine()}")}
                                  from ({sql})  as [{component.RefName}]
                                  {joins.Combine(" ")}
                                  where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            else
            {
                reportQuery = $@"select {select1s.Combine()}{(selects.Nothing() ? "" : $",{selects.Combine()}")}
                                  from [{component.RefName}] as [{component.RefName}]
                                  {joins.Combine(" ")}
                                  where 1=1 {(where.IsNullOrWhiteSpace() ? $"" : $" and {where}")}";
            }
            var connectionStr = Startup.GetConnectionString(serviceProvider, config, "Default");
            using var con = new SqlConnection(connectionStr);
            var sqlCmd = new SqlCommand(reportQuery, con)
            {
                CommandType = CommandType.Text
            };
            con.Open();
            var tables = new List<List<Dictionary<string, object>>>();
            try
            {
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
            }
            catch (Exception e)
            {
                throw new ApiException(e.Message);
            }
            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Name = component.RefName;
            var i = 1;
            worksheet.Cells.Rows[0][0].PutValue("STT");
            Style style = worksheet.Cells.Rows[0][0].GetStyle();
            style.Pattern = BackgroundType.Solid;
            style.ForegroundColor = Color.LightGreen;
            style.Font.Name = "Times New Roman";
            style.SetBorder(BorderType.BottomBorder, CellBorderType.Thin, Color.Black);
            style.SetBorder(BorderType.LeftBorder, CellBorderType.Thin, Color.Black);
            style.SetBorder(BorderType.RightBorder, CellBorderType.Thin, Color.Black);
            style.SetBorder(BorderType.TopBorder, CellBorderType.Thin, Color.Black);
            worksheet.Cells.Rows[0][0].SetStyle(style);
            foreach (var item in gridPolicy)
            {
                worksheet.Cells.Rows[0][i].PutValue(item.ShortDesc);
                worksheet.Cells.Rows[0][i].SetStyle(style);
                i++;
            }
            var x = 1;
            foreach (var item in tables[0])
            {
                var y = 1;
                worksheet.Cells.Rows[x][0].PutValue(x);
                Style styletd = worksheet.Cells.Rows[x][0].GetStyle();
                styletd.SetBorder(BorderType.BottomBorder, CellBorderType.Thin, Color.Black);
                styletd.SetBorder(BorderType.LeftBorder, CellBorderType.Thin, Color.Black);
                styletd.Font.Name = "Times New Roman";
                styletd.SetBorder(BorderType.RightBorder, CellBorderType.Thin, Color.Black);
                styletd.SetBorder(BorderType.TopBorder, CellBorderType.Thin, Color.Black);
                worksheet.Cells.Rows[x][0].SetStyle(styletd);
                foreach (var itemDetail in gridPolicy)
                {
                    var vl = item.GetValueOrDefault(itemDetail.FieldName);
                    worksheet.Cells.Rows[x][y].SetStyle(styletd);
                    switch (itemDetail.ComponentType)
                    {
                        case "Input":
                            vl = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cells.Rows[x][y].PutValue(vl);
                            break;
                        case "Textarea":
                            vl = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cells.Rows[x][y].PutValue(vl);
                            break;
                        case "Label":
                            vl = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cells.Rows[x][y].PutValue(vl);
                            break;
                        case "Datepicker":
                            worksheet.Cells.Rows[x][y].PutValue(vl);
                            Style datepicker = worksheet.Cells.Rows[x][y].GetStyle();
                            datepicker.Custom = "dd/MM/yyyy";
                            worksheet.Cells.Rows[x][y].SetStyle(datepicker);
                            break;
                        case "Number":
                            worksheet.Cells.Rows[x][y].PutValue(vl);
                            Style number = worksheet.Cells.Rows[x][y].GetStyle();
                            number.Number = 3;
                            worksheet.Cells.Rows[x][y].SetStyle(number);
                            break;
                        case "Dropdown":
                            var objField = itemDetail.FieldName.Substring(0, itemDetail.FieldName.Length - 2);
                            vl = item.GetValueOrDefault(objField);
                            vl = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cells.Rows[x][y].PutValue(vl);
                            break;
                        default:
                            break;
                    }
                    y++;
                }
                x++;
            }
            var url = $"{component.RefName}{DateTime.Now:ddMMyyyyhhmm}.xlsx";
            workbook.Save($"wwwroot\\excel\\Download\\{url}", new OoxmlSaveOptions(SaveFormat.Xlsx));
            return url;
        }
    }
}
