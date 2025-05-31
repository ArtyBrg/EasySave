using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasySave.Models;
using EasySave.Services;

namespace EasySaveClient.ViewModels
{
    // MainViewModel for the EasySave client application
    public class MainViewModel : INotifyPropertyChanged
    {
        // Import for allocating a console window (for debugging)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        // Collection of backup states received from the server
        public ObservableCollection<BackupState> BackupStates { get; set; } = new();

        // TCP client and network stream for server communication
        private TcpClient _client;
        private NetworkStream _stream;

        // Progress value for UI binding
        private double _progress;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        // Commands for controlling backup jobs
        public ICommand PauseCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }

        // Constructor initializes commands and connects to the server
        public MainViewModel()
        {
            PauseCommand = new RelayCommand(param =>
            {
                var jobName = param as string;
                if (!string.IsNullOrEmpty(jobName))
                    SendCommandToServer(jobName, "Pause");
            });

            StopCommand = new RelayCommand(param =>
            {
                var jobName = param as string;
                if (!string.IsNullOrEmpty(jobName))
                    SendCommandToServer(jobName, "Stop");
            });

            PlayCommand = new RelayCommand(param =>
            {
                var jobName = param as string;
                if (!string.IsNullOrEmpty(jobName))
                    SendCommandToServer(jobName, "Play");
            });

            ConnectToServer();

            AllocConsole();
        }

        // Connects to the remote server and listens for state updates
        private async void ConnectToServer()
        {
            try
            {
                _client = new TcpClient("127.0.0.1", 8080);
                _stream = _client.GetStream();

                using var reader = new System.IO.StreamReader(_stream, Encoding.UTF8);

                while (true)
                {
                    var line = await reader.ReadLineAsync(); // Reads until \n
                    if (line == null) break;

                    Console.WriteLine("Données reçues : " + line);

                    try
                    {
                        var message = JsonSerializer.Deserialize<NetworkMessage>(line);
                        if (message.Type == "StateUpdate")
                        {
                            var payloadText = message.Payload.GetRawText();

                            // Check if payload is a list or a single object
                            if (payloadText.TrimStart().StartsWith("["))
                            {
                                var states = JsonSerializer.Deserialize<List<BackupState>>(payloadText);
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    BackupStates.Clear();
                                    foreach (var state in states)
                                        BackupStates.Add(state);
                                });
                            }
                            else
                            {
                                var state = JsonSerializer.Deserialize<BackupState>(payloadText);
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    var existing = BackupStates.FirstOrDefault(s => s.Name == state.Name);
                                    if (existing == null)
                                        BackupStates.Add(state);
                                    else
                                    {
                                        // Update properties if already present
                                        existing.State = state.State;
                                        existing.Progress = state.Progress;
                                        existing.SourcePath = state.SourcePath;
                                        existing.TargetPath = state.TargetPath;
                                        existing.TotalFiles = state.TotalFiles;
                                        existing.TotalSize = state.TotalSize;
                                        existing.FilesRemaining = state.FilesRemaining;
                                        existing.RemainingSize = state.RemainingSize;
                                        existing.CurrentSourceFile = state.CurrentSourceFile;
                                        existing.CurrentTargetFile = state.CurrentTargetFile;
                                        existing.Timestamp = state.Timestamp;
                                    }
                                });
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show("Erreur JSON : " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur de connexion : " + ex.Message);
            }
        }

        // Helper method for property change notification
        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Raises the PropertyChanged event
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Handler for pause button click in the UI
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as FrameworkElement)?.DataContext as BackupState;
            if (state != null)
                SendCommandToServer(state.Name, "Pause");
        }

        // Handler for stop button click in the UI
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as FrameworkElement)?.DataContext as BackupState;
            if (state != null)
                SendCommandToServer(state.Name, "Stop");
        }

        // Handler for play button click in the UI
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as FrameworkElement)?.DataContext as BackupState;
            if (state != null)
                SendCommandToServer(state.Name, "Play");
        }

        // Sends a command (Pause, Play, Stop) to the server for a specific job
        public void SendCommandToServer(string jobName, string action)
        {
            var message = new NetworkMessage
            {
                Type = "Command",
                Payload = JsonSerializer.SerializeToElement(new
                {
                    JobName = jobName,
                    Action = action
                })
            };

            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);

            Console.WriteLine($"Envoi commande : {action} sur {jobName}");

            try
            {
                if (_client != null && _client.Connected)
                    _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur envoi commande : {ex.Message}");
            }
        }
    }
}

