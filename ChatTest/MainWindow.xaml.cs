using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatTest
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;
        private string _username;
        private string _currentRoom = "General"; // Sala por defecto

        public MainWindow()
        {
            InitializeComponent();
            InitializeSignalR();
            this.Closing += MainWindow_Closing; // Agrega el evento para manejar el cierre de la ventana
        }


        private async void InitializeSignalR()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7032/chatHub")
                .Build();

            // Escuchar mensajes de la sala actual
            _connection.On<string, string, string>("ReceiveMessage", (room, user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (room == _currentRoom)
                    {
                        MessageListBox.Items.Add($"{user}: {message}");
                    }
                });
            });

            // Escuchar cuando un usuario se conecta a la sala
            _connection.On<string, string, string>("UserConnected", (room, user, connectionTime) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (room == _currentRoom)
                    {
                        MessageListBox.Items.Add($"The {user} joined at {connectionTime}");
                        UpdateUserList();
                    }
                });
            });

            // Escuchar cuando un usuario se desconecta
            _connection.On<string, string, string, string>("UserDisconnected", (room, user, connectionTime, disconnectTime) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (room == _currentRoom)
                    {
                        MessageListBox.Items.Add($"The {user} left. Disconnected at {disconnectTime}");
                        UpdateUserList();
                    }
                });
            });

            // Actualizar lista de usuarios en la sala activa
            _connection.On<string, List<string>>("UpdateUserList", (room, userList) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (room == _currentRoom)
                    {
                        UserListBox.Items.Clear();
                        foreach (var user in userList)
                        {
                            UserListBox.Items.Add(user);
                        }
                    }
                });
            });

            try
            {
                await _connection.StartAsync();
                MessageListBox.Items.Add("Connected to the server.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al conectar: {ex.Message}");
                MessageListBox.Items.Add($"Error: {ex.Message}");
            }
        }

        private async void ConfirmUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();

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
                MessageListBox.Items.Add($"Username confirmed: {_username}");

                if (_connection.State == HubConnectionState.Connected)
                {
                    await _connection.SendAsync("SetUsername", _username, _currentRoom);
                }
                else
                {
                    MessageListBox.Items.Add("Error: No connection to the server.");
                }
            }
        }

        private async void RoomComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_username == null) return; // No permitir cambio de sala si no hay usuario registrado

            string newRoom = ((System.Windows.Controls.ComboBoxItem)RoomComboBox.SelectedItem).Content.ToString();

            if (newRoom != _currentRoom)
            {
                await _connection.SendAsync("ChangeRoom", _username, _currentRoom, newRoom);
                _currentRoom = newRoom;
                MessageListBox.Items.Clear();
                MessageListBox.Items.Add($"Switched to room: {_currentRoom}");

                UpdateUserList();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    await _connection.InvokeAsync("SendMessage", _currentRoom, _username, message);
                    MessageTextBox.Clear();
                }
                catch (Exception ex)
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

        private async void UpdateUserList()
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.SendAsync("RequestUserList", _currentRoom);
            }
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Mostrar un cuadro de diálogo de confirmación
            var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // Cancelar el cierre de la ventana si el usuario elige "No"
                return;
            }

            // Desconectar de SignalR si está conectado
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
                MessageListBox.Items.Add("Disconnected from the server.");
            }
        }

    }
}
