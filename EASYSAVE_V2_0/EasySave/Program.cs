using System;
using System.Linq;
using System.Windows;
using System.Runtime.InteropServices;

namespace EasySave_WPF
{
    public static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);

                Console.WriteLine("=== Mode Console détecté ===");
                Console.WriteLine("Arguments : " + string.Join(", ", args));

                var consoleApp = new ConsoleApp();
                consoleApp.Run(args);

                Console.WriteLine("Terminé. Appuyez sur une touche pour quitter.");
                Console.ReadKey();
            }
            else
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }


    }

}
