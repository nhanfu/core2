using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using System.Linq.Dynamic.Core;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class FeatureController : TMSController<Feature>
    {
        public FeatureController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<Feature>> Get(ODataQueryOptions<Feature> options)
        {
            return base.Get(options);
        }

        [AllowAnonymous]
        public override Task<OdataResult<Feature>> GetPublic(ODataQueryOptions<Feature> options, string ids)
        {
            return base.GetPublic(options, ids);
        }

        public override async Task<ActionResult<Feature>> CreateAsync([FromBody] Feature entity)
        {
            var res = await base.CreateAsync(entity);
            var minRoleLevel = await GetTopRole();
            await AddDefaultPolicy(entity, minRoleLevel);
            await db.SaveChangesAsync();
            return res;
        }

        [HttpPost("api/[Controller]/Clone")]
        public async Task<ActionResult<bool>> CloneFeatureAsync([FromBody] int? id)
        {
            if (id == null)
            {
                return false;
            }
            var updateCommand = string.Format("EXECUTE dbo.[CloneFeature] @target= {0}", id);
            await ctx.Database.ExecuteSqlRawAsync(updateCommand);
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
            var policies = db.FeaturePolicy.Where(x => x.FeatureId != null && ids.Contains(x.FeatureId));
            db.FeaturePolicy.RemoveRange(policies);
            return await base.HardDeleteAsync(ids);
        }
    }
}
