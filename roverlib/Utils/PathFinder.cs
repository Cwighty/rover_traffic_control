
using System.Collections.Concurrent;
using Roverlib.Models.Responses;

namespace Roverlib.Utils;
public class PathFinder
{
    private class Node : IComparable<Node>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Cost { get; set; }
        public List<(int, int)> Path { get; set; }

        public int CompareTo(Node other)
        {
            return Cost.CompareTo(other.Cost);
        }
    }

    public static List<(int, int)> FindPathAStar(ConcurrentDictionary<long, Neighbor> neighbors, (int, int) start, (int, int) end)
    {
        var heap = new SortedSet<Node>();
        heap.Add(new Node { X = start.Item1, Y = start.Item2, Cost = 0, Path = new List<(int, int)> { start } });
        var visited = new HashSet<(int, int)>();
        while (heap.Count > 0)
        {
            var node = heap.Min;
            heap.Remove(node);
            if (visited.Contains((node.X, node.Y)))
            {
                continue;
            }
            visited.Add((node.X, node.Y));
            if ((node.X, node.Y).Equals(end))
            {
                return node.Path;
            }
            foreach (var dir in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
            {
                int newX = node.X + dir.Item1;
                int newY = node.Y + dir.Item2;
                var n = new Neighbor { X = newX, Y = newY };
                if (neighbors.ContainsKey(n.HashToLong()))
                {
                    int newCost = node.Cost + n.Difficulty;
                    var newPath = new List<(int, int)>(node.Path)
                    {
                        (newX, newY)
                    };
                    heap.Add(new Node { X = newX, Y = newY, Cost = newCost, Path = newPath });
                }
            }
        }
        return new();
    }
}
