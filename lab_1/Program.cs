using Lab1.Common;
using Lab1.Tasks;

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- generate         (Generates numbers.txt with 50,000,000 integers)");
    Console.WriteLine("  dotnet run -- split            (Splits numbers.txt into 8 parts for Task 2)");
    return;
}

switch (args[0].ToLowerInvariant())
{
    case "generate":
        DataGenerator.GenerateNumbers();
        break;

    case "split":
        DataGenerator.SplitFile();
        break;

    case "1a":
        int threads = args.Length > 1 ? int.Parse(args[1]) : 4; Metric.Measure($"Task 1a ({threads} threads)", () => Task1a.Run("numbers.txt", threads));
        break;

    case "1b":
        Task1b.Run();
        break;

    case "1c":
        int workers = args.Length > 1 ? int.Parse(args[1]) : 4;
        Metric.Measure($"Task 1c {workers} workers", () => Task1c.Run("numbers.txt", workers));
        break;

    case "2":
        string dir = args.Length > 1 ? args[1] : "parts";
        int t2Threads = args.Length > 2 ? int.Parse(args[2]) : 4;
        Metric.Measure($"Task 2 ({t2Threads} threads on '{dir}')", () => Task2.Run(dir, t2Threads));
        break;

    default:
        Console.WriteLine($"Unknown command: {args[0]}");
        break;
}
