using Bridge.Html5;
using Core.Extensions;
using Core.Models;
using System;

namespace Core.Clients
{
    public class WebSocketClient
    {
        private readonly WebSocket socket;
        public string deviceKey;
        public WebSocketClient(string url)
        {
            var wsUri = $"wss://{Client.Host}/{url}?access_token=" + Client.Token?.AccessToken;
            /*@
            if (typeof(ReconnectingWebSocket) !== 'undefined') {
                this.socket = new ReconnectingWebSocket(wsUri);
                this.socket.onopen = Bridge.fn.combine(this.socket.onopen, function (e) {
                    System.Console.WriteLine(System.String.format("Socket opened", e));
                });

                this.socket.onclose = Bridge.fn.combine(this.socket.onclose, function (e) {
                    System.Console.WriteLine(System.String.format("Socket closed", e));
                });

                this.socket.onerror = Bridge.fn.combine(this.socket.onerror, function (e) {
                    System.Console.WriteLine(e);
                });

                this.socket.onmessage = Bridge.fn.combine(this.socket.onmessage, Bridge.fn.cacheBind(this, this.SocketMessageEvent));
                this.socket.binaryType = "arraybuffer";
                return;
            }
            */
            socket = new WebSocket(wsUri);
            socket.OnOpen += e =>
            {
                Console.WriteLine("Socket opened", e);
            };

            socket.OnClose += (CloseEvent e) =>
            {
                Console.WriteLine("Socket closed", e);
            };

            socket.OnError += (e) =>
            {
                Console.WriteLine(e);
            };

            socket.OnMessage += SocketMessageEvent;
            socket.BinaryType = WebSocket.DataType.ArrayBuffer;
        }

        private void SocketMessageEvent(MessageEvent e)
        {
            var responseStr = e.Data.ToString();
            var objRs = responseStr.Parse<MQEvent>();
            if (objRs is null)
            {
                deviceKey = responseStr;
                return;
            }
            Window.DispatchEvent(new CustomEvent(objRs.QueueName, new CustomEventInit() { Detail = objRs }));
        }

        public void Send(string message)
        {
            socket.Send(message);
        }

        public void Close() => socket.Close();
    }
}
