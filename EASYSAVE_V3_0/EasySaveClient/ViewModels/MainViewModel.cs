using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using EasySave.Models;   
using EasySave.Services;


namespace EasySaveClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<BackupState> BackupStates { get; set; } = new();

        private TcpClient _client;
        private NetworkStream _stream;

        private double _progress;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public MainViewModel()
        {
            ConnectToServer();
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
                    var line = await reader.ReadLineAsync(); // Lis jusqu’au \n
                    if (line == null) break;

                    Console.WriteLine("Données reçues : " + line);

                    try
                    {
                        var message = JsonSerializer.Deserialize<NetworkMessage>(line);
                        if (message.Type == "StateUpdate")
                        {
                            var payloadText = message.Payload.GetRawText();

                            // Vérifie s’il s’agit d’un tableau (liste) ou d’un seul objet
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
                                        // Mise à jour manuelle des propriétés si déjà présent
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
    }


}
