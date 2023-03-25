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
}
