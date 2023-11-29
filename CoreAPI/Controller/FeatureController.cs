using Core.Extensions;
using Core.Models;
using HtmlAgilityPack;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Text;

namespace Core.Controllers
{
    public class FeatureController : TMSController<Feature>
    {
        private const string ContentType = "Content-Type";
        private const string NotFoundFile = "wwwRoot/404.html";
        private const string href = "href";
        private const string src = "src";
        private readonly IDistributedCache _cached;
        public FeatureController(CoreContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor, IDistributedCache cached)
            : base(context, entityService, httpContextAccessor)
        {
            _cached = cached;
        }

        [AllowAnonymous]
        [HttpGet("/{tenant?}/{area?}/{env?}/{feature?}")]
        public async Task Index([FromRoute] string tenant = "system",
            [FromRoute] string area = "admin", [FromRoute] string env = "test")
        {
            if (_userSvc.TenantCode != null && _userSvc.TenantCode != tenant)
            {
                throw new UnauthorizedAccessException($"Page not found for the tanent {tenant} due to the current user was signed in with the tenant {_userSvc.TenantCode}.");
            }
            var ext = Path.GetExtension(Request.Path);
            if (!ext.IsNullOrWhiteSpace())
            {
                await Response.WriteAsync("File not found");
                return;
            }
            var htmlMimeType = Utils.GetMimeType("html");
            var key = $"{tenant}_{env}_{area}";
#if RELEASE
            var cache = await _cached.GetStringAsync(key);
            if (cache != null)
            {
                var pageCached = JsonConvert.DeserializeObject<TenantPage>(cache);
                await WriteTemplateAsync(Response, pageCached, env, tenant);
                return;
            }
#endif

            var tenantEnv = await db.TenantEnv.FirstOrDefaultAsync(x => x.TenantCode == tenant && x.Env == env);
            if (tenantEnv is null)
            {
                await WriteDefaultFile(NotFoundFile, htmlMimeType, HttpStatusCode.NotFound);
                return;
            }
            var page = await db.TenantPage.AsNoTracking().FirstOrDefaultAsync(x =>
                x.TenantEnvId == tenantEnv.Id && x.Area == area);
            await _cached.SetStringAsync(key, JsonConvert.SerializeObject(page));
            await WriteTemplateAsync(Response, page, env: env, tenant: tenant);
        }

        private async Task WriteTemplateAsync(HttpResponse reponse, TenantPage page, string env, string tenant)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(page.Template);

            var links = htmlDoc.DocumentNode.SelectNodes("//link | //script")
                .SelectForEach((HtmlNode x, int i) =>
                {
                    ShouldAddVersion(x, href);
                    ShouldAddVersion(x, src);
                });
            var meta = new HtmlNode(HtmlNodeType.Element, htmlDoc, 1)
            {
                Name = "meta"
            };
            meta.SetAttributeValue("name", "startupSvc");
            meta.SetAttributeValue("content", page.SvcId);
            htmlDoc.DocumentNode.SelectSingleNode("//head")?.AppendChild(meta);
            reponse.Headers.TryAdd(ContentType, Utils.GetMimeType("html"));
            reponse.StatusCode = (int)HttpStatusCode.OK;
            await reponse.WriteAsync(htmlDoc.DocumentNode.OuterHtml);
        }

        private static void ShouldAddVersion(HtmlNode x, string attr)
        {
            var shouldAdd = x.Attributes.Contains(attr)
                && x.Attributes[attr].Value.IndexOf("?v=") < 0;
            if (shouldAdd)
            {
                x.Attributes[attr].Value += "?v=" + Guid.NewGuid().ToString();
            }
        }

        private async Task WriteDefaultFile(string file, string contentType
            , HttpStatusCode code = HttpStatusCode.OK)
        {
            if (!Response.HasStarted)
            {
                Response.Headers.TryAdd(ContentType, contentType);
                Response.Headers.TryAdd("Content-Encoding", "gzip");
                Response.StatusCode = (int)code;
            }
            var html = await System.IO.File.ReadAllTextAsync(file, encoding: Encoding.UTF8);
            await Response.WriteAsync(html);
        }

        [AllowAnonymous]
        public override Task<OdataResult<Feature>> Get(ODataQueryOptions<Feature> options)
        {
            var query = db.Feature
                .Where(x => _userSvc.TenantCode != null && x.TenantCode == _userSvc.TenantCode
                || _userSvc.TenantCode == null && x.IsPublic);
            return ApplyQuery(options, query);
        }

        [AllowAnonymous]
        public override Task<OdataResult<Feature>> GetPublic(ODataQueryOptions<Feature> options, string ids)
        {
            return base.GetPublic(options, ids);
        }

        public override async Task<ActionResult<Feature>> CreateAsync([FromBody] Feature entity)
        {
            SetAuditInfo(entity);
            var text = JsonConvert.SerializeObject(entity);
            entity.Signed = _userSvc.GetHash(UserUtils.sHA256, text);
            var res = await base.CreateAsync(entity);
            var minRoleLevel = await GetTopRole();
            await AddDefaultPolicy(entity, minRoleLevel);
            await db.SaveChangesAsync();
            return res;
        }

