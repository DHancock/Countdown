
namespace Countdown.ViewModels;

internal class EquationComparer : Comparer<string>
{
    public override int Compare(string? a, string? b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        if (a is null)
        {
            return -1;
        }

        if (b is null)
        {
            return 1;
        }

        int result = a.Length - b.Length;

        if (result == 0)
        {
            result = string.Compare(b, a);
        }

        return result;
    }
}
