namespace Countdown.Utils;

internal static class Extensions
{

    public static int CountOf<T>(this IList<T> list, Func<T, bool> predicate)
    {
        int count = 0;

        foreach (T item in list)
        {
            if (predicate(item))
                count++;
        }

        return count;
    }

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        if (list.Count > 1)
        {
            Random random = new Random();

            for (int index = list.Count - 1; index > 0; --index)
                Swap(list, index, random.Next(index + 1));
        }

        return list;
    }

    public static IList<T> ReduceDuplicateSequences<T>(this IList<T> list, IEqualityComparer? comparer = null)
    {
        if (list.Count > 2)
        {
            Random random = new Random();
            comparer ??= EqualityComparer<T>.Default;

            int duplicateCount = 0;
            T previous = list[0];

            for (int index = 1; index < list.Count; index++)
            {
                T current = list[index];

                if (comparer.Equals(previous, current))
                    duplicateCount++;
                else
                    previous = current;

                if (duplicateCount > 0)
                {
                    duplicateCount = 0;
                    int attempts = 10;

                    while (attempts-- > 0)  // an occasional duplicate is ok, infinite loops aren't
                    {
                        int altIndex = random.Next(list.Count);

                        if (CheckSwapWontCreateAnotherDuplicate(list, altIndex, current, comparer))
                        {
                            Swap(list, index, altIndex);
                            previous = list[altIndex];
                            break;
                        }
                    }
                }
            }
        }

        return list;
    }

    private static int ClampIndex(int index, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        int remainder = index % size; 

        if (index < 0)
            return (remainder == 0) ? 0 : size + remainder;

        return remainder;
    }

    private static bool CheckSwapWontCreateAnotherDuplicate<T>(IList<T> list, int index, T testValue, IEqualityComparer comparer)
    {
        return !comparer.Equals(testValue, list[index]) &&
                !comparer.Equals(testValue, list[ClampIndex(index - 1, list.Count)]) &&
                !comparer.Equals(testValue, list[ClampIndex(index + 1, list.Count)]);
    }

    private static void Swap<T>(IList<T> list, int i1, int i2)
    {
        T temp = list[i1];
        list[i1] = list[i2];
        list[i2] = temp;
    }

    public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count)
    {
        if (count == 0)
            return 0;

        int size;
        int bytesLeft = count;
        int bytesRead = 0;

        do 
        {
            size = stream.Read(buffer, bytesRead + offset, bytesLeft);

            bytesLeft -= size;
            bytesRead += size;
        }
        while ((bytesLeft > 0) && (size > 0));

        return bytesRead;
    }
}

