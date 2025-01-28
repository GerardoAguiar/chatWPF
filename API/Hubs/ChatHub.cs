using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Hubs
{
    public class ChatHub : Hub
    {
        // Lista de usuarios conectados (compartida entre todas las instancias del Hub)
        private static ConcurrentDictionary<string, (string Username, string ConnectionTime)> _users = new ConcurrentDictionary<string, (string, string)>();
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // Método para enviar mensajes
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // Método para notificar cuando un usuario se conecta
        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            string username = Context.GetHttpContext().Request.Query["username"];

            if (!string.IsNullOrEmpty(username))
            {
                // Registrar la hora y fecha de conexión
                string connectionTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                _users[connectionId] = (username, connectionTime);

                await Clients.All.SendAsync("UserConnected", username, connectionTime); // Enviar evento UserConnected con la hora
                await UpdateUserList(); // Actualizar la lista de usuarios conectados
            }

            await base.OnConnectedAsync();
        }

        // Método para notificar cuando un usuario se desconecta
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;

            if (_users.TryRemove(connectionId, out var userInfo))
            {
                string username = userInfo.Username;
                string connectionTime = userInfo.ConnectionTime;
                string disconnectTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                await Clients.All.SendAsync("UserDisconnected", username, connectionTime, disconnectTime); // Enviar evento UserDisconnected con la hora de desconexión
                await UpdateUserList(); // Actualizar la lista de usuarios conectados
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Método para actualizar la lista de usuarios conectados
        private async Task UpdateUserList()
        {
            var userList = _users.Values.Select(u => $"{u.Username}").ToList();
            await Clients.All.SendAsync("UpdateUserList", userList); // Enviar evento UpdateUserList con la hora de conexión
        }

        // Método para recibir y establecer el nombre de usuario
        public async Task SetUsername(string username)
        {
            string connectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(username))
            {
                // Guardamos el nombre de usuario y la hora de conexión
                string connectionTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                _users[connectionId] = (username, connectionTime);

                // Enviar evento a todos los clientes sobre el nuevo usuario conectado
                await Clients.All.SendAsync("UserConnected", username, connectionTime);

                // Actualizamos la lista de usuarios conectados
                await UpdateUserList();
            }
        }
    }
}
