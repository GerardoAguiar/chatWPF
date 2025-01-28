using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Windows.Input;


namespace ChatTest
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;
        private string _username;
        public MainWindow()
        {
            InitializeComponent();
            InitializeSignalR();
        }

        private async void InitializeSignalR()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7032/chatHub")
                .Build();

            // Escuchar mensajes entrantes
            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageListBox.Items.Add($"{user}: {message}");
                });
            });

            // Escuchar cuando un usuario se conecta
            _connection.On<string, string>("UserConnected", (user, connectionTime) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageListBox.Items.Add($"The {user} is connected at {connectionTime}");
                });
            });

            // Escuchar cuando un usuario se desconecta
            _connection.On<string, string, string>("UserDisconnected", (user, connectionTime, disconnectTime) =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageListBox.Items.Add($"The {user} is disconnected. Disconnected at {disconnectTime}");
                });
            });

            // Escuchar actualizaciones de la lista de usuarios conectados
            _connection.On<List<string>>("UpdateUserList", (userList) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UserListBox.Items.Clear();
                    foreach (var user in userList)
                    {
                        UserListBox.Items.Add(user);
                    }
                });
            });

            try
            {
                await _connection.StartAsync();
                MessageListBox.Items.Add("Connected to the server.");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error al conectar: {ex.Message}");
                MessageListBox.Items.Add($"Error: {ex.Message}");
            }
        }



        private async void ConfirmUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter a valid username.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                _username = username;
                UsernameTextBox.IsEnabled = false;
                ConfirmUsernameButton.IsEnabled = false;
                MessageTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;
                MessageListBox.Items.Add($"Username confirm: {_username}");

                // Enviar el nombre de usuario al servidor solo después de que la conexión esté establecida
                if (_connection.State == HubConnectionState.Connected)
                {
                    await _connection.SendAsync("SetUsername", _username);
                }
                else
                {
                    MessageListBox.Items.Add("Error: No connection to the server has been established.");
                }
            }
        }


        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text;

            if (!string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    await _connection.InvokeAsync("SendMessage", _username, message);
                    MessageTextBox.Clear();
                }
                catch (System.Exception ex)
                {
                    MessageListBox.Items.Add($"Error: {ex.Message}");
                }
            }
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
            }
        }
    }
}