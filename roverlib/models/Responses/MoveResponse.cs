namespace Roverlib.Models.Responses;

public class MoveResponse
{
    public int X { get; set; }
    public int Y { get; set; }
    public int batteryLevel { get; set; }
    public Neighbor[] neighbors { get; set; }
    public string message { get; set; }
    public string orientation { get; set; }
}

