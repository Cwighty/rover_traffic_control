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
        HeliCancelSource = new CancellationTokenSource();
    }
    public string GameId { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public PerserveranceRover Rover { get; set; }
    public IngenuityRover Heli { get; set; }

    public CancellationTokenSource HeliCancelSource { get; private set; }



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
            Heli.Location = new Location(result.X, result.Y);
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
    public async Task MoveHeliToPointAsync(Location point)
    {
        // Find path to point and move heli along it two steps at a time
        if (point == Heli.Location)
        {
            return;
        }
        var line = new ParametricLine((Heli.Location.X, Heli.Location.Y), (point.X, point.Y));
        var path = line.GetDiscretePointsAlongLine().ToQueue();
        while (path.Count > 0)
        {
            if (HeliCancelSource.IsCancellationRequested)
            {
                return;
            }
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

    public async Task MoveHeliToNearestAxisAsync(TrafficControlService trafficControl)
    {
        var curLoc = Heli.Location;
        var width = trafficControl.GameBoard.Width;
        var height = trafficControl.GameBoard.Height;
        var midx = width / 2;
        var midy = height / 2;
        var halfPoints = new List<(int X, int Y)>(){
            (midx, 0),
            (midx, height),
            (0, midy),
            (width, midy)
        };
        var nearestPoint = halfPoints.OrderBy(p => Math.Abs(p.X - curLoc.X) + Math.Abs(p.Y - curLoc.Y)).First();
        await MoveHeliToPointAsync(new Location(nearestPoint.X, nearestPoint.Y));
    }
    public void CancelHeli()
    {
        HeliCancelSource.Cancel();
    }

    public async Task MoveRoverAsync(Direction direction)
    {
        var res = await client.GetAsync($"/Game/MovePerseverance?token={Token}&direction={direction}");
        if (res.IsSuccessStatusCode)
        {
            var result = await res.Content.ReadFromJsonAsync<MoveResponse>();
            Rover.Location = new Location(result.X, result.Y);
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
        // Find path to point and move rover along it two steps at a time
        var targetLoc = new Location (x, y);
        if (targetLoc == Rover.Location)
        {
            return;
        }
        try
        {
            var curLoc = Rover.Location;
            var curOrientation = Rover.Orientation;
            if (curLoc.Y != targetLoc.Y)
            {
                while (curOrientation != Orientation.North && curOrientation != Orientation.South)
                {
                    await MoveRoverAsync(Direction.Right);
                    curOrientation = Rover.Orientation;
                }
            }
            while (curLoc.Y < targetLoc.Y)
            {
                if (curOrientation == Orientation.North)
                    await MoveRoverAsync(Direction.Forward);
                else
                    await MoveRoverAsync(Direction.Reverse);
                curLoc = Rover.Location;
            }
            while (curLoc.Y > targetLoc.Y)
            {
                if (curOrientation == Orientation.North)
                    await MoveRoverAsync(Direction.Reverse);
                else
                    await MoveRoverAsync(Direction.Forward);
                curLoc = Rover.Location;
            }
            if (curLoc.X != targetLoc.X)
            {
                while (curOrientation != Orientation.East && curOrientation != Orientation.West)
                {
                    await MoveRoverAsync(Direction.Right);
                    curOrientation = Rover.Orientation;
                }
            }
            while (curLoc.X < targetLoc.X)
            {
                if (curOrientation == Orientation.East)
                    await MoveRoverAsync(Direction.Forward);
                else
                    await MoveRoverAsync(Direction.Reverse);
                curLoc = Rover.Location;
            }
            while (curLoc.X > targetLoc.X)
            {
                if (curOrientation == Orientation.East)
                    await MoveRoverAsync(Direction.Reverse);
                else
                    await MoveRoverAsync(Direction.Forward);
                curLoc = Rover.Location;
            }
        }
        catch { }
    }


    public async Task MoveRoverToNearestAxisAsync(TrafficControlService trafficControl)
    {
        var curLoc = Rover.Location;
        var width = trafficControl.GameBoard.Width;
        var height = trafficControl.GameBoard.Height;
        var midx = width / 2;
        var midy = height / 2;
        var halfPoints = new List<(int X, int Y)>(){
            (midx, 0),
            (midx, height),
            (0, midy),
            (width, midy)
        };
        var nearestPoint = halfPoints.OrderBy(p => Math.Abs(p.X - curLoc.X) + Math.Abs(p.Y - curLoc.Y)).First();
        await MoveRoverToPointAsync(nearestPoint.X, nearestPoint.Y);
    }
    public async Task MoveRoverAlongPathAsync(Queue<(int X, int Y)> path)
    {
        while (path.Count > 0)
        {
            var p = path.Peek();
            try
            {
                await MoveRoverToPointAsync(p.X, p.Y);
                if (Rover.Location == new Location(p.X, p.Y))
                    path.Dequeue();
            }
            catch
            { }
        }
    }


    private void updateVisited(IEnumerable<Neighbor> neighbors)
    {
        if (NotifyGameManager != null)
        {
            NotifyGameManager(new NewNeighborsEventArgs(neighbors));
        }
    }
}
