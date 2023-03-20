using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class EntityController : TMSController<Entity>
    {
        public EntityController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<Entity>> Get(ODataQueryOptions<Entity> options)
        {
            return base.Get(options);
        }

        [HttpGet("api/[Controller]/Enum/{enumType}")]
        public virtual IActionResult GetState(string enumType, string ns, int? current, ODataQueryOptions<Entity> options)
        {
            var query = Enum.GetValues(Type.GetType((ns ?? "TMS.API.Enums") + "." + enumType));
            var res = query.ToDynamicList().Where(x => current is null || (int)x >= current).ToArray();
            return QueryEnum(options, res);
        }

        private IActionResult QueryEnum(ODataQueryOptions<Entity> options, Array query)
        {
            var res = new List<Entity>();
            foreach (var item in query)
            {
                res.Add(new Entity
                {
                    Id = (int)item,
                    Name = item.ToString(),
                    Description = ReflectionExt.GetEnumDescription(item as Enum),
                    Active = true
                });
            }
            return Ok(new OdataResult<Entity>
            {
                value = options.ApplyTo(res.AsQueryable())
            });
        }

        /// <summary>
        /// The entity id is not auto increasement
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="reasonOfChange"></param>
        /// <param name="ignoreSaveChanges"></param>
        /// <returns></returns>
        public override Task<ActionResult<Entity>> UpdateAsync([FromBody] Entity entity, string reasonOfChange = "")
        {
            if (entity.Id == 0)
            {
                _userSvc.SetAuditInfo(entity);
            }
            return base.UpdateAsync(entity, reasonOfChange);
        }
    }
}
