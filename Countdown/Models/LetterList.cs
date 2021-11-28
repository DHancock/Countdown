namespace Countdown.Models;


internal abstract class LetterList : ReadOnlyCollection<LetterTile>
{

    /// <summary>
    /// Decent error checking, the settings file can be edited by the user
    /// </summary>
    /// <param name="list"></param>
    public LetterList(IList<LetterTile> list) : base(list)
    {
    }


    public LetterList(LetterList other) : base(new List<LetterTile>(other.Count))
    {
        foreach (LetterTile tile in other.Items)
            Items.Add(new LetterTile(tile));
    }



    public char GetLetter(Random random)
    {
        // find tile with frequency match
        int probability = random.Next(this.Sum(lt => lt.Frequency));
        int sum = 0;

        foreach (LetterTile tile in this)
        {
            sum += tile.Frequency;

            if (probability < sum)
                return tile.Letter;
        }

        return '\0';
    }

    protected void ResetTo(LetterList toThis)
    {
        if (Count != toThis.Count)
            throw new ArgumentOutOfRangeException(nameof(toThis));

        for (int index = 0; index < Count; index++)
            this[index].Frequency = toThis[index].Frequency;
    }


    public override int GetHashCode() => throw new NotImplementedException();


    public override bool Equals(object? obj)
    {
        if (obj != null)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is LetterList other)
            {
                if (Count != other.Count)
                    return false;

                for (int index = 0; index < Count; index++)
                {
                    if (this[index] != other[index])
                        return false;
                }

                return true;
            }
        }

        return false;
    }


    public static bool operator ==(LetterList a, LetterList b)
    {
        return (a is null) ? (b is null) : a.Equals(b);
    }

    public static bool operator !=(LetterList a, LetterList b)
    {
        return !(a == b);
    }
}
