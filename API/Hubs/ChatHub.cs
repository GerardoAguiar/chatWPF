using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Hubs
{
    public class ChatHub : Hub
    {
        // Lista de usuarios conectados (compartida entre todas las instancias del Hub)
        private static ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // Método para enviar mensajes
        public async Task SendMessage(string user, string message)
        {
            _logger.LogInformation($"Mensaje recibido de {user}: {message}");
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // Método para notificar cuando un usuario se conecta
        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            string username = Context.GetHttpContext().Request.Query["username"];

            if (!string.IsNullOrEmpty(username))
            {
                _users[connectionId] = username;
                _logger.LogInformation($"Usuario conectado: {username}");
                await Clients.All.SendAsync("UserConnected", username); // Enviar evento UserConnected
            }

            await base.OnConnectedAsync();
        }

        // Método para notificar cuando un usuario se desconecta
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;

            if (_users.TryRemove(connectionId, out string username))
            {
                _logger.LogInformation($"Usuario desconectado: {username}");
                await Clients.All.SendAsync("UserDisconnected", username); // Enviar evento UserDisconnected
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Método para actualizar la lista de usuarios conectados
        private async Task UpdateUserList()
        {
            var userList = _users.Values.ToList();
            _logger.LogInformation($"Actualizando lista de usuarios: {string.Join(", ", userList)}");
            await Clients.All.SendAsync("UpdateUserList", userList); // Enviar evento UpdateUserList
        }
    }
}
