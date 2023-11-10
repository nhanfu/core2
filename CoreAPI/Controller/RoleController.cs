using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Core.Models;

namespace Core.Controllers
{
    public class RoleController : TMSController<Role>
    {
        public RoleController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        public override Task<OdataResult<Role>> Get(ODataQueryOptions<Role> options)
        {

            var query =
                from role in db.Role
                from policy in db.FeaturePolicy
                    .Where(x => x.RecordId == role.Id && x.EntityId == _entitySvc.GetEntity(nameof(Role)).Id && (x.UserId == UserId || AllRoleIds.Contains(x.RoleId)))
                    .DefaultIfEmpty()
                where AllRoleIds.Contains(role.Id) || policy != null || role.InsertedBy == UserId
                select role;
            return ApplyQuery(options, query.Distinct());
        }

        public override async Task<ActionResult<Role>> UpdateAsync([FromBody] Role entity, string reasonOfChange = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!await HasSystemRole())
            {
                return Unauthorized("Bạn không có quyền cập nhật");
            }
            if (entity.ParentRoleId != null)
            {
                var parentEntity = await db.Role.FirstOrDefaultAsync(x => x.Id == entity.ParentRoleId);
                var pathParent = parentEntity.Path;
                entity.Path = @$"\{pathParent}\{ entity.ParentRoleId}\".Replace(@"\\", @"\");
            }
            else
            {
                entity.Path = null;
            }
            SetLevel(entity);
            var res = await base.UpdateAsync(entity, reasonOfChange);
            var children = await db.Role.Where(x => x.Path.Contains("\\" + entity.Id + "\\"))
                .Include(x => x.ParentRole)
                .OrderByDescending(x => x.Path.Length)
                .ToListAsync();
            if (children.Nothing())
            {
                return res;
            }
            var roleMap = children.Union(children.Select(x => x.ParentRole).Where(x => x != null))
                .DistinctBy(x => x.Id).ToDictionary(x => x.Id);
            roleMap.Values.SelectForeach(x =>
            {
                if (x.ParentRoleId is null || !roleMap.ContainsKey(x.ParentRoleId))
                {
                    return;
                }
                var parent = roleMap[x.ParentRoleId];
                x.ParentRole = parent;
                if (parent.InverseParentRole is null)
                {
                    parent.InverseParentRole = new HashSet<Role>();
                }
                parent.InverseParentRole.Add(x);
            });
            var directChildren = roleMap.Values.Where(x => x.ParentRoleId == entity.Id);
            SetChildrenPath(directChildren);
            await db.SaveChangesAsync();
            return res;
        }

        private void SetChildrenPath(IEnumerable<Role> directChildren)
        {
            if (directChildren.Nothing())
            {
                return;
            }
            directChildren.SelectForeach(child =>
            {
                if (child.ParentRoleId is not null)
                {
                    child.Path = @$"\{child.ParentRole.Path}\{child.ParentRoleId}\".Replace(@"\\", @"\");
                }
                SetLevel(child);
                SetChildrenPath(child.InverseParentRole);
            });
        }

        public override async Task<ActionResult<Role>> CreateAsync([FromBody] Role entity)
        {
            if (!await HasSystemRole())
            {
                return Unauthorized("Bạn không có quyền cập nhật");
            }
            if (entity.ParentRoleId != null)
            {
                var parentEntity = await db.Role.FirstOrDefaultAsync(x => x.Id == entity.ParentRoleId);
                if (parentEntity != null)
                {
                    entity.Path = @$"\{parentEntity.Path}\{entity.ParentRoleId}\".Replace(@"\\", @"\");
                }
            }
            else
            {
                entity.Path = null;
            }
            SetLevel(entity);
            var rs = await base.CreateAsync(entity);
            if (entity.InverseParentRole.Any())
            {
                entity.InverseParentRole.SelectForeach(x =>
                {
                    x.Path = @$"\{entity.Path}\{x.Id}\".Replace(@"\\", @"\");
                });
            }
            await db.SaveChangesAsync();
            return rs;
        }
    }
}
