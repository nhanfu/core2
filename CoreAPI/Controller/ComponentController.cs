using Core.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Core.Models;
using Core.ViewModels;

namespace Core.Controllers
{
    public class ComponentController : TMSController<Component>
    {
        public ComponentController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {
        }

        [AllowAnonymous]
        public override Task<OdataResult<Component>> Get(ODataQueryOptions<Component> options)
        {
            var query = db.Component
                .Where(x => _userSvc.TenantCode != null && x.TenantCode == _userSvc.TenantCode
                || _userSvc.TenantCode == null && !x.IsPrivate);
            return ApplyQuery(options, query);
        }

        [HttpPost("api/[Controller]", Order = 0)]
        public async Task<ActionResult<Component>> CreateAsync([FromBody] Component entity, [FromQuery] string env = "test")
        {
            entity.Signed = await _userSvc.EncryptQuery(entity.Query, env);
            return await base.CreateAsync(entity);
        }

        [HttpPut("api/[Controller]", Order = 0)]
        public async Task<ActionResult<Component>> UpdateAsync([FromBody] Component entity
            , [FromQuery]string reasonOfChange = "", [FromQuery] string env = "test")
        {
            entity.Signed = await _userSvc.EncryptQuery(entity.Query, env);
            return await base.UpdateAsync(entity, reasonOfChange);
        }

        static readonly Regex[] _fobiddenTerm = new Regex[] { new Regex(@"delete\s"), new Regex(@"create\s"), new Regex(@"insert\s"),
                new Regex(@"update\s"), new Regex(@"select\s"), new Regex(@"from\s"),new Regex(@"where\s"),
                new Regex(@"group by\s"), new Regex(@"having\s"), new Regex(@"order by\s") };

        [HttpPost("api/[Controller]/Reader")]
        public async Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> Reader(
            [FromBody] SqlViewModel model, [FromQuery] string env = "test")
        {
            var entity = model.Component;
            var connStr = await _userSvc.DecryptQuery(entity.Query, entity.Signed, env);
            
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
            var jsRes = await _userSvc.ExecJs(model.Entity, entity.Query);
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
            return await ReportDataSet(finalQuery, connStr);
        }
    }
}
