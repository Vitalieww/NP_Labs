namespace Lab1.Tasks;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;
using Lab1.Common;

public static class Task1c
{
    private const int BatchSize = 10_000;
    private const int MaxBatchesInQueue = 100;

    public static CounterResult Run(string filePath, int workerCount)
    {
        var queue = new BlockingCollection<string[]>(boundedCapacity: MaxBatchesInQueue);
        CounterResult[] workers = new CounterResult[workerCount];

        Thread readerThread = new Thread(() =>
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8);
            List<string> currentBatch = new List<string>(BatchSize);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (currentBatch.Count == BatchSize)
                {
                    queue.Add(currentBatch.ToArray());
                    currentBatch = new List<string>(BatchSize);
                }
                currentBatch.Add(line);
            }
            if (currentBatch.Count > 0)
            {
                queue.Add(currentBatch.ToArray());
            }
            queue.CompleteAdding();

        });
        readerThread.Start();

        Thread[] workerThreads = new Thread[workerCount];
        for (int w = 0; w < workerCount; w++)
        {
            int workerIndex = w;
            workerThreads[w] = new Thread(() =>
            {
                long neg = 0, pos = 0, zero = 0;
                foreach (string[] batch in queue.GetConsumingEnumerable())
                {
                    for (int i = 0; i < batch.Length; i++)
                    {
                        if (int.TryParse(batch[i], out int val))
                        {
                            if (val < 0) neg++;
                            else if (val > 0) pos++;
                            else zero++;

                        }
                    }

                }
                workers[workerIndex] = new CounterResult(neg, pos, zero);
            });
            workerThreads[w].Start();

        }
        readerThread.Join();
        for (int w = 0; w < workerCount; w++)
        {
            workerThreads[w].Join();
        }
        CounterResult total = new CounterResult(0, 0, 0);
        for (int i = 0; i < workerCount; i++)
        {
            total += workers[i];
        }
        return total;
    }
}