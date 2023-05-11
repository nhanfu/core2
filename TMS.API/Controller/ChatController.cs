using Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using TMS.API.Models;
using TMS.API.ViewModels;

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
            if (entity.ToId == 552)
            {
                var rs1 = await GetChatGPTResponse(entity);
                db.Add(rs1);
                SetAuditInfo(rs1);
                await db.SaveChangesAsync();
                var chat = new WebSocketResponse<Chat>
                {
                    EntityId = _entitySvc.GetEntity(nameof(Chat))?.Id ?? 0,
                    Data = rs1
                };
                await _taskService.SendChatToUser(chat);
                return rs;
            }
            else
            {
                var chat = new WebSocketResponse<Chat>
                {
                    EntityId = _entitySvc.GetEntity(nameof(Chat))?.Id ?? 0,
                    Data = rs.Value
                };
                await _taskService.SendChatToUser(chat);
                return rs;
            }
        }

        private async Task<Chat> GetChatGPTResponse(Chat entity)
        {
            var apiKey = "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";
            var endpoint = "https://api.openai.com/v1/chat/completions";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var requestData = new ChatGptVM
                {
                    model = "gpt-3.5-turbo",
                    messages = new List<ChatGptMessVM>()
                    {
                        new ChatGptMessVM
                        {
                            role = "user",
                            content = entity.Context,
                            name = entity.FromId.ToString(),
                        }
                    }
                };
                var jsonRequestData = JsonConvert.SerializeObject(requestData);
                var response = await httpClient.PostAsync(endpoint, new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));
                var jsonResponseData = await response.Content.ReadAsStringAsync();
                var rs = JsonConvert.DeserializeObject<RsChatGpt>(jsonResponseData);
                return new Chat()
                {
                    FromId = entity.ToId,
                    ToId = entity.FromId,
                    Context = rs.choices.FirstOrDefault().message.content,
                    ConvertationId = entity.ConvertationId,
                    IsSeft = true,
                };
            }
        }
    }
}
