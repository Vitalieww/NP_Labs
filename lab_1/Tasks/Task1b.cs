namespace Lab1.Tasks;

using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using System.Text;
using Lab1.Common;

public static class Task1b
{
    public static int[] LoadNumbers(string filePath)
    {
        Console.WriteLine("Loading numbers.txt into memory (not measured) ...");
        int[] numbers = new int[DataGenerator.TotalNumbers];
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
        using var reader = new StreamReader(fs, Encoding.UTF8);
        int index = 0;
        string? line;
        while ((line = reader.ReadLine()) != null && index < numbers.Length)
        {
            if (int.TryParse(line, out int val))
            {
                numbers[index++] = val;
            }

        }
        Console.WriteLine($"Loaded {index:N0} numbers into RAM.");
        return numbers;
    }

    public static CounterResult CountUnsynchronized(int[] numbers, int threadCount)
    {
        long neg = 0, pos = 0, zero = 0;

        int chunkSize = numbers.Length / threadCount;
        Thread[] threads = new Thread[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            int threadIndex = t;
            int start = threadIndex * chunkSize;
            int end = (threadIndex == threadCount - 1) ? numbers.Length
            : (threadIndex + 1) * chunkSize;

            threads[t] = new Thread(() =>
            {
                for (int i = start; i < end; i++)
                {
                    int val = numbers[i];
                    if (val < 0) neg++;
                    else if (val > 0) pos++;
                    else zero++;
                }

            });
            threads[t].Start();
        }

        for (int t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }
        return new CounterResult(neg, pos, zero);
    }

    public static CounterResult CountWithLock(int[] numbers, int threadCount)
    {
        long neg = 0, pos = 0, zero = 0;

        object lockObj = new object();
        int chunkSize = numbers.Length / threadCount;
        Thread[] threads = new Thread[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            int threadIndex = t;
            int start = threadIndex * chunkSize;
            int end = (threadIndex == threadCount - 1) ? numbers.Length
            : (threadIndex + 1) * chunkSize;

            threads[t] = new Thread(() =>
            {
                for (int i = start; i < end; i++)
                {
                    int val = numbers[i];
                    lock (lockObj)
                    {
                        if (val < 0) neg++;
                        else if (val > 0) pos++;
                        else zero++;
                    }
                }
            });
            threads[t].Start();
        }

        for (int t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }
        return new CounterResult(neg, pos, zero);
    }
    public static CounterResult CountWithLocalCounter(int[] numbers, int threadCount)
    {
        int chunkSize = numbers.Length / threadCount;
        Thread[] threads = new Thread[threadCount];
        CounterResult[] results = new CounterResult[threadCount];

        for (int t = 0; t < threadCount; t++)
        {
            int threadIndex = t;
            int start = threadIndex * chunkSize;
            int end = (threadIndex == threadCount - 1) ? numbers.Length : (threadIndex + 1) * chunkSize;

            threads[t] = new Thread(() =>
            {
                long localNeg = 0, localPos = 0, localZero = 0;
                for (int i = start; i < end; i++)
                {
                    int val = numbers[i];
                    if (val < 0) localNeg++;
                    else if (val > 0) localPos++;
                    else localZero++;
                }
                results[threadIndex] = new CounterResult(localNeg, localPos, localZero);
            });
            threads[t].Start();
        }
        for (int t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }

        CounterResult total = new(0, 0, 0);

        foreach (var r in results)
        {
            total += r;
        }
        return total;
    }

    public static void Run(string filePath = "numbers.txt")
    {
        var numbers = LoadNumbers(filePath);
        int threadCount = Environment.ProcessorCount;
        Console.WriteLine($"\nRunning Task 1b with {threadCount} threads (CPU logical processors: {threadCount})...\n");
        // Version 1: Run 3 times to observe non-deterministic race conditions
        for (int i = 1; i <= 3; i++)
        {
            Metric.Measure($"Task 1b: Version 1 (Unsynchronized) - Run {i}", () => CountUnsynchronized(numbers, threadCount));
        }

        // Version 2: Lock protection
        Metric.Measure("Task 1b: Version 2 (Lock)", () => CountWithLock(numbers, threadCount));
        
        // Version 3: Separate per-thread counters
        Metric.Measure("Task 1b: Version 3 (Local counters)", () => CountWithLocalCounter(numbers, threadCount));
    }
}
