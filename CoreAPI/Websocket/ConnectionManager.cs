using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Core.Websocket
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, List<string>> _queues = new();
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _sockets;
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public void AddSocket(WebSocket socket, string userId, List<string> roleIds, string ip)
        {
            _sockets.TryAdd($"{userId}/{string.Join(",", roleIds)}/{Guid.NewGuid()}/{ip}", socket);
        }

        public void SubScribeQueue(string userId, List<string> roleIds, string ip, string queueName)
        {
            var key = $"{userId}/{string.Join(",", roleIds)}/{Guid.NewGuid()}/{ip}";
            var hasVal = _queues.TryGetValue(key, out var value);
            if (hasVal && !value.Contains(queueName))
            {
                value.Add(queueName);
            }
            else
            {
                _queues[key] = [queueName];
            }
        }

        public void UnsubScribeQueue(string userId, List<string> roleIds, string ip, string queueName)
        {
            var key = $"{userId}/{string.Join(",", roleIds)}/{Guid.NewGuid()}/{ip}";
            var hasVal = _queues.TryGetValue(key, out var value);
            if (hasVal)
            {
                value.Remove(queueName);
            }
        }

        public async Task RemoveSocket(string id)
        {
            _sockets.TryRemove(id, out var socket);

            await socket.CloseAsync(
                closeStatus: WebSocketCloseStatus.NormalClosure,
                statusDescription: "Closed by the ConnectionManager",
                cancellationToken: CancellationToken.None);
        }
    }
}
