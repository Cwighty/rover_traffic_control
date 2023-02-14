namespace Roverlib.Models.Responses;

public class LowResolutionMap
{
    public int lowerLeftX { get; set; }
    public int lowerLeftY { get; set; }
    public int upperRightX { get; set; }
    public int upperRightY { get; set; }
    public int averageDifficulty { get; set; }
}

