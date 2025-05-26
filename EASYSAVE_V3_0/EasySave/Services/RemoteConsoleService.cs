using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using EasySave.Models;

namespace EasySave.Services
{
    public class RemoteConsoleService
    {

        private readonly StateService _stateService;

        private Socket _lastClient;

        public RemoteConsoleService(StateService stateService)
        {
            _stateService = stateService;
        }

        private Socket _serverSocket;
        private bool _isRunning;

        public void Start()
        {
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

        private void HandleClient(Socket client)
        {
            try
            {
                _lastClient = client;

                // ⬇️ Envoie immédiatement l'état complet au client
                var currentStates = _stateService.GetAllStates();
                var response = JsonSerializer.Serialize(new NetworkMessage
                {
                    Type = "StateUpdate",
                    Payload = JsonSerializer.SerializeToElement(currentStates)
                }) + "\n";

                client.Send(Encoding.UTF8.GetBytes(response));

                // ⬇️ Optionnel : lire ensuite une commande du client
                byte[] buffer = new byte[1024];
                int bytesRec = client.Receive(buffer);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRec);

                Console.WriteLine($"Commande reçue : {request}");

                var command = JsonSerializer.Deserialize<NetworkMessage>(request);

                if (command.Type == "Command")
                {
                    var action = command.Payload.GetProperty("Action").GetString();
                    var jobName = command.Payload.GetProperty("JobName").GetString();
                    Console.WriteLine($"Action sur {jobName} : {action}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur client : " + ex.Message);
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
                Console.WriteLine("Message envoyé au client : " + json); // <== ICI

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
