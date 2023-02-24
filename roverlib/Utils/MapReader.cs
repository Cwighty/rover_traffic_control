using System.Collections.Concurrent;
using Newtonsoft.Json;
using Priority_Queue;
public class MapReader
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
    public static Dictionary<(int X, int Y), int> ReadMap(string mapPath)
    {
        var reader = new StreamReader(mapPath);
        string json = reader.ReadToEnd();
        int[,] map = JsonConvert.DeserializeObject<int[,]>(json);
        var map2 = new Dictionary<(int X, int Y), int>();
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                map2.Add((x, y), map[x, y]);
            }
        }
        return map2;
    }

    public static void WriteMap(ConcurrentDictionary<int, int> map, string mapPath)
    {
        string json = JsonConvert.SerializeObject(map);
        File.WriteAllText(mapPath, json);
    }


    // public static List<(int X, int Y)> FindPath(Dictionary<(int X, int Y), int> map, (int X, int Y) start, (int X, int Y) target)
    // {
    //     map = GetSubmap(map, start, target, 10);
    //     // Check if the start and target positions are within the map
    //     if (!map.ContainsKey(start) || !map.ContainsKey(target))
    //     {
    //         return new List<(int X, int Y)>();
    //     }

    //     // A* algorithm to find most efficient path
    //     var heap = new SimplePriorityQueue<Node, int>();
    //     heap.Enqueue(new Node { X = start.X, Y = start.Y, Cost = 0, Path = new List<(int X, int Y)> { start } }, 0);
    //     var visited = new HashSet<(int X, int Y)>();
    //     while (heap.Count > 0)
    //     {
    //         var node = heap.Dequeue();
    //         if (visited.Contains((node.X, node.Y)))
    //         {
    //             continue;
    //         }
    //         visited.Add((node.X, node.Y));
    //         if (node.X == target.X && node.Y == target.Y)
    //         {
    //             return node.Path;
    //         }
    //         foreach (var dir in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
    //         {
    //             int newX = node.X + dir.Item1;
    //             int newY = node.Y + dir.Item2;
    //             var n = (newX, newY);
    //             if (map.ContainsKey(n) && !visited.Contains(n))
    //             {
    //                 int newCost = node.Cost + map[n];
    //                 var newPath = new List<(int X, int Y)>(node.Path);
    //                 newPath.Add((newX, newY));
    //                 heap.Enqueue(new Node { X = newX, Y = newY, Cost = newCost, Path = newPath }, newCost);
    //             }
    //         }
    //     }

    //     // If no path was found, return an empty list
    //     return new List<(int X, int Y)>();
    // }


    public static List<(int X, int Y)> FindPath(Dictionary<(int X, int Y), int> map, (int X, int Y) start, (int X, int Y) target)
    {
        map = GetSubmap(map, start, target, 10);

        // Check if the start and target positions are within the map
        if (!map.ContainsKey(start) || !map.ContainsKey(target))
        {
            return new List<(int X, int Y)>();
        }

        // A* algorithm to find most efficient path
        var heap = new PriorityQueue<Node, int>();
        heap.Enqueue(new Node { X = start.X, Y = start.Y, Cost = 0, Heuristic = ManhattanDistance(start, target), Path = new List<(int X, int Y)> { start } }, 0);
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
                return node.Path;
            }
            foreach (var dir in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int newX = node.X + dir.Item1;
                int newY = node.Y + dir.Item2;
                var n = (newX, newY);
                if (map.ContainsKey(n) && !visited.Contains(n))
                {
                    int newCost = node.Cost + map[n];
                    int newHeuristic = ManhattanDistance(n, target);
                    var newPath = new List<(int X, int Y)>(node.Path);
                    if (newPath.Count > 1 && (newX - newPath[newPath.Count - 2].Item1) * (newY - newPath[newPath.Count - 2].Item2) != 0)
                    {
                        newCost += 7; // Add extra cost for changing direction
                    }
                    newPath.Add((newX, newY));
                    heap.Enqueue(new Node { X = newX, Y = newY, Cost = newCost, Heuristic = newHeuristic, Path = newPath }, newCost + newHeuristic);
                }
            }
        }

        // If no path was found, return an empty list
        return new List<(int X, int Y)>();
    }

    private static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }


    public static Dictionary<(int, int), int> GetSubmap(
    Dictionary<(int, int), int> mapData,
    (int, int) start,
    (int, int) target,
    int width)
    {
        // Compute the slope and intercept of the line connecting start and target
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
                    // Compute the distance between the current cell and the line connecting startPos and targetPos
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
    public static (int X, int Y) FindBestStart(int[,] map, (int X, int Y) target)
    {
        var heap = new SortedSet<Node>();
        heap.Add(new Node { X = target.X, Y = target.Y, Cost = 0, Path = new List<(int X, int Y)> { target } });
        var visited = new HashSet<(int X, int Y)>();
        while (heap.Count > 0)
        {
            var node = heap.Min;
            heap.Remove(node);
            if (visited.Contains((node.X, node.Y)))
            {
                continue;
            }
            visited.Add((node.X, node.Y));
            if (map[node.X, node.Y] == 0)
            {
                return (node.X, node.Y);
            }
            foreach (var dir in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int newX = node.X + dir.Item1;
                int newY = node.Y + dir.Item2;
                if (map[newX, newY] != 0)
                {
                    int newCost = node.Cost + map[newX, newY];
                    var newPath = new List<(int X, int Y)>(node.Path)
                    {
                        (newX, newY)
                    };
                    heap.Add(new Node { X = newX, Y = newY, Cost = newCost, Path = newPath });
                }
            }
        }
        return (0, 0);
    }
}