using Core.Extensions;
using Core.Models;

namespace Core.Controllers
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
