namespace Lab1.Common;

public record CounterResult(long Negative, long Positive, long Zero)
{
    public long Total => Negative + Positive + Zero;

    public static CounterResult operator +(CounterResult a, CounterResult b) =>
        new(a.Negative + b.Negative, a.Positive + b.Positive, a.Zero + b.Zero);
}
