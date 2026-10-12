using Lab1.Common;

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

    default:
        Console.WriteLine($"Unknown command: {args[0]}");
        break;
}
