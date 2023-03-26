using System.Collections.Concurrent;
using Roverlib.Models;
using Roverlib.Models.Responses;

namespace Roverlib.Utils;

public class PathUtils
{
    public static int StraightIncentive { get; set; }

    private class Node : IComparable<Node>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Cost { get; set; }
        public int Heuristic { get; set; }
        public List<(int, int)> Path { get; set; }

        public int CompareTo(Node other)
        {
            if (other == null)
                return 1;
            return (Cost + Heuristic).CompareTo(other.Cost + other.Heuristic);
        }
    }

    public static (List<(int X, int Y)>, int) FindPath(
        ConcurrentDictionary<long, Neighbor> cmap,
        (int X, int Y) start,
        (int X, int Y) target,
        Func<(int, int), (int, int), int> heuristicFunction = null,
        int mapOptimizationBuffer = 20,
        int? straightDrivingIncentive = null
    )
    {
        if (heuristicFunction == null)
        {
            heuristicFunction = ManhattanDistance;
        }

        var map = GetSubmap(cmap, start, target, mapOptimizationBuffer);

        // Check if the start and target positions are within the map
        if (!map.ContainsKey(start) || !map.ContainsKey(target))
        {
            return (new List<(int X, int Y)>(), -1);
        }

        // A* algorithm to find most efficient path
        var heap = new PriorityQueue<Node, int>();
        heap.Enqueue(
            new Node
            {
                X = start.X,
                Y = start.Y,
                Cost = 0,
                Heuristic = heuristicFunction.Invoke(start, target),
                Path = new List<(int X, int Y)> { start }
            },
            0
        );
        var visited = new HashSet<(int X, int Y)>();
        while (heap.Count > 0)
        {
            var node = heap.Dequeue();
            if (visited.Contains((node.X, node.Y)))
            {
                continue;
            }
            visited.Add((node.X, node.Y));
            if (node.X == target.X && node.Y == target.Y)
            {
                return (node.Path, node.Cost);
            }
            foreach (var dir in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int newX = node.X + dir.Item1;
                int newY = node.Y + dir.Item2;
                var n = (newX, newY);
                if (map.ContainsKey(n) && !visited.Contains(n))
                {
                    int newCost = node.Cost + map[n];
                    int newHeuristic = heuristicFunction.Invoke(n, target);
                    var newPath = new List<(int X, int Y)>(node.Path);
                    if (
                        newPath.Count > 1
                        && (newX - newPath[newPath.Count - 2].Item1)
                            * (newY - newPath[newPath.Count - 2].Item2)
                            != 0
                    )
                    { // add cost for not driving straight
                        newCost += straightDrivingIncentive ?? StraightIncentive;
                    }
                    newPath.Add((newX, newY));
                    heap.Enqueue(
                        new Node
                        {
                            X = newX,
                            Y = newY,
                            Cost = newCost,
                            Heuristic = newHeuristic,
                            Path = newPath
                        },
                        newCost + newHeuristic
                    );
                }
            }
        }

        return (new List<(int X, int Y)>(), -1);
    }

    public static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
    { // as the cars drive
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    public static int ManhattanDistance(Location a, Location b)
    { // as the cars drive
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    public static int EuclideanDistance((int X, int Y) a, (int X, int Y) b)
    { // as the crow flies
        return (int)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    public static int EuclideanDistance(Location a, Location b)
    { // as the crow flies
        return (int)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    public static Dictionary<(int X, int Y), int> GetSubmap(
        ConcurrentDictionary<long, Neighbor> mapData,
        (int, int) start,
        (int, int) target,
        int buffer
    )
    {
        var map = new Dictionary<(int X, int Y), int>();
        if (mapData.Count == 0)
            return map;

        // Determine the points along the line using Bresenham's algorithm
        var linePoints = BresenhamLine(start.Item1, start.Item2, target.Item1, target.Item2);

        // Add the points along the line and their buffer zones to the map
        foreach (var point in linePoints)
        {
            for (int i = -buffer; i <= buffer; i++)
            {
                for (int j = -buffer; j <= buffer; j++)
                {
                    var x = point.Item1 + i;
                    var y = point.Item2 + j;

                    // Check if the cell is within the map data and add it to the map
                    mapData.TryGetValue(
                        new Neighbor { X = x, Y = y }.HashToLong(),
                        out var neighborData
                    );
                    if (neighborData != null)
                        map.TryAdd((x, y), neighborData.Difficulty);
                }
            }
        }

        return map;
    }

    private static List<(int, int)> BresenhamLine(int x0, int y0, int x1, int y1)
    {
        var linePoints = new List<(int, int)>();
        int dx = Math.Abs(x1 - x0),
            sx = x0 < x1 ? 1 : -1;
        int dy = Math.Abs(y1 - y0),
            sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            linePoints.Add((x0, y0));

            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return linePoints;
    }

    private static int getMapDataAt((int X, int Y) point, Dictionary<long, Neighbor> mapData)
    {
        var n = new Neighbor() { X = point.X, Y = point.Y };
        return mapData.TryGetValue(n.HashToLong(), out var neighbor)
            ? neighbor.Difficulty
            : int.MaxValue;
    }

    public static Location GetNearestTarget(Location currentLocation, List<Location> targets)
    {
        var nearest = targets
            .OrderBy(t => Math.Abs(t.X - currentLocation.X) + Math.Abs(t.Y - currentLocation.Y))
            .First();
        return nearest;
    }

    public static int EstimateCostToFinish(
        Location currentLocation,
        List<Location> targets,
        double mapAvgDifficulty
    )
    {
        // Estimate the cost to finish from current location to all targets by multiplying the distance to travel from target to target by average difficulty of the map
        double totalCost = 0;
        var targetsCopy = new List<Location>(targets);
        while (targetsCopy.Count > 0)
        {
            var nearest = GetNearestTarget(currentLocation, targetsCopy);
            totalCost +=
                ManhattanDistance((currentLocation.X, currentLocation.Y), (nearest.X, nearest.Y))
                * mapAvgDifficulty;
            currentLocation = nearest;
            targetsCopy.Remove(nearest);
        }
        return (int)totalCost;
    }

    public static double GetClosestTargetDistanceToEdge(Board gameBoard)
    {
        var targets = gameBoard.Targets;
        var mapWidth = gameBoard.Width;
        var mapHeight = gameBoard.Height;
        double minDistance = double.MaxValue;

        foreach (Location target in targets)
        {
            double distanceToLeftEdge = target.X;
            double distanceToRightEdge = mapWidth - target.X;
            double distanceToTopEdge = target.Y;
            double distanceToBottomEdge = mapHeight - target.Y;

            double minDistanceToEdge = Math.Min(
                Math.Min(distanceToLeftEdge, distanceToRightEdge),
                Math.Min(distanceToTopEdge, distanceToBottomEdge)
            );

            if (minDistanceToEdge < minDistance)
            {
                minDistance = minDistanceToEdge;
            }
        }
        return minDistance;
    }
}
