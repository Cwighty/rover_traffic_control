namespace Roverlib.Models.Responses;

public class Neighbor
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Difficulty { get; set; }

    public override string ToString()
    {
        return $"{X}, {Y}";
    }

    public (int, int) ToTuple()
    {
        return (X, Y);
    }

    public long HashToLong()
    {
        return (long)X << 32 | (uint)Y;
    }
}
