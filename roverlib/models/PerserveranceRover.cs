using System.Collections.Concurrent;
using System.Net.Http.Json;
using Roverlib.Models.Responses;
using Roverlib.Services;
using Roverlib.Utils;

namespace Roverlib.Models;

public class PerserveranceRover
{
    private readonly HttpClient client;
    private readonly Action<IEnumerable<Neighbor>> updateVisited;
    private string token;
    private CancellationTokenSource CancelSource;
    private int straightDrivingIncentive = 0;

    public PerserveranceRover(
        JoinResponse response,
        HttpClient client,
        Action<IEnumerable<Neighbor>> updateVisited
    )
    {
        this.token = response.token;
        Location = new Location(response.startingX, response.startingY);
        Orientation = Enum.TryParse<Orientation>(response.orientation, out var orient)
            ? orient
            : Orientation.North;
        this.client = client;
        this.InitialBattery = response.batteryLevel;
        this.updateVisited = updateVisited;
        this.TravelCostEstimate = PathUtils.EstimateCostToFinish(
            Location,
            response.targets,
            response.lowResolutionMap.Average(x => x.averageDifficulty)
        );
        CancelSource = new CancellationTokenSource();
    }

    public string CurrentLocation
    {
        get => $"{Location.X}, {Location.Y}";
    }

    public int InitialBattery { get; set; }
    public int Battery { get; set; }
    private List<int> batteryLevels { get; set; } = new List<int>();
    public Location Location { get; set; }
    public Orientation Orientation { get; set; }

    public int TravelCostEstimate { get; private set; }

    public EventHandler WinEvent { get; set; }

