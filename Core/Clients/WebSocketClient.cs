using Bridge.Html5;
using Core.Models;
using Newtonsoft.Json;
using System;

namespace Core.Clients
{
    public class WebSocketClient
    {
        private readonly WebSocket _socket;
        public WebSocketClient(string url)
        {
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
                var objRs = JsonConvert.DeserializeObject<MQData>(responseStr);
                var queueName = objRs.QueueName;
                Window.DispatchEvent(new CustomEvent(objRs.QueueName, new CustomEventInit() { Detail = objRs }));
            };
            _socket.BinaryType = WebSocket.DataType.ArrayBuffer;
        }

        public void Send(string message)
        {
            _socket.Send(message);
        }

        public void Close() => _socket.Close();
    }
}
