namespace Lab1.Tasks;

using System.IO;
using Lab1.Common;

public static class Task2
{
    public static CounterResult Run(string directoryPath, int threadCount)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory '{directoryPath}' not found.");

        string[] files = Directory.GetFiles(directoryPath, "part_*.txt");
        if (files.Length == 0)
            files = Directory.GetFiles(directoryPath, "*.txt");
        Array.Sort(files);

        Thread[] threads = new Thread[threadCount];
        CounterResult[] results = new CounterResult[threadCount];
        int chunkSize = files.Length / threadCount;

        for (int t = 0; t < threadCount; t++)
        {
            int threadIndex = t;
            int start = threadIndex * chunkSize;
            int end = (threadIndex == threadCount - 1) ? files.Length : (threadIndex + 1) * chunkSize;

            threads[t] = new Thread(() =>
            {
                CounterResult threadTotal = new(0, 0, 0);
                for (int i = start; i < end; i++)
                {
                    threadTotal += ProcessFile(files[i]);
                }
                results[threadIndex] = threadTotal;
            });
            threads[t].Start();
        }
        for (int t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }
        CounterResult result = new CounterResult(0, 0, 0);
        for (int i = 0; i < threadCount; i++)
        {
            result += results[i];
        }
        return result;
    }

    private static CounterResult ProcessFile(string filePath)
    {
        byte[] data = File.ReadAllBytes(filePath);
        long neg = 0, pos = 0, zero = 0;
        bool isNegative = false;
        long currentNumber = 0;
        bool hasNumber = false;

        for (int i = 0; i < data.Length; i++)
        {
            byte b = data[i];

            if (b == '-')
            {
                isNegative = true;
            }
            else if (b >= '0' && b <= '9')
            {
                currentNumber = currentNumber * 10 + (b - '0');
                hasNumber = true;
            }
            else if (b == '\n')
            {
                if (hasNumber)
                {
                    if (currentNumber == 0) zero++;
                    else if (isNegative) neg++;
                    else pos++;
                    currentNumber = 0;
                    isNegative = false;
                    hasNumber = false;
                }
            }
        }
        if (hasNumber)
        {
            if (currentNumber == 0) zero++;
            else if (isNegative) neg++;
            else pos++;
        }
        return new CounterResult(neg, pos, zero);
    }

}
