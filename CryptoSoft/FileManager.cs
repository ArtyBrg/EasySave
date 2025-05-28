using System.Diagnostics;
using System.Text;

namespace CryptoSoft;

public class FileManager(string path, string key)
{
    private string FilePath { get; } = path;
    private string Key { get; } = key;

    private const string MutexName = "Global\\CryptoSoft_DLL_Mutex"; // nom global partagé

    /// <summary>
    /// check if the file exists
    /// </summary>
    private bool CheckFile()
    {
        if (File.Exists(FilePath))
            return true;

        Console.WriteLine("File not found.");
        Thread.Sleep(1000);
        return false;
    }

    /// <summary>
    /// Encrypts the file with xor encryption (mono-instance)
    /// </summary>
    public int TransformFile()
    {
        using var mutex = new Mutex(false, MutexName);

        Console.WriteLine("Waiting for lock...");

        // Attendre que le mutex soit disponible (infini)
        mutex.WaitOne();

        Console.WriteLine("Lock acquired.");
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            if (!CheckFile()) return -1;

            Console.WriteLine("Simulating long encryption task...");
            Thread.Sleep(5000); // Simule une tâche longue

            var fileBytes = File.ReadAllBytes(FilePath);
            var keyBytes = ConvertToByte(Key);
            fileBytes = XorMethod(fileBytes, keyBytes);
            File.WriteAllBytes(FilePath, fileBytes);

            stopwatch.Stop();
            Console.WriteLine("Task complete.");
            return (int)stopwatch.ElapsedMilliseconds;
        }
        finally
        {
            mutex.ReleaseMutex();
            Console.WriteLine("Lock released.");
        }
    }

    private static byte[] ConvertToByte(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }

    private static byte[] XorMethod(IReadOnlyList<byte> fileBytes, IReadOnlyList<byte> keyBytes)
    {
        var result = new byte[fileBytes.Count];
        for (var i = 0; i < fileBytes.Count; i++)
        {
            result[i] = (byte)(fileBytes[i] ^ keyBytes[i % keyBytes.Count]);
        }

        return result;
    }
}
