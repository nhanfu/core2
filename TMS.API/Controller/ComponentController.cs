using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TMS.API.Models;
using TMS.API.ViewModels;

namespace TMS.API.Controllers
{
    public class ComponentController : TMSController<Component>
    {
        public ComponentController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
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
    }
}
