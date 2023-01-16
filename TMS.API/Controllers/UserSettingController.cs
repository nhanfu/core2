using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class UserSettingController : TMSController<UserSetting>
    {
        public UserSettingController(TMSContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {

        }

        [AllowAnonymous]
        public override Task<OdataResult<UserSetting>> Get(ODataQueryOptions<UserSetting> options)
        {
            var query =
                from setting in db.UserSetting
                from role in db.UserRole.Where(x => x.UserId == UserId && (setting.UserId == UserId || x.RoleId == setting.RoleId))
                where setting.Active
                select setting;
            return ApplyQuery(options, query.Distinct());
        }
    }
}
