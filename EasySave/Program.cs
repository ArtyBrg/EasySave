using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Programme principal

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== EasySave Backup Application ===");
            Console.WriteLine("Version 1.0\n");

            try
            {
                var app = new ConsoleApp();
                app.Run();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Application crashed: {ex}");
                Console.WriteLine("A critical error occurred. The application will now close.");
                Console.WriteLine("Error details have been logged.");
            }
        }
    }
}