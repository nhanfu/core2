using ClosedXML.Excel;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.ViewModels;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Tenray.Topaz;
using Tenray.Topaz.API;
using HttpStatusCode = Core.Enums.HttpStatusCode;

namespace Core.Services
{
    public class UserService
    {
        private const int MAX_LOGIN = 5;
        public readonly IHttpContextAccessor Context;
        private readonly CoreContext db;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;

        public string UserId { get; set; }
        public string BranchId { get; set; }
        public List<string> CenterIds { get; set; }
        public string VendorId { get; set; }
        public string Env { get; set; }
        public string TenantCode { get; set; }
        public List<string> AllRoleIds { get; set; }
        public List<string> RoleIds { get; set; }

        static readonly Regex[] _fobiddenTerm = new Regex[] { new Regex(@"delete\s"), new Regex(@"create\s"), new Regex(@"insert\s"),
                new Regex(@"update\s"), new Regex(@"select\s"), new Regex(@"from\s"),new Regex(@"where\s"),
                new Regex(@"group by\s"), new Regex(@"having\s"), new Regex(@"order by\s") };
        static readonly string[] _systemFields = new string[] { IdField, nameof(TenantCode), nameof(User.Active), nameof(User.InsertedBy), nameof(User.InsertedDate) }
            .Select(x => x.ToLower()).ToArray();

