using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using TMS.API.Models;

namespace TMS.API.Controllers
{
    public class ChatController : TMSController<Chat>
    {
        public ChatController(TMSContext context, EntityService entityService, IHttpContextAccessor httpContextAccessor) : base(context, entityService, httpContextAccessor)
        {

        }

        public override async Task<ActionResult<Chat>> CreateAsync([FromBody] Chat entity)
        {
            var rs = await base.CreateAsync(entity);
            var chat = new WebSocketResponse<Chat>
            {
                EntityId = _entitySvc.GetEntity(nameof(Chat))?.Id ?? 0,
                Data = rs.Value
            };
            await _taskService.SendChatToUser(chat);
            return rs;
        }
    }
}
