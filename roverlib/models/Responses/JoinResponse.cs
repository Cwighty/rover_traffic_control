namespace Roverlib.Models.Responses;


public class JoinResponse
{
    public string token { get; set; }
    public int startingX { get; set; }
    public int startingY { get; set; }
    public int targetX { get; set; }
    public int targetY { get; set; }
    public Neighbor[] neighbors { get; set; }
    public LowResolutionMap[] lowResolutionMap { get; set; }
    public string orientation { get; set; }
}

