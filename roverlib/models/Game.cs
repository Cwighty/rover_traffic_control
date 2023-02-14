using System.Net.Http.Json;
using Roverlib.Models.Responses;
namespace Roverlib.Models;

public class Game
{
    private readonly HttpClient client;

    public Game(string name, string gameId, JoinResponse response, HttpClient client)
    {
        Name = name;
        GameId = gameId;
        this.client = client;
        Token = response.token;
        Enum.TryParse<Orientation>(response.orientation, out var orient);
        Rover = new PerserveranceRover(response);
        Heli = new IngenuityRover(response);

    }
    public string GameId { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public Board Board { get; set; }
    public PerserveranceRover Rover { get; set; }
    public IngenuityRover Heli { get; set; }

    public async Task MoveRoverAsync(Direction direction)
    {
        var res = await client.GetAsync($"/Game/MovePerseverance?token={Token}&direction={direction}");
        if (res.IsSuccessStatusCode)
        {
            var result = await res.Content.ReadFromJsonAsync<MoveResponse>();
            Rover.Location = (result.X, result.Y);
            Rover.Battery = result.batteryLevel;
            Enum.TryParse<Orientation>(result.orientation, out var orient);
            Rover.Orientation = orient;
        }
        else
        {
            if (res.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
            {
                var result = await res.Content.ReadFromJsonAsync<ProblemDetail>();
                throw new ProblemDetailException(result);
            }
        }
    }
    public async Task MoveHeliAsync(int X, int Y)
    {
        var res = await client.GetAsync($"/Game/MoveIngenuity?token={Token}&destinationX={X}&destinationY={Y}");
        if (res.IsSuccessStatusCode)
        {
            var result = await res.Content.ReadFromJsonAsync<MoveResponse>();
            Heli.Location = (result.X, result.Y);
            Heli.Battery = result.batteryLevel;
            updateVisited(result.neighbors);
        }
        else
        {
            var result = new ProblemDetail();
            try
            {
                result = await res.Content.ReadFromJsonAsync<ProblemDetail>();
            }
            catch { }
            throw new ProblemDetailException(result);
        }
    }

    private void updateVisited(IEnumerable<Neighbor> neighbors)
    {
        foreach (var n in neighbors)
        {
            Board.VisitedNeighbors.TryAdd(n.HashToLong(), n);
        }
    }
}
