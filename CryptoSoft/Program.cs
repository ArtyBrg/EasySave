using System;
using System.Threading;

namespace CryptoSoft
{
    public static class Program
    {
        private static readonly string mutexName = "Global\\CryptoSoft_Mutex";

        public static void Main(string[] args)
        {
            bool createdNew;

            // Création d’un mutex système global
            using (var mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (!createdNew)
                {
                    Console.WriteLine("An other CryptoSoft instance is already running.");
                    Environment.Exit(-1); // code d’erreur mono-instance
                }

                try
                {
                    foreach (var arg in args)
                    {
                        Console.WriteLine(arg);
                    }

                    var fileManager = new FileManager(args[0], args[1]);
                    int elapsedTime = fileManager.TransformFile();
                    Environment.Exit(elapsedTime);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error : " + e.Message);
                    Environment.Exit(-99); // code d’erreur général
                }
            }
        }
    }
}