    public async Task MoveAsync(Direction direction)
    {
        var res = await client.GetAsync(
            $"/Game/MovePerseverance?token={token}&direction={direction}"
        );
        if (res.IsSuccessStatusCode)
        {
            var result = await res.Content.ReadFromJsonAsync<MoveResponse>();
            if (result.message.Contains("You made it to all the targets!"))
            {
                WinEvent?.Invoke(this, EventArgs.Empty);
            }
            if (result.message.Contains("Insufficient battery to make move"))
            {
                Console.WriteLine("Insufficient battery to make move, waiting 1 second");
                Thread.Sleep(1000);
            }
            Location = new Location(result.X, result.Y);
            Battery = result.batteryLevel;
            Enum.TryParse<Orientation>(result.orientation, out var orient);
            Orientation = orient;
            updateVisited(result.neighbors);
        }
        else
        {
            if (res.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
            {
                var result = await res.Content.ReadFromJsonAsync<ProblemDetail>();
                throw new ProblemDetailException(result);
            }
            else
            {
                Console.WriteLine("Perseverance too many requests, sleeping for 1 second");
                Thread.Sleep(1000);
            }
        }
    }

    public async Task DriveToPointAsync(int x, int y)
    {
        // Find path to point and move rover along it two steps at a time
        var targetLoc = new Location(x, y);
        if (targetLoc == Location)
        {
            return;
        }
        try
        {
            var curLoc = Location;
            var curOrientation = Orientation;
            if (curLoc.Y != targetLoc.Y)
            {
                while (curOrientation != Orientation.North && curOrientation != Orientation.South)
                {
                    await MoveAsync(Direction.Right);
                    curOrientation = Orientation;
                }
            }
            while (curLoc.Y < targetLoc.Y)
            {
                if (curOrientation == Orientation.North)
                    await MoveAsync(Direction.Forward);
                else
                    await MoveAsync(Direction.Reverse);
                curLoc = Location;
            }
            while (curLoc.Y > targetLoc.Y)
            {
                if (curOrientation == Orientation.North)
                    await MoveAsync(Direction.Reverse);
                else
                    await MoveAsync(Direction.Forward);
                curLoc = Location;
            }
            if (curLoc.X != targetLoc.X)
            {
                while (curOrientation != Orientation.East && curOrientation != Orientation.West)
                {
                    await MoveAsync(Direction.Right);
                    curOrientation = Orientation;
                }
            }
            while (curLoc.X < targetLoc.X)
            {
                if (curOrientation == Orientation.East)
                    await MoveAsync(Direction.Forward);
                else
                    await MoveAsync(Direction.Reverse);
                curLoc = Location;
            }
            while (curLoc.X > targetLoc.X)
            {
                if (curOrientation == Orientation.East)
                    await MoveAsync(Direction.Reverse);
                else
                    await MoveAsync(Direction.Forward);
                curLoc = Location;
            }
        }
        catch { }
    }

    public async Task DriveAlongPathAsync(Queue<(int X, int Y)> path)
    {
        while (path.Count > 0)
        {
            if (CancelSource.Token.IsCancellationRequested)
            {
                return;
            }
            var p = path.Peek();
            try
            {
                await DriveToPointAsync(p.X, p.Y);
                if ((Location.X == p.X && Location.Y == p.Y))
                    path.Dequeue();
            }
            catch { }
        }
    }

    public async Task DriveToNearestAxisAsync(TrafficControlService trafficControl)
    {
        var curLoc = Location;
        var width = trafficControl.GameBoard.Width;
        var height = trafficControl.GameBoard.Height;
        var midx = width / 2;
        var midy = height / 2;
        var halfPoints = new List<(int X, int Y)>()
        {
            (midx, 0),
            (midx, height),
            (0, midy),
            (width, midy)
        };
        var nearestPoint = halfPoints
            .OrderBy(p => Math.Abs(p.X - curLoc.X) + Math.Abs(p.Y - curLoc.Y))
            .First();
        await DriveToPointAsync(nearestPoint.X, nearestPoint.Y);
    }

    private async Task PathfindToPointAsync(
        ConcurrentDictionary<long, Neighbor> map,
        Location target,
        Func<(int, int), (int, int), int> heuristic,
        int optBuffer,
        int targetsLeft
    )
    {
        while (Location != target)
        {
            Console.WriteLine("Calculating path...");
            var path = new List<(int, int)>();
            while (path.Count == 0)
            {
                if (Location == target)
                {
                    return;
                }
                (path, var cost) = PathUtils.FindPath(
                    map,
                    (Location.X, Location.Y),
                    (target.X, target.Y),
                    heuristic,
                    optBuffer,
                    straightDrivingIncentive
                );
                if (targetsLeft == 1 && Battery > cost)
                {
                    straightDrivingIncentive = 100;
                }
                if (cost != -1)
                {
                    Console.WriteLine(
                        $"Path found, cost: {cost}, straight driving incentive: {straightDrivingIncentive}"
                    );
                }
            }
            await DriveAlongPathAsync(path.Take(50).ToQueue());
        }
    }

    public async void DriveToTargets(
        ConcurrentDictionary<long, Neighbor> map,
        List<Location> targets,
        Func<(int, int), (int, int), int> heuristic = null,
        int optBuffer = 20
    )
    {
        var localTargets = new List<Location>(targets);
        Console.WriteLine(
            $"Targets will be driven in this order: {string.Join(", ", localTargets)}"
        );
        if (localTargets.Count == 0)
        {
            Console.WriteLine("Rover Won!");
            WinEvent.Invoke(this, EventArgs.Empty);
            return;
        }
        //var target = PathUtils.GetNearestTarget(Location, localTargets);
        var target = localTargets[0];
        Console.WriteLine($"Driving to target {target}");
        var targetsLeft = localTargets.Count;
        var task = PathfindToPointAsync(map, target, heuristic, optBuffer, targetsLeft);
        await task;
        localTargets.Remove(target);
        DriveToTargets(map, localTargets, heuristic, optBuffer);
    }

    public void CancelDrive()
    {
        CancelSource.Cancel();
    }

    internal async Task DriveStraightToTargetsAsync(List<Location> targetRoute)
    {
        foreach (var target in targetRoute)
        {
            await DriveToPointAsync(target.X, target.Y);
        }
    }
}
