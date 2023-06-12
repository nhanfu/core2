using Bridge.Html5;
using Core.Extensions;
using Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Clients
{
    public class EntityAction
    {
        public int EntityId { get; set; }
        public Action<object> Action { get; set; }
    }

    public class WebSocketClient
    {
        private readonly WebSocket _socket;
        private readonly List<EntityAction> EntityAction;
        public WebSocketClient(string url)
        {
            EntityAction = new List<EntityAction>();
            var wsUri = $"wss://{Client.Host}/{url}?access_token=" + Client.Token.AccessToken;
            _socket = new WebSocket(wsUri);
            _socket.OnOpen += e =>
            {
                Console.WriteLine("Socket opened", e);
            };

            _socket.OnClose += e =>
            {
                Console.WriteLine("Socket closed", e);
            };

            _socket.OnError += (e) =>
            {
                Console.WriteLine(e);
            };

            _socket.OnMessage += e =>
            {
                var responseStr = e.Data.ToString();
                var start = responseStr.IndexOf(":");
                var end = responseStr.IndexOf(",");
                var entityIdStr = responseStr.Substring(start + 1, end - start - 1);
                var parsed = int.TryParse(entityIdStr, out int entityId);
                if (!parsed)
                {
                    return;
                }

                var entityEnum = entityId;
                var entity = Utils.GetEntity(entityId);
                var entityType = entity.GetEntityType();
                if (entityType is null)
                {
                    return;
                }
                var responseType = typeof(WebSocketResponse<>).MakeGenericType(new Type[] { entityType });
                var result = JsonConvert.DeserializeObject(responseStr, responseType).As<WebSocketResponse<object>>();
                var res = result.Data;
                EntityAction.Where(x => x.EntityId == entityId).ForEach(x =>
                {
                    x.Action(res);
                });
            };
            _socket.BinaryType = WebSocket.DataType.ArrayBuffer;
        }

        public void Send(string message)
        {
            _socket.Send(message);
        }

        public void AddListener(int entityId, Action<object> entityAction)
        {
            var lastUpdate = EntityAction.FirstOrDefault(x => x.EntityId == entityId && x.Action == entityAction);
            if (lastUpdate is null)
            {
                EntityAction.Add(new EntityAction { EntityId = entityId, Action = entityAction });
            }
        }

        public void AddListener(string entityName, Action<object> entityAction)
        {
            var entity = Client.Entities.Values.FirstOrDefault(x => x.Name == entityName);
            EntityAction.Add(new EntityAction { EntityId = entity.Id, Action = entityAction });
        }

        public void RemoveListener(Action<object> action, int entityId)
        {
            EntityAction.RemoveAll(x => x.Action == action && x.EntityId == entityId);
        }

        public void Close() => _socket.Close();
    }
}