        [HttpPost("api/[Controller]/Clone")]
        public async Task<ActionResult<bool>> CloneFeatureAsync([FromBody] string id)
        {
            if (id == null)
            {
                return false;
            }
            var feature = await db.Feature.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            var policies = await db.FeaturePolicy.AsNoTracking().Where(x => x.FeatureId == id).ToArrayAsync();
            var groups = await db.ComponentGroup.AsNoTracking().Where(x => x.FeatureId == id).ToArrayAsync();
            var components = await db.Component.AsNoTracking().Where(x => groups.Select(g => g.Id).Contains(x.ComponentGroupId)).ToArrayAsync();
            feature.Id = Id.NewGuid().ToString();
            policies.SelectForeach(x =>
            {
                x.Id = Id.NewGuid().ToString();
                x.FeatureId = feature.Id;
            });
            feature.FeaturePolicy = policies;
            groups.SelectForeach(group =>
            {
                var com = components.Where(c => c.ComponentGroupId == group.Id).ToList();
                group.Id = Id.NewGuid().ToString();
                com.ForEach(c =>
                {
                    c.Id = Id.NewGuid().ToString();
                    c.ComponentGroupId = group.Id;
                });
            });
            feature.ComponentGroup = groups;
            db.Add(feature);
            db.AddRange(groups);
            db.AddRange(components);
            await db.SaveChangesAsync();
            return true;
        }

        public override async Task<ActionResult<Feature>> UpdateAsync([FromBody] Feature entity, string reasonOfChange = "")
        {
            var res = await base.UpdateAsync(entity, reasonOfChange);
            await InheritParentPolicy(entity);
            var connectionString = _config.GetConnectionString("Default");
            using SqlConnection connection = new(connectionString);
            connection.Open();
            SqlCommand command = new("SELECT name FROM sys.databases where name like N'tms_%'", connection);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string databaseName = reader["name"].ToString();
                Console.WriteLine(databaseName);
            }
            reader.Close();
            return res;
        }

        private async Task AddDefaultPolicy(Feature feature, Role minRoleLevel)
        {
            var featurePolicy = new FeaturePolicy
            {
                Active = true,
                CanRead = true,
                CanWrite = true,
                CanDelete = true,
                RoleId = minRoleLevel.Id,
                FeatureId = feature.Id,
            };
            SetAuditInfo(featurePolicy);
            db.FeaturePolicy.Add(featurePolicy);
            await db.SaveChangesAsync();
            await InheritParentPolicy(feature);
        }

        private async Task InheritParentPolicy(Feature feature)
        {
            if (!feature.InheritParentFeature || feature.ParentId is null)
            {
                return;
            }
            var currentPolicy = await db.FeaturePolicy
                .Where(x => x.Active && x.CanRead && x.FeatureId == feature.Id && x.RoleId != null && (x.RecordId == null || x.RecordId == "0")).ToListAsync();
            db.RemoveRange(currentPolicy);
            var parentPolicy = await db.FeaturePolicy.AsNoTracking()
                .Where(x => x.Active && x.CanRead && x.FeatureId == feature.ParentId && x.RoleId != null && (x.RecordId == null || x.RecordId == "0")).ToListAsync();
            parentPolicy
                .ForEach(policy =>
                {
                    policy.Id = null;
                    policy.FeatureId = feature.Id;
                    _userSvc.SetAuditInfo(policy);
                    db.FeaturePolicy.Add(policy);
                });
            await db.SaveChangesAsync();
        }

        private async Task<Role> GetTopRole()
        {
            var roles =
                from role in db.Role
                where AllRoleIds.Contains(role.Id)
                orderby role.Path descending
                select role;
            var minRoleLevel = await roles.FirstOrDefaultAsync();
            return minRoleLevel;
        }

        public override async Task<ActionResult<bool>> DeactivateAsync([FromBody] List<string> ids)
        {
            var hasSystemRole = await HasSystemRole();
            if (!hasSystemRole)
            {
                throw new UnauthorizedAccessException("You dont have system role to delete this feature");
            }
            return await base.DeactivateAsync(ids);
        }

        public override async Task<ActionResult<bool>> HardDeleteAsync([FromBody] List<string> ids)
        {
            var hasSystemRole = await HasSystemRole();
            if (!hasSystemRole)
            {
                throw new UnauthorizedAccessException("You dont have system role to delete this feature");
            }
            var features = await db.Feature.Where(x => ids.Contains(x.Id)).ToListAsync();
            var policies = await db.FeaturePolicy.Where(x => ids.Contains(x.FeatureId)).ToListAsync();
            var groups = await db.ComponentGroup.Where(x => ids.Contains(x.FeatureId)).ToListAsync();
            var components = await db.Component.Where(x => groups.Select(g => g.Id).Contains(x.ComponentGroupId)).ToArrayAsync();
            db.FeaturePolicy.RemoveRange(policies);
            db.ComponentGroup.RemoveRange(groups);
            db.Component.RemoveRange(components);
            db.Feature.RemoveRange(features);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