        public UserService(IHttpContextAccessor httpContextAccessor, CoreContext db, IConfiguration configuration, IDistributedCache cache)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache;
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            Context = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            if (Context?.HttpContext is null)
            {
                UserId = Utils.SystemId;
                VendorId = Utils.SelfVendorId;
                return;
            }
            var claims = Context.HttpContext.User.Claims;
            BranchId = claims.FirstOrDefault(x => x.Type == nameof(BranchId))?.Value;
            UserId = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            AllRoleIds = claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).Where(x => x != null).ToList();
            CenterIds = claims.Where(x => x.Type == nameof(CenterIds)).Select(x => x.Value).Where(x => x != null).ToList();
            RoleIds = claims.Where(x => x.Type == ClaimTypes.Actor).Select(x => x.Value).Where(x => x != null).ToList();
            VendorId = claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value;
            TenantCode = claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimaryGroupSid)?.Value.ToUpper();
            Env = claims.FirstOrDefault(x => x.Type == ClaimTypes.Spn)?.Value.ToUpper();
        }

        public string GenerateRandomToken(int? maxLength = 32)
        {
            var builder = new StringBuilder();
            var random = new Random();
            char ch;
            for (int i = 0; i < maxLength; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public const string IdField = "Id";
        private const string PassPhrase = "d7a9220a-6949-44c8-a702-789587e536cb";

        public void SetAuditInfo<K>(K entity, string userId = null) where K : class
        {
            ReflectionExt.ProcessObjectRecursive(entity, (obj) =>
            {
                string id = obj.GetPropValue(IdField)?.ToString();

                if (id is null)
                {
                    obj.SetPropValue(IdField, Guid.NewGuid().ToString());
                    obj.SetPropValue(nameof(User.TenantCode), TenantCode);
                    obj.SetPropValue(nameof(User.InsertedBy), userId ?? UserId);
                    obj.SetPropValue(nameof(User.InsertedDate), DateTimeOffset.Now);
                    obj.SetPropValue(nameof(User.UpdatedBy), userId ?? UserId);
                    obj.SetPropValue(nameof(User.UpdatedDate), DateTimeOffset.Now);
                    obj.SetPropValue(nameof(User.Active), true);
                }
                else
                {
                    obj.SetPropValue(nameof(User.UpdatedBy), userId ?? UserId);
                    obj.SetPropValue(nameof(User.UpdatedDate), DateTimeOffset.Now);
                    obj.SetPropValue(nameof(User.TenantCode), TenantCode);
                }
            });
        }

        public string GetRemoteIpAddress(HttpContext context)
        {
            return context.Request.Headers.ContainsKey("X-Forwarded-For")
                ? context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim()
                : context.Connection.RemoteIpAddress.ToString();
        }

        public async Task<Token> SignInAsync(LoginVM login, bool skipHash = false)
        {
            if (login.CompanyName.HasAnyChar())
            {
                login.CompanyName = login.CompanyName.Trim();
            }
            var matchedUser = await GetUserByLogin(login) ?? throw new ApiException($"Sai mật khẩu hoặc tên đăng nhập.<br /> Vui lòng đăng nhập lại!") { StatusCode = HttpStatusCode.BadRequest };
            if (matchedUser.LoginFailedCount >= MAX_LOGIN && matchedUser.LastFailedLogin < DateTimeOffset.Now.AddMinutes(5))
            {
                throw new ApiException($"Tài khoản {login.UserName} đã bị khóa trong 5 phút!") { StatusCode = HttpStatusCode.Conflict };
            }
            var hashedPassword = GetHash(UserUtils.sHA256, login.Password + matchedUser.Salt);
            var matchPassword = skipHash ? matchedUser.Password == login.Password : matchedUser.Password == hashedPassword;
            if (!matchPassword)
            {
                if (!login.RecoveryToken.IsNullOrWhiteSpace() && login.RecoveryToken == matchedUser.Recover)
                {
                    matchedUser.Password = hashedPassword;
                }
                else
                {
                    matchedUser.LastFailedLogin = DateTimeOffset.Now;
                    matchedUser.LoginFailedCount = matchedUser.LoginFailedCount.HasValue ? matchedUser.LoginFailedCount + 1 : 1;
                }
            }
            else
            {
                matchedUser.LastLogin = DateTimeOffset.Now;
                matchedUser.LoginFailedCount = 0;
            }
            if (!skipHash)
            {
                await db.SaveChangesAsync();
            }
            if (!matchPassword)
            {
                throw new ApiException($"Wrong username or password. Please try again!") { StatusCode = HttpStatusCode.BadRequest };
            }
            return await GetUserToken(matchedUser, tenant: login.CompanyName, env: login.Env, null, login.AutoSignIn);
        }

        private async Task<User> GetUserByLogin(LoginVM login)
        {
            var matchedUser =
                from user in db.User.Include(user => user.Vendor).Include(user => user.UserRole).ThenInclude(userRole => userRole.Role)
                where user.UserName == login.UserName && user.Active && user.Vendor.Code == login.CompanyName
                select user;
            return await matchedUser.FirstOrDefaultAsync();
        }

        protected virtual async Task<Token> GetUserToken(User user, string tenant, string env, string refreshToken = null, bool autoSigin = false)
        {
            if (user is null)
            {
                return null;
            }
            var roleIds = user.UserRole.Select(x => x.RoleId).Distinct().ToList();
            var allRoles = await GetDecendantPath<Role>(roleIds, true);
            var signinDate = DateTimeOffset.Now;
            var jit = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.GroupSid, user.VendorId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(nameof(User.BranchId), user.BranchId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Birthdate, user.DoB.ToString()),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.FullName?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Iat, signinDate.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jit),
                new Claim(ClaimTypes.PrimaryGroupSid, tenant),
                new Claim(ClaimTypes.Spn, env),
            };
            claims.AddRange(allRoles.Select(x => new Claim(ClaimTypes.Role, x.ToString())));
            claims.AddRange(roleIds.Select(x => new Claim(ClaimTypes.Actor, x.ToString())));
            var newLogin = refreshToken is null;
            refreshToken ??= GenerateRandomToken();
            var (token, exp) = AccessToken(claims);
            var res = JsonToken(user, user.UserRole.ToList(), tenant, allRoles.ToList(), refreshToken, token, exp, signinDate);
            if (!newLogin || !autoSigin)
            {
                return res;
            }
            var userLogin = new UserLogin
            {
                Id = jit,
                TenantCode = tenant,
                UserId = user.Id,
                IpAddress = GetRemoteIpAddress(Context.HttpContext),
                RefreshToken = refreshToken,
                ExpiredDate = res.RefreshTokenExp,
                SignInDate = signinDate,
            };
            db.Add(userLogin);
            await db.SaveChangesAsync();
            return res;
        }

        private Task<List<string>> GetDecendantPath<T>(string rootId, bool includeRoot) where T : class => GetDecendantPath<T>(new List<string> { rootId }, includeRoot);
        public async Task<List<string>> GetDecendantPath<T>(List<string> rootIds, bool includeRoot) where T : class
        {
            var str_roleIds = string.Join(",", rootIds);
            var tableName = typeof(T).Name;
            var decendants = rootIds.Nothing() ? new List<T>() : await db.Set<T>().FromSqlRaw(
                $"select * from [{tableName}] d " +
                $"cross apply (select [data] from dbo.SplitStringToTable(d.Path, '\\') where [data] in ({str_roleIds})) as decendants"
            ).ToListAsync();
            List<string> result = null;
            if (includeRoot)
            {
                result = decendants.Select(x => (string)x.GetPropValue(IdField)).Union(rootIds).ToList();
            }
            else
            {
                result = decendants.Select(x => (string)x.GetPropValue(IdField)).Except(rootIds).ToList();
            }
            return result;
        }

        public (JwtSecurityToken, DateTimeOffset) AccessToken(IEnumerable<Claim> claims, DateTimeOffset? expire = null)
        {
            var exp = expire ?? DateTimeOffset.Now.AddDays(1);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Tokens:Issuer"],
                _configuration["Tokens:Issuer"],
                claims,
                expires: exp.DateTime,
                signingCredentials: creds);
            return (token, exp);
        }

        private Token JsonToken(User user,
            List<UserRole> roles, string tanent, List<string> allRoleIds, string refreshToken,
            JwtSecurityToken token, DateTimeOffset exp, DateTimeOffset signinDate)
        {
            var vendor = new Vendor();
            vendor.CopyPropFrom(user.Vendor);
            return new Token
            {
                UserId = user.Id,
                CostCenterId = user.BranchId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                UserName = user.UserName,
                Address = user.Address,
                Avatar = user.Avatar,
                PhoneNumber = user.PhoneNumber,
                Ssn = user.Ssn,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                AccessTokenExp = exp,
                RefreshTokenExp = DateTimeOffset.Now.AddYears(1),
                RefreshToken = refreshToken,
                RoleIds = roles.Select(x => x.RoleId).ToList(),
                AllRoleIds = allRoleIds,
                RoleNames = roles.Select(x => x.Role.RoleName).ToList(),
                Vendor = vendor,
                TenantCode = tanent,
                SysName = _configuration["SysName"],
                SigninDate = signinDate
            };
        }

        public async Task<Token> RefreshAsync(RefreshVM token)
        {
            var principal = UserUtils.GetPrincipalFromAccessToken(token.AccessToken, _configuration);
            var issuedAt = principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Iat)?.Value.TryParseDateTime();
            var userId = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var tenant = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimaryGroupSid)?.Value;
            var env = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Spn)?.Value;
            if (userId is null)
            {
                throw new InvalidOperationException($"{nameof(userId)} is null");
            }
            var ipAddress = GetRemoteIpAddress(Context.HttpContext);
            var userLogin = await db.UserLogin
                .OrderByDescending(x => x.SignInDate)
                .FirstOrDefaultAsync(x => x.UserId == userId
                    && x.RefreshToken == token.RefreshToken
                    && x.ExpiredDate > DateTimeOffset.Now);

            if (userLogin == null)
            {
                Console.WriteLine("Refresh token timeout.");
                return null;
            }
            var updatedUser = await db.User.Include(user => user.Vendor)
                .Include(x => x.UserRole).ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);
            return await GetUserToken(user: updatedUser, tenant: tenant, env: env, refreshToken: token.RefreshToken);
        }

        public async Task<Role> GetRole(string roleName, RoleSelection? selection = RoleSelection.TopFirst)
        {
            var roleQuery = db.Role
                .Where(x => AllRoleIds.Contains(x.Id) && x.RoleName.Contains(roleName));
            if (selection == RoleSelection.TopFirst)
            {
                roleQuery = roleQuery.OrderBy(x => x.Path.Length);
            }
            else
            {
                roleQuery = roleQuery.OrderByDescending(x => x.Path.Length);
            }
            return await roleQuery.FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByRole(string roleName, RoleSelection? selection = RoleSelection.TopFirst)
        {
            var role = await GetRole(roleName, selection);
            var userQuery =
                from user in db.User
                join userRole in db.UserRole on user.Id equals userRole.UserId
                where user.Active && userRole.Active && role.Id == userRole.RoleId
                select user;
            return await userQuery.FirstOrDefaultAsync();
        }

        public async Task<string> EncryptQuery(string query, string env, string system, string tenantCode, bool encryptQuery = false, string connKey = null)
        {
            if (query.IsNullOrEmpty()) return null;
            var hash = GetHash(UserUtils.sHA256, query);
            if (connKey is null)
            {
                var tenantConnKey = await db.TenantEnv
                    .Where(x => x.TenantCode == tenantCode && x.Env == env)
                    .Select(x => x.ConnKey).FirstOrDefaultAsync()
                    ?? throw new ApiException("Tenant config not found");
                connKey = tenantConnKey;
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Hash, hash),
                new Claim(ClaimTypes.System, connKey, PassPhrase),
            };
            if (encryptQuery)
            {
                claims.Add(new Claim("Query", query));
            }
            var accessToken = AccessToken(claims, DateTimeOffset.Now.AddDays(1)).Item1;
            return new JwtSecurityTokenHandler().WriteToken(accessToken);
        }

        public async Task<(string Conn, string Query)> DecryptQuery(string signed, string plainQuery = null)
        {
            var token = UserUtils.GetPrincipalFromAccessToken(signed, _configuration);
            var hash = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Hash)?.Value;
            var query = token.Claims.FirstOrDefault(x => x.Type == "Query")?.Value ?? plainQuery;
            if (plainQuery.HasAnyChar())
            {
                var originalHash = GetHash(UserUtils.sHA256, query);
                if (hash != originalHash)
                {
                    throw new ApiException("Permission denied!") { StatusCode = HttpStatusCode.Unauthorized };
                }
            }
            var connKey = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.System)?.Value;
            var connString = await GetConnStrFromKey(connKey);
            return (connString, query);
        }

        public async Task<string> GetConnStrFromKey(string connKey)
        {
            var key = $"{TenantCode}_{connKey}_{Env}";
            var conStr = await _cache.GetStringAsync(key);
            if (conStr != null) return conStr;
            var tenantEnvTask = db.TenantEnv
                .Where(x => x.TenantCode == TenantCode && x.ConnKey == connKey && x.Env == Env);
            var tenantEnv = await tenantEnvTask.FirstOrDefaultAsync();
            await _cache.SetStringAsync(key, tenantEnv.ConnStr, Utils.CacheTTL);
            return tenantEnv.ConnStr;
        }

        public async Task<SqlQueryResult> ExecJs(string entityParam, string query)
        {
            SqlQueryResult result = new();
            var engine = new TopazEngine();
            engine.SetValue("JSON", new JSONObject());
            engine.AddType<HttpClient>("HttpClient");
            engine.AddNamespace("System");
            var claims = Context.HttpContext.User?.Claims;
            if (claims != null)
            {
                var map = new { UserId, RoleIds, AllRoleIds, TenantCode, Env, CenterIds, BranchId, VendorId };
                engine.SetValue("claims", JsonConvert.SerializeObject(map));
            }
            engine.SetValue("args", entityParam);

            await engine.ExecuteScriptAsync(query);
            var res = engine.GetValue("result") as string;
            try
            {
                result = JsonConvert.DeserializeObject<SqlQueryResult>(res);
            }
            catch (Exception)
            {
                result.Query = res;
            }
            return result;
        }

        public bool HasSqlComment(string sql)
        {
            TSql110Parser parser = new(true);
            var fragments = parser.Parse(new StringReader(sql), out var errors);

            return fragments.ScriptTokenStream
                .Any(x => x.TokenType == TSqlTokenType.MultilineComment || x.TokenType == TSqlTokenType.SingleLineComment);
        }

        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ReadDataSet(string query, string connStr, bool exportQuery = true)
        {
            var anyComment = HasSqlComment(query);
            if (anyComment) throw new ApiException("Comment is NOT allowed");
            var connectionStr = connStr;
            using var con = new SqlConnection(connectionStr);
            var sqlCmd = new SqlCommand(query, con)
            {
                CommandType = CommandType.Text
            };
            con.Open();
            var tables = new List<List<Dictionary<string, object>>>();
            using var reader = await sqlCmd.ExecuteReaderAsync();
            do
            {
                var table = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    table.Add(Read(reader));
                }
                tables.Add(table);
            } while (await reader.NextResultAsync());
