using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.Diagnostics;


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
                Debug.WriteLine($"Evento ReceiveMessage recibido: {user}: {message}");
                Dispatcher.Invoke(() =>
                {
                    MessageListBox.Items.Add($"{user}: {message}");
                });
            });

            // Escuchar cuando un usuario se conecta
            _connection.On<string>("UserConnected", (user) =>
            {
                Debug.WriteLine($"Evento UserConnected recibido: {user}");
                Dispatcher.Invoke(() =>
                {
                    MessageListBox.Items.Add($"{user} se ha conectado.");
                });
            });

            // Escuchar cuando un usuario se desconecta
            _connection.On<string>("UserDisconnected", (user) =>
            {
                Debug.WriteLine($"Evento UserDisconnected recibido: {user}");
                Dispatcher.Invoke(() =>
                {
                    MessageListBox.Items.Add($"{user} se ha desconectado.");
                });
            });

            // Escuchar actualizaciones de la lista de usuarios conectados
            _connection.On<List<string>>("UpdateUserList", (userList) =>
            {
                Debug.WriteLine($"Evento UpdateUserList recibido: {string.Join(", ", userList)}");
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
                Debug.WriteLine("Conectado al servidor.");
                MessageListBox.Items.Add("Conectado al servidor.");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error al conectar: {ex.Message}");
                MessageListBox.Items.Add($"Error: {ex.Message}");
            }
        }



        private void ConfirmUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Por favor, ingresa un nombre de usuario válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                _username = username;
                UsernameTextBox.IsEnabled = false;
                ConfirmUsernameButton.IsEnabled = false;
                MessageTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;
                MessageListBox.Items.Add($"Nombre de usuario confirmado: {_username}");

                // Enviar el nombre de usuario al servidor
                Debug.WriteLine($"Enviando nombre de usuario al servidor: {_username}");
                _connection.SendAsync("SetUsername", _username);
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
    }
}