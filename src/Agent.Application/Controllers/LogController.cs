using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;

namespace Agent.Application.Controllers
{
    public class LogController : BaseController
    {
        private readonly ILogger<LogController> _logger;

        public LogController(ILogger<LogController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/[controller]/{service}")]
        [AllowAnonymous]
        public async Task Status([FromRoute] string service)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                using var process = new ObservableProcess("journalctl", $" -u {service} -f -n 200");
                try
                {
                    process.Start();
                    await Echo(websocket, process.StandardOutput);
                }
                catch (Exception e)
                {
                    _logger.LogError($"{e}");
                }
                finally
                {
                    process.Stop(true);
                }
            }
        }

        private async Task Echo(WebSocket webSocket, Subject<string> standardOuput)
        {
            standardOuput.Subscribe(async data =>
            {
                var byteArray = Encoding.UTF8.GetBytes(data);
                await webSocket.SendAsync(new ArraySegment<byte>(byteArray, 0, byteArray.Length), WebSocketMessageType.Text, true, HttpContext.RequestAborted);
            });


            var receivedBuffer = new byte[1024 * 4];
            var receiveMessage = await webSocket.ReceiveAsync(new ArraySegment<byte>(receivedBuffer), HttpContext.RequestAborted);
            while (!receiveMessage.CloseStatus.HasValue)
            {

            }
            await webSocket.CloseAsync(receiveMessage.CloseStatus.Value, receiveMessage.CloseStatusDescription, HttpContext.RequestAborted);
        }
    }


}
