
namespace Countdown.Utils;

internal static class Extensions
{

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        if (list.Count > 0)
        {
            Random random = new Random();

            for (int index = list.Count - 1; index > 0; --index)
            {
                int next = random.Next(index + 1);

                T temp = list[next];
                list[next] = list[index];
                list[index] = temp;
            }
        }

        return list;
    }


    public static IList<T> ReduceDuplicateSequences<T>(this IList<T> list)
    {
        if (list.Count > 0)
        {
            Random random = new Random();
            IEqualityComparer comparer = EqualityComparer<T>.Default;

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
                    int attempts = 20;

                    while (attempts-- > 0)  // an occasional duplicate is ok, infinite loops aren't
                    {
                        int altIndex = random.Next(list.Count);

                        if (TestAltIndex<T>(list, altIndex, current, comparer))
                        {
                            T altValue = list[altIndex];
                            list[altIndex] = current;
                            list[index] = altValue;
                            previous = altValue;
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
        if (index < 0)
            return size + index;
        else if (index >= size)
            return index - size;

        return index;
    }

    private static bool TestAltIndex<T>(IList<T> list, int index, T testValue, IEqualityComparer comparer)
    {
        // check that a swap won't create a new duplicate
        return !comparer.Equals(testValue, list[index]) && 
                !comparer.Equals(testValue, list[ClampIndex(index - 1, list.Count)]) && 
                !comparer.Equals(testValue, list[ClampIndex(index + 1, list.Count)]);
    }
}

