namespace Lab1.Common;

using System.Diagnostics;
using System.IO;
using System.Text;

public static class DataGenerator
{
    public const int TotalNumbers = 50_000_000;
    public const int MinValue = -1_000_000;
    public const int MaxValue = 1_000_000;
    public const int PartCount = 8;
    public const int NumbersPerPart = TotalNumbers / PartCount; // 6,250,000

    public static void GenerateNumbers(string filePath = "numbers.txt")
    {
        Console.WriteLine($"Generating {TotalNumbers:N0} numbers into '{filePath}'...");
        var sw = Stopwatch.StartNew();

        // Use a 64 KB buffer for high I/O write throughput
        using (var writer = new StreamWriter(filePath, append: false, Encoding.UTF8, bufferSize: 65536))
        {
            var rng = Random.Shared;

            for (int i = 0; i < TotalNumbers; i++)
            {
                // Random.Next upper bound is exclusive, so MaxValue + 1 ensures 1,000,000 is included
                int number = rng.Next(MinValue, MaxValue + 1);
                writer.WriteLine(number);
            }
        }

        sw.Stop();
        var fileInfo = new FileInfo(filePath);
        Console.WriteLine($"Finished in {sw.Elapsed.TotalSeconds:F2}s. File size: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
    }

    public static void SplitFile(string sourcePath = "numbers.txt", string outputDirectory = "parts")
    {
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"Source file '{sourcePath}' not found. Generating it first...");
            GenerateNumbers(sourcePath);
        }

        Directory.CreateDirectory(outputDirectory);
        Console.WriteLine($"Splitting '{sourcePath}' into {PartCount} files of {NumbersPerPart:N0} numbers each...");
        var sw = Stopwatch.StartNew();

        // 64 KB buffer for fast sequential reading
        using var reader = new StreamReader(sourcePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 65536);

        for (int part = 0; part < PartCount; part++)
        {
            string partPath = Path.Combine(outputDirectory, $"part_{part + 1}.txt");
            using var writer = new StreamWriter(partPath, append: false, Encoding.UTF8, bufferSize: 65536);

            for (int i = 0; i < NumbersPerPart; i++)
            {
                string? line = reader.ReadLine();
                if (line == null) break;
                writer.WriteLine(line);
            }
        }

        sw.Stop();
        Console.WriteLine($"Split completed in {sw.Elapsed.TotalSeconds:F2}s in directory '{outputDirectory}/'.");
    }
}
