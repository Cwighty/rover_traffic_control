using Roverlib.Models.Responses;

namespace Roverlib.Models;

public class IngenuityRover
{
    public IngenuityRover(JoinResponse response)
    {
        Location = (response.startingX, response.startingY);
    }

    public (int X, int Y) Location { get; set; }
    public int Battery { get; set; }
}