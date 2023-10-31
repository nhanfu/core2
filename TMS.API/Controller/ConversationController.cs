using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ConversationController : TMSController<Conversation>
    {
        public ConversationController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        protected override IQueryable<Conversation> GetQuery()
        {
            return db.Conversation.Where(x => x.TenantCode == _userSvc.TenantCode);
        }
    }
}