#if DEBUG
            if (exportQuery)
            {
                tables.Add(new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "query",  query }
                    }
                });
            }
#endif
            return tables;
        }

        protected static Dictionary<string, object> Read(IDataRecord reader)
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader[i];
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
            }
            return row;
        }

        public static string RemoveWhiteSpace(string val) => val.Replace(" ", "");

        public async Task<bool> Patch(PatchUpdate vm)
        {
            if (vm == null || vm.Table.IsNullOrWhiteSpace() || vm.Changes.Nothing())
            {
                throw new ApiException("Table name and change details can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
            }
            vm.Table = RemoveWhiteSpace(vm.Table);
            vm.Changes = vm.Changes.Where(x =>
            {
                if (x.Field.IsNullOrWhiteSpace()) throw new ApiException($"Field name can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
                x.Field = RemoveWhiteSpace(x.Field);
                return !x.JustHistory;
            }).ToList();
            bool writePerm;
            var idField = vm.Changes.FirstOrDefault(x => x.Field == Utils.IdField);
            var oldId = idField?.OldVal;
            var allRights = await GetEntityPerm(vm.Table, recordId: null);
            var connStr = await GetConnStrFromKey(vm.ConnKey);
            if (oldId is null)
            {
                writePerm = allRights.Any(x => x.CanAdd || x.CanWriteAll);
            }
            else
            {
                var origin = @$"select t.* from [{vm.Table}] as t where t.Id = '{oldId}'";
                var ds = await ReadDataSet(origin, connStr);
                var originRow = ds.Count() > 1 ? ds.ElementAt(0).FirstOrDefault() : null;
                var isOwner = Utils.IsOwner(originRow, UserId, RoleIds);
                writePerm = isOwner || allRights.Any(x => x.CanWriteAll);
            }
            if (!writePerm) throw new ApiException("Access denied!") { StatusCode = HttpStatusCode.Unauthorized };
            using SqlConnection connection = new(connStr);
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqlCommand command = new();
                command.Transaction = transaction;
                command.Connection = connection;

                var valueFields = vm.Changes.Where(x => !_systemFields.Contains(x.Field.ToLower())).ToList();
                var update = valueFields.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                var now = DateTimeOffset.Now.ToString(DateTimeExt.DateFormat);
                if (oldId is not null)
                {
                    command.CommandText = @$"update [{vm.Table}] set {update.Combine()}, 
                            TenantCode = '{TenantCode}', UpdatedBy = '{UserId}', UpdatedDate = '{now}' where Id = '{oldId}';";
                }
                else
                {
                    var fields = valueFields.Combine(x => $"[{x.Field}]");
                    var fieldParams = valueFields.Combine(x => $"@{x.Field}");
                    command.CommandText = @$"insert into [{vm.Table}] ([Id], [TenantCode], [Active], [InsertedBy], [InsertedDate], {fields}) 
                        values ('{idField.Value}', '{TenantCode}', 1, '{UserId}', '{now}', {fieldParams});";
                }
                foreach (var item in valueFields)
                {
                    command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                }
                var anyComment = HasSqlComment(command.CommandText);
                if (anyComment) throw new ApiException("Comment is NOT allowed");
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                if (!vm.QueueName.IsNullOrWhiteSpace())
                {
                    var mqEvent = new MQEvent
                    {
                        Id = Id.NewGuid(),
                        QueueName = vm.QueueName,
                        Message = vm
                    };
                    BackgroundJob.Enqueue<TaskService>(x => x.SendMessageToSubscribers(mqEvent, mqEvent.QueueName));
                }
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        internal async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ReadDataSetWrapper(SqlViewModel vm)
        {
            var com = await GetComponent(vm.ComId);
            string decryptedConnStr = null;
            string decryptedQuery = null;
            if (com is null)
            {
                var (connStr, query) = await DecryptQuery(vm.Component.Signed);
                decryptedConnStr = connStr;
                decryptedQuery = query;
            }
            var anyInvalid = _fobiddenTerm.Any(term =>
            {
                return vm.Select != null && term.IsMatch(vm.Select.ToLower())
                || vm.Entity != null && term.IsMatch(vm.Entity.ToLower())
                || vm.Where != null && term.IsMatch(vm.Where.ToLower())
                || vm.GroupBy != null && term.IsMatch(vm.GroupBy.ToLower())
                || vm.Having != null && term.IsMatch(vm.Having.ToLower())
                || vm.OrderBy != null && term.IsMatch(vm.OrderBy.ToLower())
                || vm.Paging != null && term.IsMatch(vm.Paging.ToLower());
            });
            if (anyInvalid)
            {
                throw new ArgumentException("Parameters must NOT contains sql keywords");
            }
            var jsRes = await ExecJs(vm.Entity, decryptedQuery ?? com.Query);
            return await GetResultFromQuery(vm, decryptedConnStr ?? await GetConnStrFromKey(com.ConnKey), jsRes);
        }

        private async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> GetResultFromQuery(SqlViewModel vm, string decryptedConnStr, SqlQueryResult jsRes)
        {
            var select = vm.Select.HasAnyChar() ? $"select {vm.Select}" : string.Empty;
            var where = vm.Where.HasAnyChar() ? $"where {vm.Where}" : string.Empty;
            var groupBy = vm.GroupBy.HasAnyChar() ? $"group by {vm.GroupBy}" : string.Empty;
            var having = vm.Having.HasAnyChar() ? $"having {vm.Having}" : string.Empty;
            var orderBy = vm.OrderBy.HasAnyChar() ? $"order by {vm.OrderBy}" : string.Empty;
            var xQuery = vm.SkipXQuery ? string.Empty : jsRes.XQuery;
            var countQuery = vm.Count ?
                $@"select count(*) as total from (
                {jsRes.Query}) as ds 
                {where}
                {groupBy}
                {having};" : string.Empty;
            var finalQuery = @$"{select}
                from ({jsRes.Query}) as ds
                {where}
                {groupBy}
                {having}
                {orderBy}
                {vm.Paging};
                {countQuery}
                {xQuery}";
            return await ReadDataSet(finalQuery, decryptedConnStr);
        }

        private async Task<Component> GetComponent(string comId)
        {
            Component com = null;
            var comKey = "com_" + comId;
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
                com = await db.Component
                    .Where(x => x.Annonymous || !x.IsPrivate || x.TenantCode == TenantCode)
                    .Where(x => x.Id == comId)
                    .FirstOrDefaultAsync();
                if (com is null) return null;
                await _cache.SetStringAsync(comKey, JsonConvert.SerializeObject(com), Utils.CacheTTL);
            }
            var readPermission = await GetEntityPerm("Component", comId, x => x.CanRead);
            var hasPerm = com.Annonymous || !com.IsPrivate && UserId != null || readPermission.Any();
            if (!hasPerm)
            {
                throw new ApiException("Access denied") { StatusCode = HttpStatusCode.Unauthorized };
            }

            return com;
        }

        private async Task<FeaturePolicy[]> GetEntityPerm(string entityName, string recordId, Expression<Func<FeaturePolicy, bool>> pre = null)
        {
            var permission = ((pre?.Body as MemberExpression)?.Member as PropertyInfo)?.Name ?? "AllRights";
            var key = entityName + "_" + permission;
            var permissionByComCache = await _cache.GetStringAsync(key);
            FeaturePolicy[] permissions;
            if (!permissionByComCache.IsNullOrWhiteSpace())
            {
                permissions = JsonConvert.DeserializeObject<FeaturePolicy[]>(permissionByComCache);
            }
            else
            {
                var query = db.FeaturePolicy as IQueryable<FeaturePolicy>;
                if (pre != null) query = query.Where(pre);
                permissions = await query
                    .Where(x => x.EntityName == entityName && x.RecordId == recordId && RoleIds.Contains(x.RoleId))
                    .ToArrayAsync();
                await _cache.SetStringAsync(key, JsonConvert.SerializeObject(permissions), Utils.CacheTTL);
            }

            return permissions;
        }

        internal async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecUserSvc(SqlViewModel vm)
        {
            var sv = await GetService(vm);
            var jsRes = await ExecJs(vm.Entity, sv.Content);
            var conStr = TenantCode is null && vm.AnnonymousTenant is not null
                ? sv.ConnKey : await GetConnStrFromKey(sv.ConnKey);
            return await GetResultFromQuery(vm, conStr, jsRes);
        }

        private async Task<Models.Services> GetService(SqlViewModel vm)
        {
            if (vm is null || vm.SvcId.IsNullOrWhiteSpace() && (vm.Action.IsNullOrWhiteSpace() || vm.ComId.IsNullOrWhiteSpace()))
            {
                throw new ApiException("Service can NOT be identified due to lack of Id or action") { StatusCode = HttpStatusCode.BadRequest };
            }
            Models.Services sv;
            if (vm.SvcId is not null)
            {
                var cacheSv = await _cache.GetStringAsync(vm.SvcId);
                if (cacheSv != null)
                {
                    sv = JsonConvert.DeserializeObject<Models.Services>(cacheSv);
                }
            }

            var query = @$"select * from Services
                where (ComId = '{vm.ComId}' and Action = '{vm.Action}' or Id = '{vm.SvcId}') and (TenantCode = '{TenantCode}' or Annonymous = 1 and TenantCode = '{vm.AnnonymousTenant}')";
            var ds = await ReadDataSet(query, _configuration.GetConnectionString("default"));
            if (ds.Nothing()) return null;
            sv = new Models.Services();
            var svKeyMap = ds.ElementAt(0)?.ElementAt(0);
            svKeyMap.Keys.SelectForEach((x, i) =>
            {
                sv.SetPropValue(x, svKeyMap[x]);
            });

            if (sv == null)
            {
                return null;
            }
            if (TenantCode is null && !sv.Annonymous)
            {
                throw new UnauthorizedAccessException("The service is required login");
            }
            var isValidRole = sv.IsPublicInTenant ||
                (from svRole in sv.RoleIds.Split(',')
                 join usrRole in RoleIds on svRole equals usrRole
                 select svRole).Any();
            if (!isValidRole) throw new UnauthorizedAccessException("The service is not accessible by your roles");
            return sv;
        }

        internal async Task<string> ExportExcel(SqlViewModel vm)
        {
            var dataSets = await ReadDataSetWrapper(vm);
            var table = dataSets.ElementAt(0);
            var headers = JsonConvert.DeserializeObject<List<Component>>(JsonConvert.SerializeObject(dataSets.ElementAt(1)));
            headers = headers.Where(x => vm.FieldName.Contains(x.FieldName)).ToList();
            return ExportExcel(vm.ComId, headers, table);
        }

        public string ConvertHtmlToPlainText(string htmlContent)
        {
            // Remove HTML tags using regular expression
            string plainText = Regex.Replace(htmlContent, @"<[^>]+>|&nbsp;", "").Trim();

            // Decode HTML entities using regular expression
            plainText = Regex.Replace(plainText, @"&(amp|quot|gt|lt|nbsp);", m => DecodeEntity(m.Groups[1].Value));

            return plainText;
        }

        public string DecodeEntity(string entity)
        {
            switch (entity)
            {
                case "amp":
                    return "&";
                case "quot":
                    return "\"";
                case "gt":
                    return ">";
                case "lt":
                    return "<";
                case "nbsp":
                    return " ";
                default:
                    return entity;
            }
        }

        private string ExportExcel(string refName, List<Component> headers, IEnumerable<Dictionary<string, object>> dataSet)
        {
            XLWorkbook workbook;
            bool anyGroup = headers.Any(x => !string.IsNullOrEmpty(x.GroupName));
            workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");
            worksheet.Cell("A1").Value = refName;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Range(1, 1, headers.Count() + 1, headers.Count + 1).Row(1).Merge();
            worksheet.Style.Font.SetFontName("Times New Roman");
            var i = 2;
            worksheet.Cell(2, 1).SetValue("STT");
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(2, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            if (anyGroup)
            {
                worksheet.Range(2, 1, 3, 1).Merge();
            }
            foreach (var header in headers)
            {
                if (anyGroup && !string.IsNullOrEmpty(header.GroupName))
                {
                    var colspan = headers.Count(x => x.GroupName == header.GroupName);
                    if (header != headers.FirstOrDefault(x => x.GroupName == header.GroupName))
                    {
                        i++;
                        continue;
                    }
                    worksheet.Cell(2, i).SetValue(ConvertHtmlToPlainText(header.GroupName));
                    worksheet.Range(2, i, 2, i + colspan - 1).Merge();
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Font.Bold = true;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(2, i, 2, i + colspan - 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    i++;
                    continue;
                }
                worksheet.Cell(2, i).SetValue(ConvertHtmlToPlainText(header.ShortDesc));
                worksheet.Cell(2, i).Style.Font.Bold = true;
                worksheet.Cell(2, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(2, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(2, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (anyGroup && string.IsNullOrEmpty(header.GroupName))
                {
                    worksheet.Range(2, i, 3, i).Merge();
                    worksheet.Cell(3, i).Style.Font.Bold = true;
                    worksheet.Cell(3, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(3, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(3, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                i++;
            }
            var h = 2;
            if (anyGroup)
            {
                foreach (var item in headers)
                {
                    if (anyGroup && !string.IsNullOrEmpty(item.GroupName))
                    {
                        worksheet.Cell(3, h).SetValue(ConvertHtmlToPlainText(item.ShortDesc));
                        worksheet.Cell(3, h).Style.Font.Bold = true;
                        worksheet.Cell(3, h).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(3, h).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(3, h).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                    h++;
                }
            }
            var x = 3;
            if (anyGroup)
            {
                x++;
            }
            var j = 1;
            foreach (var item in dataSet)
            {
                var y = 2;
                worksheet.Cell(x, 1).SetValue(j);
                worksheet.Cell(x, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(x, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                foreach (var itemDetail in headers)
                {
                    var vl = item.GetValueOrDefault(itemDetail.FieldName);
                    switch (itemDetail.ComponentType)
                    {
                        case "Input":
                            var vl1 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl1);
                            break;
                        case "Textarea":
                            var vl2 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl2);
                            break;
                        case "Label":
                            var vl3 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl3);
                            break;
                        case "Datepicker":
                            worksheet.Cell(x, y).SetValue((DateTime?)vl);
                            break;
                        case "Number":
                            if (vl is int)
                            {
                                worksheet.Cell(x, y).SetValue(vl is null ? default(int) : (int)vl);
                            }
                            else
                            {
                                worksheet.Cell(x, y).SetValue(vl is null ? default(decimal) : (decimal)vl);
                                worksheet.Cell(x, y).Style.NumberFormat.Format = "#,##";
                            }
                            break;
                        case "SearchEntry":
                            var objField = itemDetail.FieldName.Substring(0, itemDetail.FieldName.Length - 2);
                            vl = item.GetValueOrDefault(objField);
                            var vl4 = vl is null ? null : vl.ToString().DecodeSpecialChar();
                            worksheet.Cell(x, y).SetValue(vl4);
                            break;
                        case "Checkbox":
                            worksheet.Cell(x, y).SetValue(vl.ToString() == "False" ? default(int) : 1);
                            break;
                        default:
                            break;
                    }
                    worksheet.Cell(x, y).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(x, y).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(x, y).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(x, y).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    y++;
                }
                j++;
                x++;
            }
            var k = 2;
            var last = dataSet.Count() + 3;
            worksheet.Cell(last, 1).Value = "Total";
            worksheet.Cell(last, 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            worksheet.Cell(last, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            foreach (var item in headers)
            {
                if (item.ComponentType == "Number")
                {
                    var value = dataSet.Select(x => x[item.FieldName]).Where(x => x != null).Sum(x =>
                    {
                        if (x is int)
                        {
                            return x is null ? default(int) : (int)x;
                        }
                        else
                        {
                            return x is null ? default(decimal) : (decimal)x;
                        }
                    });
                    worksheet.Cell(last, k).SetValue(value);
                    worksheet.Cell(last, k).Style.Font.Bold = true;
                    worksheet.Cell(last, k).Style.NumberFormat.Format = "#,##";
                }
                worksheet.Cell(last, k).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(last, k).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(last, k).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(last, k).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                k++;
            }
            var url = $"{refName}{DateTimeOffset.Now:ddMMyyyyhhmm}.xlsx";
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs($"wwwroot\\excel\\Download\\{url}");
            return url;
        }
    }
}
