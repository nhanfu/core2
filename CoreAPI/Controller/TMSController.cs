using Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Dynamic.Core;
using Core.Models;

namespace Core.Controllers
{
    [Authorize]
    public class TMSController<T> : GenericController<T> where T : class
    {
        private const string SearchEntry = "SearchEntry";
        protected readonly CoreContext db;
        protected readonly ILogger<TMSController<T>> _logger;
        protected HttpClient _client;
        protected string PathField = "Path";
        protected string ParentIdField = "ParentId";
        protected string ChildrenField = "InverseParent";
        private string _address;

        public TMSController(CoreContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
            db = context;
            _logger = (ILogger<TMSController<T>>)httpContextAccessor.HttpContext.RequestServices.GetService(typeof(ILogger<TMSController<T>>));
            _config = _httpContext.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            _address = _config["ServiceAddress"];
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
        public virtual Task<ActionResult<bool>> HardDeleteListenerAsync([FromRoute] string id)
        {
            return HardDeleteAsync(id);
        }

        protected async Task<bool> HasSystemRole()
        {
            return await db.Role.AnyAsync(x => AllRoleIds.Contains(x.Id) && x.RoleName.Contains("system"));
        }


        protected virtual async Task Approving(T entity)
        {
            var oldEntity = await db.Set<T>().FindAsync((int)entity.GetComplexPropValue(IdField));
            oldEntity.CopyPropFrom(entity);
            await db.SaveChangesAsync();
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
            nodeMap.Values.SelectForeach(x =>
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
            directChildren.SelectForeach(child =>
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
    }
}
