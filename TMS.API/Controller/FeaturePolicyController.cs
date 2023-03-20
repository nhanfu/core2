using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class FeaturePolicyController : TMSController<FeaturePolicy>
    {
        public FeaturePolicyController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public override Task<OdataResult<FeaturePolicy>> Get(ODataQueryOptions<FeaturePolicy> options)
        {
            var query = db.FeaturePolicy.Where(x => x.Active)
                .Where(x => x.InsertedBy == UserId || x.Feature.InsertedBy == UserId 
                    || x.UserId == UserId || AllRoleIds.Contains(x.RoleId.Value));
            return ApplyQuery(options, query);
        }

        public override Task<OdataResult<FeaturePolicy>> GetPublic(ODataQueryOptions<FeaturePolicy> options, string ids)
        {
            return Get(options);
        }

        public async override Task<ActionResult<FeaturePolicy>> CreateAsync([FromBody] FeaturePolicy entity)
        {
            var check = await db.FeaturePolicy.FirstOrDefaultAsync(x => x.UserId == entity.UserId && x.UserId != 0 && x.RecordId == entity.RecordId && x.RecordId != 0 && x.EntityId == entity.EntityId && x.EntityId != null);
            if (check != null)
            {
                return entity;
            }
            return await base.CreateAsync(entity);
        }

        [HttpPost("api/[Controller]/Ownership")]
        public async Task<List<int>> Ownership([FromBody] OwnershipRequest request)
        {
            return await db.FeaturePolicy
                .Where(x => x.EntityId == request.EntityType.Id
                    && x.CanShare && request.RecordIds.Contains(x.RecordId)
                    && (x.UserId == UserId || AllRoleIds.Contains(x.RoleId.Value)))
                .Select(x => x.RecordId)
                .ToListAsync();
        }

        [HttpPost("api/[Controller]/SharePermission")]
        public async Task<ActionResult<bool>> SharePermission([FromBody] SecurityVM securityVM)
        {
            var policies = securityVM.RecordIds.Where(x => x > 0).Select(x => new FeaturePolicy
            {
                Id = 0,
                FeatureId = securityVM.FeatureId,
                EntityId = securityVM.EntityId,
                UserId = securityVM.UserId,
                RoleId = securityVM.RoleId,
                CanRead = securityVM.CanRead,
                CanWrite = securityVM.CanWrite,
                CanDeactivate = securityVM.CanDeactivate,
                CanDelete = securityVM.CanDelete,
                CanShare = securityVM.CanShare,
                InsertedBy = securityVM.InsertedBy,
                InsertedDate = securityVM.InsertedDate,
                Active = true,
                RecordId = x,
            }).ToList();
            await BulkUpdateAsync(policies);
            return true;
        }

        public override async Task<List<FeaturePolicy>> BulkUpdateAsync([FromBody] List<FeaturePolicy> entities, string reasonOfChange = "")
        {
            if (entities.Nothing())
            {
                throw new ApiException("Không có dữ liệu thay đổi") { StatusCode = HttpStatusCode.BadRequest };
            }
            var entityId = entities.First().EntityId;
            foreach (var policy in entities)
            {
                if (policy.Id <= 0)
                {
                    var exist = await db.FeaturePolicy.FirstOrDefaultAsync(old => old.EntityId == entityId && old.RecordId == policy.RecordId
                        && ((policy.UserId != null && old.UserId == policy.UserId) || (policy.RoleId != null && old.RoleId == policy.RoleId)));
                    if (exist is null)
                    {
                        SetAuditInfo(policy);
                        db.Add(policy);
                        await db.SaveChangesAsync();
                        continue;
                    }
                    policy.Id = exist.Id;
                    exist.LockDeleteAfterCreated = policy.LockDeleteAfterCreated;
                    exist.LockUpdateAfterCreated = policy.LockUpdateAfterCreated;
                    exist.RecordId = policy.RecordId;
                    exist.RoleId = policy.RoleId;
                    exist.UserId = policy.UserId;
                    exist.FeatureId = policy.FeatureId;
                    exist.EntityId = policy.EntityId;
                    exist.CanWrite = policy.CanWrite;
                    exist.CanShare = policy.CanShare;
                    exist.CanRead = policy.CanRead;
                    exist.CanDelete = policy.CanDelete;
                    exist.CanDeactivate = policy.CanDeactivate;
                    exist.Active = true;
                    SetAuditInfo(exist);
                    await db.SaveChangesAsync();
                }
                else
                {
                    SetAuditInfo(policy);
                    db.Update(policy);
                    await db.SaveChangesAsync();
                }
            }
            return entities;
        }
    }
}
