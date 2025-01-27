using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    public class ChatHub : Hub
    {
        // Método para enviar mensajes a todos los clientes conectados
        public async Task SendMessage(string user, string message)
        {
            // Envía el mensaje a todos los clientes
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
