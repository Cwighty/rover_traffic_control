using System.Collections.Concurrent;
using Newtonsoft.Json;
using Priority_Queue;
using Roverlib.Models.Responses;

public class PathFinder
{
    private class Node : IComparable<Node>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Cost { get; set; }
        public int Heuristic { get; set; }
        public List<(int, int)> Path { get; set; }

        public int CompareTo(Node other)
        {
            if (other == null) return 1;
            return (Cost + Heuristic).CompareTo(other.Cost + other.Heuristic);
        }
    }

    public static (List<(int X, int Y)>, int) FindPath(
        Dictionary<(int X, int Y), int> map,
        (int X, int Y) start,
        (int X, int Y) target,
        Func<(int, int), (int, int), int> heuristicFunction = null,
        int mapOptimizationBuffer = 20,
        int straightDrivingIncentive = 0)
    {
        if (heuristicFunction == null)
        {
            heuristicFunction = ManhattanDistance;
        }

        map = GetSubmap(map, start, target, mapOptimizationBuffer);

        // Check if the start and target positions are within the map
        if (!map.ContainsKey(start) || !map.ContainsKey(target))
        {
            return (new List<(int X, int Y)>(), -1);
        }

        // A* algorithm to find most efficient path
        var heap = new PriorityQueue<Node, int>();
        heap.Enqueue(new Node { X = start.X, Y = start.Y, Cost = 0, Heuristic = heuristicFunction.Invoke(start, target), Path = new List<(int X, int Y)> { start } }, 0);
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
                    if (newPath.Count > 1 && (newX - newPath[newPath.Count - 2].Item1) * (newY - newPath[newPath.Count - 2].Item2) != 0)
                    { // add cost for not driving straight
                        newCost += straightDrivingIncentive;
                    }
                    newPath.Add((newX, newY));
                    heap.Enqueue(new Node { X = newX, Y = newY, Cost = newCost, Heuristic = newHeuristic, Path = newPath }, newCost + newHeuristic);
                }
            }
        }

        return (new List<(int X, int Y)>(), -1);
    }

    public static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
    { // as the cars drive
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    public static int EuclideanDistance((int X, int Y) a, (int X, int Y) b)
    { // as the crow flies
        return (int)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    public static Dictionary<(int, int), int> GetSubmap(
    Dictionary<(int, int), int> mapData,
    (int, int) start,
    (int, int) target,
    int width)
    {
        // Optimize the map by only including the cells that are within the buffer
        // Compute the slope and intercept 
        double m = (double)(target.Item2 - start.Item2) / (double)(target.Item1 - start.Item1);
        double b = (double)start.Item2 - m * (double)start.Item1;

        // Determine the range of rows and columns to include in the submap
        int minRow = Math.Min(start.Item1, target.Item1) - width;
        int maxRow = Math.Max(start.Item1, target.Item1) + width;
        int minCol = Math.Min(start.Item2, target.Item2) - width;
        int maxCol = Math.Max(start.Item2, target.Item2) + width;

        Dictionary<(int, int), int> submapData = new Dictionary<(int, int), int>();

        // Iterate over the rows and columns of the map and copy the cells that
        // fall within the range and the "buffer zone" to the submap
        for (int rowIdx = minRow; rowIdx <= maxRow; rowIdx++)
        {
            for (int colIdx = minCol; colIdx <= maxCol; colIdx++)
            {
                (int, int) pos = (rowIdx, colIdx);
                if (mapData.TryGetValue(pos, out int val))
                {
                    // check distance to see if it is within the buffer zone
                    double dist = Math.Abs(m * pos.Item1 - pos.Item2 + b) / Math.Sqrt(Math.Pow(m, 2) + 1);
                    if (dist <= width)
                    {
                        submapData[pos] = val;
                    }
                }
            }
        }

        return submapData;
    }

    public static Location GetNearestTarget(Location currentLocation, List<Location> targets)
    {
        var nearest = targets.OrderBy(t => Math.Abs(t.X - currentLocation.X) + Math.Abs(t.Y - currentLocation.Y)).First();
        return nearest;
    }

    public static int EstimateCostToFinish(Location currentLocation, List<Location> targets, double mapAvgDifficulty)
    {
        // Estimate the cost to finish from current location to all targets by multiplying the distance to travel from target to target by average difficulty of the map
        double totalCost = 0;
        var targetsCopy = new List<Location>(targets);
        while (targetsCopy.Count > 0)
        {
            var nearest = GetNearestTarget(currentLocation, targetsCopy);
            totalCost += ManhattanDistance((currentLocation.X, currentLocation.Y), (nearest.X, nearest.Y)) * mapAvgDifficulty;
            currentLocation = nearest;
            targetsCopy.Remove(nearest);
        }
        return (int)totalCost;
    }

}