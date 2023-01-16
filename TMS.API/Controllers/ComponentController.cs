using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.API.Controllers
{
    public class ComponentController : TMSController<Component>
    {
        public ComponentController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<Component>> Get(ODataQueryOptions<Component> options)
        {
            return base.Get(options);
        }

        [HttpPost("api/[Controller]", Order = -1)]
        public async Task<ActionResult<Component>> CreateAsync(
            [FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config,
            [FromBody] Component entity, CancellationToken cancellation)
        {
            if (entity == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await base.CreateAsync(entity);
            if (!entity.Migration.IsNullOrWhiteSpace())
            {
                var migration = JsonConvert.DeserializeObject<Migration>(entity.Migration);
                await ExeNonQuery(serviceProvider, config, migration?.Up, migration.Sys);
            }
            return entity;
        }

        [HttpPost("api/[Controller]/HardDelete", Order = -1)]
        public async Task<ActionResult<bool>> HardDeleteAsync(
            [FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config,
            [FromBody] List<int> ids, CancellationToken cancellation)
        {
            var com = await db.Component.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
            var res = await HardDeleteAndWebhookAsync(ids);

            foreach (var entity in com)
            {
                var migration = JsonConvert.DeserializeObject<Migration>(entity.Migration);
                await ExeNonQuery(serviceProvider, config, migration?.Down, migration.Sys);
            }
            return res;
        }

        public List<ComponentGroup> BuildTree(List<ComponentGroup> componentGroup)
        {
            var componentGroupMap = componentGroup.ToDictionary(x => x.Id);
            ComponentGroup parent;
            foreach (var item in componentGroup)
            {
                if (item.ParentId is null)
                {
                    continue;
                }
                if (!componentGroupMap.ContainsKey(item.ParentId.Value))
                {
                    continue;
                }
                parent = componentGroupMap[item.ParentId.Value];
                if (parent.InverseParent == null)
                {
                    parent.InverseParent = new List<ComponentGroup>();
                }
                if (!parent.InverseParent.Contains(item))
                {
                    parent.InverseParent.Add(item);
                }
                item.Parent = parent;
            }
            foreach (var item in componentGroup)
            {
                if (item.Component == null || !item.Component.Any())
                {
                    continue;
                }
                foreach (var ui in item.Component)
                {
                    ui.ComponentGroup = item;
                }
                if (item.InverseParent != null)
                {
                    item.InverseParent = item.InverseParent.OrderBy(x => x.Order).ToList();
                }
            }
            var res = componentGroup.Where(x => x.ParentId is null);
            return res.ToList();
        }
    }
}
