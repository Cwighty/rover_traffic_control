using Roverlib.Models.Responses;
using Roverlib.Services;

namespace Roverlib.Models;
public class RoverTeam
{
    private readonly HttpClient client;
    public event NotifyNeighborsDelegate NotifyGameManager;
    public RoverTeam(string name, string gameId, JoinResponse response, HttpClient client)
    {
        Name = name;
        GameId = gameId;
        this.client = client;
        Token = response.token;
        Enum.TryParse<Orientation>(response.orientation, out var orient);
        Rover = new PerserveranceRover(response, client, updateVisited);
        Heli = new IngenuityRover(response, client, updateVisited);
        updateVisited(response.neighbors);
    }
    public string GameId { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public PerserveranceRover Rover { get; set; }
    public IngenuityRover Heli { get; set; }


    private void updateVisited(IEnumerable<Neighbor> neighbors)
    {
        if (NotifyGameManager != null)
        {
            NotifyGameManager(new NewNeighborsEventArgs(neighbors));
        }
    }
}
