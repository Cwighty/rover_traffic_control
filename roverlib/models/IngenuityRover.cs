﻿using System.Net.Http.Json;
using Roverlib.Models.Responses;
using Roverlib.Services;
using Roverlib.Utils;

namespace Roverlib.Models;

public class IngenuityRover
{
    private HttpClient client;
    private readonly Action<IEnumerable<Neighbor>> updateVisited;
    private string token;
    private static List<Location> reachedTargets = new();
    public int Battery { get; set; }
    public CancellationTokenSource CancelSource { get; private set; }
    public Location Location { get; set; }

    public IngenuityRover(
        JoinResponse response,
        HttpClient client,
        Action<IEnumerable<Neighbor>> updateVisited
    )
    {
        Location = new Location(response.startingX, response.startingY);
        this.client = client;
        this.updateVisited = updateVisited;
        this.token = response.token;
        CancelSource = new CancellationTokenSource();
    }

    public async Task MoveAsync(int X, int Y)
    {
        var res = await client.GetAsync(
            $"/Game/MoveIngenuity?token={token}&destinationRow={X}&destinationColumn={Y}"
        );
        if (res.IsSuccessStatusCode)
        {
            var result = await res.Content.ReadFromJsonAsync<MoveResponse>();
            if (result.message.Contains("Ingenuity cannot fly that far at once."))
            {
                throw new Exception("Ingenuity cannot fly that far at once.");
            }
            Location = new Location(result.X, result.Y);
            Battery = result.batteryLevel;
            updateVisited(result.neighbors);
        }
        else
        {
            if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("Heli too many requests, sleeping for 1 second");
                Thread.Sleep(1000);
            }
            var result = new ProblemDetail();
            try
            {
                result = await res.Content.ReadFromJsonAsync<ProblemDetail>();
            }
            catch { }
            throw new ProblemDetailException(result);
        }
    }

    public async Task FlyToTargets(List<Location> targets)
    {
        var localTargets = new List<Location>(targets);
        if (localTargets.Count == 0)
        {
            CancelFlight();
            return;
        }

        var nextTarget = localTargets.First();
        localTargets.Remove(nextTarget);
        var task = MoveToPointAsync(nextTarget);

        await task.ContinueWith(
            async (t) =>
            {
                // if (!reachedTargets.Contains(target) && localTargets.Count > 0)
                // { // Shut off the first heli to reach the target to save rate limiting for frontrunners
                //     reachedTargets.Add(target);
                //     Console.WriteLine($"Heli {token} reached target {target.X}, {target.Y}");
                //     CancelFlight();
                // }
                await FlyToTargets(localTargets);
            }
        );
    }

    public async Task FlyToReconPoints(List<Location> targets)
    {
        if (targets.Count == 0)
        {
            CancelFlight();
            return;
        }
        var target = PathUtils.GetNearestTarget(Location, targets);
        targets.Remove(target);
        var task = MoveToPointAsync(target);

        await task.ContinueWith(
            async (t) =>
            {
                await FlyToReconPoints(targets);
            }
        );
    }

    public async Task MoveToPointAsync(Location point)
    {
        // Find path to point and move heli along it two steps at a time
        if (point == Location)
        {
            return;
        }
        var line = new ParametricLine((Location.X, Location.Y), (point.X, point.Y));
        var path = line.GetDiscretePointsAlongLine().ToQueue();
        while (path.Count > 0)
        {
            if (CancelSource.IsCancellationRequested)
            {
                return;
            }
            var next = path.Peek();
            try
            {
                await MoveAsync(next.X, next.Y);
                path.Dequeue();
                path.Dequeue();
            }
            catch { }
        }
        try
        {
            await MoveAsync(point.X, point.Y);
        }
        catch { }
    }

    public async Task FlyToNearestAxisAsync(TrafficControlService trafficControl)
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
        await MoveToPointAsync(new Location(nearestPoint.X, nearestPoint.Y));
    }

    public void CancelFlight()
    {
        CancelSource.Cancel();
    }
}
