using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using EasySave.Models;
using EasySave.ViewModels;

namespace EasySave.Services
{
    public class RemoteConsoleService
    {

        private readonly StateService _stateService;
        private BackupManagerViewModel _backupManager;

        private Socket _lastClient;

        public RemoteConsoleService(StateService stateService)
        {
            _stateService = stateService;
        }

        private Socket _serverSocket;
        private bool _isRunning;

        public void Start()
        {
            if (_backupManager == null)
            {
                throw new InvalidOperationException("Le BackupManager doit être initialisé avant de démarrer le serveur.");
            }

            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            _serverSocket.Listen(10);
            _isRunning = true;

            Console.WriteLine("Console distante en écoute sur le port 8080...");

            Thread serverThread = new Thread(() =>
            {
                while (_isRunning)
                {

                    Socket client = _serverSocket.Accept();
                    _lastClient = client;
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            });

            serverThread.IsBackground = true;
            serverThread.Start();
        }

        public void Initialize(BackupManagerViewModel backupManager)
        {
            _backupManager = backupManager;
            Console.WriteLine("BackupManager initialisé dans le serveur");
        }

        private void HandleClient(Socket client)
        {
            try
            {
                _lastClient = client;

                // Envoie l'état initial
                var currentStates = _stateService.GetAllStates();
                var response = JsonSerializer.Serialize(new NetworkMessage
                {
                    Type = "StateUpdate",
                    Payload = JsonSerializer.SerializeToElement(currentStates)
                }) + "\n";
                client.Send(Encoding.UTF8.GetBytes(response));

                byte[] buffer = new byte[4096]; // plus grand pour éviter les coupures

                while (client.Connected)
                {
                    int bytesRec = client.Receive(buffer);
                    if (bytesRec == 0)
                    {
                        Console.WriteLine("Client déconnecté.");
                        break;
                    }

                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                    Console.WriteLine($"Commande reçue : {request}");

                    var command = JsonSerializer.Deserialize<NetworkMessage>(request);

                    if (command.Type == "Command")
                    {
                        var action = command.Payload.GetProperty("Action").GetString();
                        var jobName = command.Payload.GetProperty("JobName").GetString();

                        Console.WriteLine($"Action demandée: {action} pour job: {jobName}");

                        if (_backupManager == null)
                        {
                            Console.WriteLine("❌ _backupManager est NULL !");
                            return;
                        }

                        var job = _backupManager.Jobs.FirstOrDefault(j => j.Name == jobName);
                        if (job == null)
                        {
                            Console.WriteLine($"❌ Job '{jobName}' introuvable !");
                            return;
                        }

                        switch (action.ToLower())
                        {
                            case "pause":
                                Console.WriteLine($"Exécution PauseJob() pour {jobName}");
                                job.PauseJob();
                                Console.WriteLine($"PauseJob() terminé pour {jobName}");
                                break;
                            case "stop":
                                Console.WriteLine($"Exécution StopJob() pour {jobName}");
                                job.StopJob();
                                Console.WriteLine($"StopJob() terminé pour {jobName}");
                                break;
                            default:
                                Console.WriteLine($"Action '{action}' non reconnue");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur client détaillée: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }



        public void SendStateUpdate(BackupState state)
        {
            try
            {
                if (_lastClient == null || !_lastClient.Connected)
                {
                    Console.WriteLine("Aucun client connecté.");
                    return;
                }

                var message = new NetworkMessage
                {
                    Type = "StateUpdate",
                    Payload = JsonSerializer.SerializeToElement(state)
                };

                var json = JsonSerializer.Serialize(message) + "\n";
                // Console.WriteLine("Message envoyé au client : " + json); // <== ICI

                byte[] data = Encoding.UTF8.GetBytes(json);
                _lastClient?.Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur envoi update : " + ex.Message);
            }
        }

    }

    public class NetworkMessage
    {
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }
}
