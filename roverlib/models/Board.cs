using System.Collections.Concurrent;
using Roverlib.Models.Responses;

namespace Roverlib.Models;

public class Board
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<LowResolutionMap> LowResMap { get; set; }
    public ConcurrentDictionary<long, Neighbor> VisitedNeighbors { get; set; }
    public List<Location> Targets { get; internal set; }

    public int AverageDifficulty => (int)LowResMap.Average(x => x.averageDifficulty);

    public Board(JoinResponse response)
    {
        LowResMap = response.lowResolutionMap.ToList();
        Width = response.lowResolutionMap.Max(x => x.upperRightX);
        Height = response.lowResolutionMap.Max(x => x.upperRightY);
        Targets = response.targets;
        VisitedNeighbors = new();
        Console.WriteLine($"Board size: {Width}x{Height}");
        Console.WriteLine($"Average difficulty: {AverageDifficulty}");
    }
}
