using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace CryptoSoft
{
    public static class Program
    {
        private static readonly string mutexName = "Global\\CryptoSoft_Mutex";
        private static readonly string pipeName = "CryptoSoftPipe";

        public static void Main(string[] args)
        {
            bool createdNew;

            using (var mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (createdNew)
                {
                    // First instance ->  start of the pipe server
                    Console.WriteLine("Instance principale de CryptoSoft lancée.");
                    StartPipeServer(); // Start the server
                    WaitForExit();     // Wait for a manual interruption
                }
                else
                {

                    // An onther instance is already running
                    Console.WriteLine("Instance secondaire détectée. Envoi des arguments à l'instance principale.");
                    SendArgumentsToMainInstance(args);
                }
            }
        }
        // Start the named pipe server to listen for requests
        private static void StartPipeServer()
        {
            Thread serverThread = new Thread(() =>
            {
                while (true)
                {
                    using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In))
                    using (var reader = new StreamReader(server))
                    {
                        server.WaitForConnection();

                        string? filePath = reader.ReadLine();
                        string? key = reader.ReadLine();

                        if (filePath != null && key != null)
                        {
                            Console.WriteLine($"Demande reçue : {filePath}");
                            var fileManager = new FileManager(filePath, key);
                            int time = fileManager.TransformFile();
                            Console.WriteLine($"Fichier traité en {time} ms");
                        }
                        else
                        {
                            Console.WriteLine("Arguments invalides reçus.");
                        }

                        server.Disconnect();
                    }
                }
            });
            // Run the server thread in the background
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        // Send arguments to the main instance via named pipe
        private static void SendArgumentsToMainInstance(string[] args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out)) // connect to the named pipe server
                {
                    client.Connect(2000); // 2 sec timeout
                    using (var writer = new StreamWriter(client))
                    {
                        writer.AutoFlush = true;

                        if (args.Length < 2)
                        {
                            Console.WriteLine("Pas assez d’arguments pour le chiffrement.");
                            Environment.Exit(-2);
                        }

                        writer.WriteLine(args[0]); // FilePath
                        writer.WriteLine(args[1]); // Key
                    }

                    Console.WriteLine("Arguments transmis avec succès.");
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur d’envoi des arguments : " + ex.Message);
                Environment.Exit(-3);
            }
        }

        // Wait for a manual interruption to exit the application
        private static void WaitForExit()
        {
            Console.WriteLine("Appuyez sur Ctrl+C pour quitter...");
            ManualResetEvent quitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                quitEvent.Set();
            };
            quitEvent.WaitOne();
        }
    }
}
