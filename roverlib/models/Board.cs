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

    public Board(JoinResponse response)
    {
        LowResMap = response.lowResolutionMap.ToList();
        Width = response.lowResolutionMap.Max(x => x.upperRightX);
        Height = response.lowResolutionMap.Max(x => x.upperRightY);
        Targets = response.targets;
        VisitedNeighbors = new();
    }
}
