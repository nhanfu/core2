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
        public string System { get; set; }
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
            System = claims.FirstOrDefault(x => x.Type == ClaimTypes.System)?.Value.ToUpper();
            Env = claims.FirstOrDefault(x => x.Type == "Env")?.Value.ToUpper();
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
            return await GetUserToken(matchedUser,
                system: login.System, tenant: login.CompanyName, env: login.Env, null, login.AutoSignIn);
        }

        private async Task<User> GetUserByLogin(LoginVM login)
        {
            var matchedUser =
                from user in db.User.Include(user => user.Vendor).Include(user => user.UserRole).ThenInclude(userRole => userRole.Role)
                where user.UserName == login.UserName && user.Active && user.Vendor.Code == login.CompanyName
                select user;
            return await matchedUser.FirstOrDefaultAsync();
        }

        protected virtual async Task<Token> GetUserToken(User user, string system, string tenant, string env, string refreshToken = null, bool autoSigin = false)
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
                new Claim(ClaimTypes.System, system),
                new Claim("Env", env),
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
            var system = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.System)?.Value;
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
            return await GetUserToken(updatedUser, system, tenant, token.RefreshToken);
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
                    .Where(x => x.System == system && x.TenantCode == tenantCode && x.Env == env)
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

        public async Task<(string, string)> DecryptQuery(string signed, string plainQuery = null)
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
            var key = $"{System}_{TenantCode}_{connKey}_{Env}";
            var conStr = await _cache.GetStringAsync(key);
            if (conStr != null) return conStr;
            var tenantEnv = await db.TenantEnv
                .FirstOrDefaultAsync(x => x.System == System && x.TenantCode == TenantCode && x.ConnKey == connKey && x.Env == Env)
                ?? throw new ApiException("Tenant config not found");
            await _cache.SetStringAsync(key, tenantEnv.ConnStr, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
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
                var map = new { UserId, RoleIds, AllRoleIds, TenantCode, System, Env, CenterIds, BranchId, VendorId };
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
            var anyComment = HasSqlComment(result.Query);
            if (anyComment) throw new ApiException("Comment is NOT allowed");
            return result;
        }

        public bool HasSqlComment(string sql)
        {
            TSql110Parser parser = new(true);
            var fragments = parser.Parse(new StringReader(sql), out var errors);

            return fragments.ScriptTokenStream
                .Any(x => x.TokenType == TSqlTokenType.MultilineComment || x.TokenType == TSqlTokenType.SingleLineComment);
        }

        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ReadDataSet(string reportQuery, string connStr, bool ignoreQuery = false)
        {
            var connectionStr = connStr;
            using var con = new SqlConnection(connectionStr);
            var sqlCmd = new SqlCommand(reportQuery, con)
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
            if (!ignoreQuery)
            {
                var query = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "query",  reportQuery }
                    }
                };
                tables.Add(query);
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

        public async Task<bool> PatchAsync(PatchUpdate patch)
        {
            if (patch == null || patch.Table.IsNullOrWhiteSpace() || patch.Changes.Nothing())
            {
                throw new ApiException("Table name and change details can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
            }
            patch.Table = RemoveWhiteSpace(patch.Table);
            patch.Changes.ForEach(x =>
            {
                if (x.Field.IsNullOrWhiteSpace()) throw new ApiException($"Field name can NOT be empty") { StatusCode = HttpStatusCode.BadRequest };
                x.Field = RemoveWhiteSpace(x.Field);
            });
            var id = patch.Changes.FirstOrDefault(x => x.Field == Utils.IdField)?.Value;
            using SqlConnection connection = new(await GetConnStrFromKey(patch.ConnKey));
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqlCommand command = new();
                command.Transaction = transaction;
                command.Connection = connection;
                
                var valueFields = patch.Changes.Where(x => !_systemFields.Contains(x.Field.ToLower())).ToList();
                var update = valueFields.Select(x => $"[{x.Field}] = @{x.Field.ToLower()}");
                var now = DateTimeOffset.Now.ToString(DateTimeExt.DateFormat);
                if (id is not null)
                {
                    command.CommandText = @$"udpate [{patch.Table}] set {update.Combine()}, 
                            TenantCode = {TenantCode}, UpdatedBy = '{UserId}', UpdatedDate = '{now}' where Id = '{id}';";
                }
                else
                {
                    id = Id.NewGuid();
                    var fields = valueFields.Combine(x => $"[{x.Field}]");
                    var fieldParams = valueFields.Combine(x => $"@{x.Field}");
                    command.CommandText = @$"insert into [{patch.Table}] ([Id], [TenantCode], [Active], [InsertedBy], [InsertedDate], {fields}) 
                        values ('{id}', '{TenantCode}', 1, '{UserId}', '{now}', {fieldParams});";
                }
                foreach (var item in valueFields)
                {
                    command.Parameters.AddWithValue($"@{item.Field.ToLower()}", item.Value is null ? DBNull.Value : item.Value);
                }
                var anyComment = HasSqlComment(command.CommandText);
                if (anyComment) throw new ApiException("Comment is NOT allowed");
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                if (!patch.QueueName.IsNullOrWhiteSpace())
                {
                    var mqEvent = new MQEvent
                    {
                        Id = Id.NewGuid(),
                        QueueName = patch.QueueName,
                        Message = patch
                    };
                    BackgroundJob.Enqueue<TaskService>(x => x.SendMessageToSubscribers(mqEvent, mqEvent.QueueName));
                }
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        internal async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> Reader(SqlViewModel model)
        {
            var entity = model.Component;
            var (connStr, query) = await DecryptQuery(entity.Signed, entity.Query);

            var anyInvalid = _fobiddenTerm.Any(term =>
            {
                return model.Select != null && term.IsMatch(model.Select.ToLower())
                || model.Entity != null && term.IsMatch(model.Entity.ToLower())
                || model.Where != null && term.IsMatch(model.Where.ToLower())
                || model.GroupBy != null && term.IsMatch(model.GroupBy.ToLower())
                || model.Having != null && term.IsMatch(model.Having.ToLower())
                || model.OrderBy != null && term.IsMatch(model.OrderBy.ToLower())
                || model.Paging != null && term.IsMatch(model.Paging.ToLower());
            });
            if (anyInvalid)
            {
                throw new ArgumentException("Parameters must NOT contains sql keywords");
            }
            var jsRes = await ExecJs(model.Entity, entity.Query ?? query);
            var select = model.Select.HasAnyChar() ? $"select {model.Select}" : string.Empty;
            var where = model.Where.HasAnyChar() ? $"where {model.Where}" : string.Empty;
            var groupBy = model.GroupBy.HasAnyChar() ? $"group by {model.GroupBy}" : string.Empty;
            var having = model.Having.HasAnyChar() ? $"having {model.Having}" : string.Empty;
            var orderBy = model.OrderBy.HasAnyChar() ? $"order by {model.OrderBy}" : string.Empty;
            var countQuery = model.Count ?
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
                {model.Paging};
                {countQuery}
                {jsRes.XQuery}";
            return await ReadDataSet(finalQuery, connStr);
        }

        internal async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecUserSvc(SqlViewModel vm)
        {
            if (vm is null || vm.SvcId.IsNullOrWhiteSpace() && (vm.Action.IsNullOrWhiteSpace() || vm.ComId.IsNullOrWhiteSpace()))
            {
                throw new ApiException("Service can NOT be identified due to lack of Id or action") { StatusCode = Enums.HttpStatusCode.BadRequest };
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
            sv = await db.Services.Where(x =>
                    x.System == System && x.TenantCode == TenantCode
                    && (x.ComId == vm.ComId && x.Action == vm.Action || vm.SvcId != null && x.Id == vm.SvcId))
                .FirstOrDefaultAsync();

            if (sv == null)
            {
                return null;
            }
            var isValidRole = sv.IsPublicInTenant ||
                (from svRole in sv.RoleIds.Split(',')
                 join usrRole in RoleIds on svRole equals usrRole
                 select svRole).Any();
            if (!isValidRole) throw new UnauthorizedAccessException("The service is not accessible by your roles");

            var jsRes = await ExecJs(vm.Entity, sv.Content);
            var conStr = await GetConnStrFromKey(sv.ConnKey);
            return await ReadDataSet(jsRes.Query, conStr);
        }
    }
}
