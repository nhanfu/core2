using Core.Extensions;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ConversationController : TMSController<Conversation>
    {
        public ConversationController(TMSContext context,EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }
    }
}
