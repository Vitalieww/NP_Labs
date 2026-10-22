namespace Lab1.Tasks;

using System.Text;
using Lab1.Common;

public static class Task1a
{
    public static CounterResult Run(string filePath, int threadCount)
    {
        long fileSize = new FileInfo(filePath).Length;
        long[] offsets = new long[threadCount + 1];
        offsets[0] = 0;
        offsets[threadCount] = fileSize;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // If file starts with UTF-8 BOM (3 bytes), skip it so byte counts align perfectly
        if (fileSize >= 3 && fs.ReadByte() == 0xEF && fs.ReadByte() == 0xBB && fs.ReadByte() == 0xBF)
        {
            offsets[0] = 3;
        }

        for (int i = 1; i < threadCount; i++)
        {
            long rough = i * (fileSize / threadCount);
            fs.Seek(rough, SeekOrigin.Begin);
            while (fs.Position < fileSize && fs.ReadByte() != '\n') { }
            offsets[i] = fs.Position;
        }
        fs.Dispose();

        Thread[] threads = new Thread[threadCount];
        CounterResult[] results = new CounterResult[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            int index = i;
            threads[i] = new Thread(() =>
            {
                results[index] = ProcessPart(filePath, offsets[index], offsets[index + 1]);
            });
            threads[i].Start();
        }

        for (int i = 0; i < threadCount; i++)
        {
            threads[i].Join();
        }

        CounterResult total = new(0, 0, 0);
        foreach (var r in results)
        {
            total += r;
        }

        return total;
    }


    private static CounterResult ProcessPart(string filePath, long startOffSet, long endOffSet)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
        fs.Seek(startOffSet, SeekOrigin.Begin);
        using var reader = new StreamReader(fs, Encoding.UTF8, bufferSize: 65536);

        long bytesToRead = endOffSet - startOffSet;
        long bytesRead = 0;
        long neg = 0, pos = 0, zero = 0;

        string? line;
        while (bytesRead < bytesToRead && (line = reader.ReadLine()) != null)
        {
            bytesRead += line.Length + 2;

            if (int.TryParse(line, out int number))
            {
                if (number < 0) neg++;
                else if (number > 0) pos++;
                else zero++;
            }
        }

        return new CounterResult(neg, pos, zero);
    }

}