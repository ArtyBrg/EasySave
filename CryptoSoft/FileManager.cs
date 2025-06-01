using System.Diagnostics;
using System.Text;

namespace CryptoSoft;

public class FileManager(string path, string key)
{
    private string FilePath { get; } = path;
    private string Key { get; } = key;

    private const string MutexName = "Global\\CryptoSoft_DLL_Mutex"; // global name shared

    /// check if the file exists
    private bool CheckFile()
    {
        if (File.Exists(FilePath))
            return true;

        Console.WriteLine("File not found.");
        Thread.Sleep(1000);
        return false;
    }

    /// Encrypts the file with xor encryption (mono-instance)
    
    public int TransformFile()
    {
        using var mutex = new Mutex(false, MutexName);

        Console.WriteLine("Waiting for lock...");

        // wait for the mutex available (infinite)
        mutex.WaitOne();

        Console.WriteLine("Lock acquired.");
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            if (!CheckFile()) return -1;

            Console.WriteLine("Simulating long encryption task...");
            // Thread.Sleep(5000); // Simulating long encryption task

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
    /// Decrypts the file with xor encryption (mono-instance)
    private static byte[] ConvertToByte(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }
    // XOR method for encryption/decryption
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
