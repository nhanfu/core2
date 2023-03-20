using Core.Extensions;
using Core.SMSModels;
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

namespace TMS.API.Controllers
{
    public class GridPolicyController : TMSController<GridPolicy>
    {
        public GridPolicyController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<GridPolicy>> Get(ODataQueryOptions<GridPolicy> options)
        {
            var query = db.Set<GridPolicy>().AsQueryable();
            return ApplyQuery(options, query);
        }

        [HttpPost("api/[Controller]", Order = -1)]
        public async Task<ActionResult<GridPolicy>> CreateAsync(
            [FromServices] IServiceProvider serviceProvider, [FromServices] IConfiguration config,
            [FromBody] GridPolicy entity, CancellationToken cancellation)
        {
            if (entity == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await base.CreateAsync(entity);
            if (!entity.Migration.IsNullOrWhiteSpace())
            {
                var migration = JsonConvert.DeserializeObject<ViewModels.Migration>(entity.Migration);
                await ExeNonQuery(serviceProvider, config, migration?.Up, migration.Sys);
            }
            return entity;
        }

        [HttpGet("api/[Controller]/GetByComponentGroupId/{id}")]
        public Task<OdataResult<GridPolicy>> GetGridPolicyByComponentGroup(int id, ODataQueryOptions<GridPolicy> options)
        {
            var query =
                from cg in db.ComponentGroup
                join com in db.Component on cg.Id equals com.ComponentGroupId
                join feature in db.Feature on cg.FeatureId equals feature.Id
                join gridPolicy in db.GridPolicy on feature.Id equals gridPolicy.FeatureId
                where cg.Id == id && gridPolicy.EntityId == com.ReferenceId
                select gridPolicy;
            return ApplyQuery(options, query);
        }

        [HttpGet("api/[Controller]/UserColumn/{listViewId}")]
        public Task<OdataResult<GridPolicy>> UserColumn(int listViewId, ODataQueryOptions<GridPolicy> options)
        {
            var query =
                from com in db.Component
                join comGroup in db.ComponentGroup on com.ComponentGroupId equals comGroup.Id
                join feature in db.Feature on comGroup.FeatureId equals feature.Id
                join column in db.GridPolicy on feature.Id equals column.FeatureId
                where com.Id == listViewId
                select column;
            return ApplyQuery(options, query);
        }
    }
}
