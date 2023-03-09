using Roverlib.Models.Responses;

namespace Roverlib.Models;

public class IngenuityRover
{
    public IngenuityRover(JoinResponse response)
    {
        Location = new Location(response.startingX, response.startingY);
    }

    public Location Location { get; set; }
    public int Battery { get; set; }
}