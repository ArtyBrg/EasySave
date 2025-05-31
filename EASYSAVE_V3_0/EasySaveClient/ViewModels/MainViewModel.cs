using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    public class MainViewModel : INotifyPropertyChanged
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        public ObservableCollection<BackupState> BackupStates { get; set; } = new();

        private TcpClient _client;
        private NetworkStream _stream;

        private double _progress;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public ICommand PauseCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }

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

            // AllocConsole();
        }

        private async void ConnectToServer()
        {
            try
            {
                _client = new TcpClient("127.0.0.1", 8080);
                _stream = _client.GetStream();

                using var reader = new System.IO.StreamReader(_stream, Encoding.UTF8);

                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    Console.WriteLine("Data received: " + line);

                    try
                    {
                        var message = JsonSerializer.Deserialize<NetworkMessage>(line);
                        if (message.Type == "StateUpdate")
                        {
                            var payloadText = message.Payload.GetRawText();

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
                        ShowErrorAndExit("JSON parsing error: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorAndExit("Connection error: " + ex.Message);
            }
        }


        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as FrameworkElement)?.DataContext as BackupState;
            if (state != null)
                SendCommandToServer(state.Name, "Pause");
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as FrameworkElement)?.DataContext as BackupState;
            if (state != null)
                SendCommandToServer(state.Name, "Stop");
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var state = (sender as FrameworkElement)?.DataContext as BackupState;
            if (state != null)
                SendCommandToServer(state.Name, "Play");
        }


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

        private void ShowErrorAndExit(string message)
        {
            // Assure d'appeler depuis le thread UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            });
        }

    }
}
