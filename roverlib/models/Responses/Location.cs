namespace Roverlib.Models.Responses;

public class Location
{
    public Location(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public int X { get; private set; }
    public int Y { get; private set; }

    override public bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Location other = (Location)obj;
        return (X == other.X) && (Y == other.Y);
    }

    // override not equals
    public static bool operator !=(Location a, Location b)
    {
        return !(a == b);
    }

    // override equals
    public static bool operator ==(Location a, Location b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (((object)a == null) || ((object)b == null))
        {
            return false;
        }

        return a.X == b.X && a.Y == b.Y;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
