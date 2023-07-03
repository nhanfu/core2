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
        public int TypeId { get; set; }
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
                var objRs = JsonConvert.DeserializeObject<SocketResponse>(responseStr);
                var entityEnum = objRs.EntityId;
                var entity = Utils.GetEntity(objRs.EntityId);
                var entityType = entity.GetEntityType();
                if (entityType is null)
                {
                    return;
                }
                var responseType = typeof(WebSocketResponse<>).MakeGenericType(new Type[] { entityType });
                var result = JsonConvert.DeserializeObject(responseStr, responseType).As<WebSocketResponse<object>>();
                var res = result.Data;
                EntityAction.Where(x => x.EntityId == objRs.EntityId && x.TypeId == objRs.TypeId).ForEach(x =>
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

        public void AddListener(int entityId, int typeId, Action<object> entityAction)
        {
            var lastUpdate = EntityAction.FirstOrDefault(x => x.EntityId == entityId && x.Action == entityAction && x.TypeId == typeId);
            if (lastUpdate is null)
            {
                EntityAction.Add(new EntityAction { EntityId = entityId, Action = entityAction, TypeId = typeId });
            }
        }

        public void AddListener(string entityName, int typeId, Action<object> entityAction)
        {
            var entity = Client.Entities.Values.FirstOrDefault(x => x.Name == entityName);
            var lastUpdate = EntityAction.FirstOrDefault(x => x.EntityId == entity.Id && x.Action == entityAction && x.TypeId == typeId);
            if (lastUpdate is null)
            {
                EntityAction.Add(new EntityAction { EntityId = entity.Id, Action = entityAction, TypeId = typeId });
            }
        }

        public void RemoveListener(Action<object> action, int entityId, int typeId)
        {
            EntityAction.RemoveAll(x => x.Action == action && x.EntityId == entityId && x.TypeId == typeId);
        }

        public void Close() => _socket.Close();
    }
}
