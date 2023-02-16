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
        updateVisited(response.neighbors);
    }
    public string GameId { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public PerserveranceRover Rover { get; set; }
    public IngenuityRover Heli { get; set; }

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

    public async Task MoveRoverToPointAsync(int x, int y)
    {
        // Find path to point and move heli along it two steps at a time
        var targetLoc = (x, y);
        if (targetLoc == Heli.Location)
        {
            return;
        }
        var line = new ParametricLine(Rover.Location, targetLoc);
        var path = line.GetDiscretePointsAlongLine().ToQueue();
        while (path.Count > 0)
        {
            var next = path.Peek();
            try
            {
                var curLoc = Rover.Location;
                var curOrientation = Rover.Orientation;
                while (curOrientation != Orientation.North)
                {
                    await MoveRoverAsync(Direction.Right);
                    curOrientation = Rover.Orientation;
                }
                while (curLoc.Y < targetLoc.y)
                {
                    await MoveRoverAsync(Direction.Forward);
                    curLoc = Rover.Location;
                }
                while (curLoc.Y > targetLoc.y)
                {
                    await MoveRoverAsync(Direction.Reverse);
                    curLoc = Rover.Location;
                }

                while (curOrientation != Orientation.East)
                {
                    await MoveRoverAsync(Direction.Right);
                    curOrientation = Rover.Orientation;
                }
                while (curLoc.X < targetLoc.x)
                {
                    await MoveRoverAsync(Direction.Forward);
                    curLoc = Rover.Location;
                }
                while (curLoc.X > targetLoc.x)
                {
                    await MoveRoverAsync(Direction.Reverse);
                    curLoc = Rover.Location;
                }
            }
            catch { }
        }
    }

    public async Task MoveRoverAlongPathAsync(Queue<(int X, int Y)> path)
    {
        while (path.Count > 0)
        {
            var p = path.Peek();
            try
            {
                await MoveRoverToPointAsync(p.X, p.Y);
                path.Dequeue();
            }
            catch
            { }
        }
    }

    public async Task StepRoverTowardPointAsync(int x, int y)
    {
        // Find path to point and move heli along it two steps at a time
        var targetLoc = (x, y);
        if (targetLoc == Heli.Location)
        {
            return;
        }
        var line = new ParametricLine(Rover.Location, targetLoc);
        var path = line.GetDiscretePointsAlongLine().ToQueue();
        var next = path.Peek();
        try
        {
            var curLoc = Rover.Location;
            var curOrientation = Rover.Orientation;
            while (curOrientation != Orientation.North)
            {
                await MoveRoverAsync(Direction.Right);
                curOrientation = Rover.Orientation;
            }
            while (curLoc.Y < targetLoc.y)
            {
                await MoveRoverAsync(Direction.Forward);
                curLoc = Rover.Location;
            }
            while (curLoc.Y > targetLoc.y)
            {
                await MoveRoverAsync(Direction.Reverse);
                curLoc = Rover.Location;
            }

            while (curOrientation != Orientation.East)
            {
                await MoveRoverAsync(Direction.Right);
                curOrientation = Rover.Orientation;
            }
            while (curLoc.X < targetLoc.x)
            {
                await MoveRoverAsync(Direction.Forward);
                curLoc = Rover.Location;
            }
            while (curLoc.X > targetLoc.x)
            {
                await MoveRoverAsync(Direction.Reverse);
                curLoc = Rover.Location;
            }
        }
        catch { }
    }


    private void updateVisited(IEnumerable<Neighbor> neighbors)
    {
        if (NotifyGameManager != null)
        {
            NotifyGameManager(new NewNeighborsEventArgs(neighbors));
        }
    }
}
