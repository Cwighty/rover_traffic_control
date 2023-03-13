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

    internal object DistanceTo(object location)
    {
        throw new NotImplementedException();
    }
}

public static class LocationExtensions
{
    public static int DistanceTo(this Location location, Location otherLocation)
    {
        return Math.Abs(location.X - otherLocation.X) + Math.Abs(location.Y - otherLocation.Y);
    }
}