using Core.Extensions;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Core.Services
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, List<string>> _queues = new();
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ConcurrentDictionary<string, WebSocket> _clusters = new();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public IEnumerable<WebSocket> GetSocketByQueue(string queueName)
        {
            var hasQueue = _queues.TryGetValue(queueName, out var deviceIds);
            if (!hasQueue) yield break;
            foreach (var item in deviceIds)
            {
                var hasDevice = _sockets.TryGetValue(item, out var device);
                if (!hasDevice) continue;
                yield return device;
            }
        }

        public ConcurrentDictionary<string, WebSocket> GetDeviceSockets()
        {
            return _sockets;
        }

        public string GetId(WebSocket socket, bool cluster = false)
        {
            return cluster ? _clusters.FirstOrDefault(p => p.Value == socket).Key : _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public string AddSocket(WebSocket socket, string deviceKey)
        {
            _sockets.TryAdd(deviceKey, socket);
            return deviceKey;
        }

        public string AddDeviceSocket(WebSocket socket, string userId, List<string> roleIds, string ip)
        {
            var deviceKey = $"{userId}/{roleIds.Combine()}/{ip}/{Uuid7.Id25()}";
            _sockets.TryAdd(deviceKey, socket);
            return deviceKey;
        }

        public string AddClusterSocket(WebSocket socket, string clusterKey)
        {
            _clusters.TryAdd(clusterKey, socket);
            return clusterKey;
        }

        public void SubScribeQueue(string deviceKey, string queueName)
        {
            var hasVal = _queues.TryGetValue(queueName, out var devices);
            if (hasVal && !devices.Contains(deviceKey))
            {
                devices.Add(deviceKey);
            }
            else if (!hasVal)
            {
                _queues[queueName] = [deviceKey];
            }
        }

        public void UnsubScribeQueue(string deviceKey, string queueName)
        {
            var hasVal = _queues.TryGetValue(queueName, out var devices);
            if (hasVal && devices.Contains(deviceKey))
            {
                devices.Remove(deviceKey);
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
