using System;
using System.Text.Json;
using System.Diagnostics;

// Logger DLL (externe)

namespace EasySave
{
    public static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }

        public static void LogError(string errorMessage)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errorMessage}");
        }
    }
}
