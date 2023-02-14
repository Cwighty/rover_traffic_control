using System.Collections.Concurrent;
using System.Net.Http.Json;
using Roverlib.Models.Responses;
using Roverlib.Services;
using Roverlib.Utils;

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
        Rover = new PerserveranceRover(response);
        Heli = new IngenuityRover(response);

    }
    public string GameId { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
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
            updateVisited(result.neighbors);
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
        var res = await client.GetAsync($"/Game/MoveIngenuity?token={Token}&destinationRow={X}&destinationColumn={Y}");
        if (res.IsSuccessStatusCode)
        {
            var result = await res.Content.ReadFromJsonAsync<MoveResponse>();
            if (result.message.Contains("Ingenuity cannot fly that far at once."))
            {
                throw new Exception("Ingenuity cannot fly that far at once.");
            }
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

    public async Task MoveHeliToPointAsync((int X, int Y) point)
    {
        // Find path to point and move heli along it two steps at a time
        if (point == Heli.Location)
        {
            return;
        }
        var line = new ParametricLine(Heli.Location, point);
        var path = line.GetDiscretePointsAlongLine().ToQueue();
        while (path.Count > 0)
        {
            var next = path.Peek();
            try
            {
                await MoveHeliAsync(next.X, next.Y);
                path.Dequeue();
                path.Dequeue();
            }
            catch
            {
            }
        }
        await MoveHeliAsync(point.X, point.Y);
    }



    private List<(int X, int Y)> getNeighbors((int X, int Y) start)
    {
        return new List<(int X, int Y)>() { (start.X + 1, start.Y), (start.X - 1, start.Y), (start.X, start.Y + 1), (start.X, start.Y - 1) };
    }

    private void updateVisited(IEnumerable<Neighbor> neighbors)
    {
        if (NotifyGameManager != null)
        {
            NotifyGameManager(new NewNeighborsEventArgs(neighbors));
        }
    }
}
