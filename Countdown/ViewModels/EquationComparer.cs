namespace Countdown.ViewModels;

internal readonly struct EquationComparer : IComparer<string>
{
    public int Compare(string? a, string? b)
    {
        Debug.Assert(a is not null);
        Debug.Assert(b is not null);

        int result = a.Length - b.Length;

        if (result == 0)
        {
            result = string.Compare(b, a);
        }

        return result;
    }
}
