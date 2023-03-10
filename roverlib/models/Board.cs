using System.Collections.Concurrent;
using Roverlib.Models.Responses;

namespace Roverlib.Models;

public class Board
{
    public Board(JoinResponse response)
    {
        LowResMap = response.lowResolutionMap.ToList();
        Width = response.lowResolutionMap.Max(x => x.upperRightX);
        Height = response.lowResolutionMap.Max(x => x.upperRightY);
        Targets = response.targets;
        VisitedNeighbors = new();
    }

    public static ConcurrentDictionary<long, Neighbor> InitializeMap(List<LowResolutionMap> lowResolutionMap)
    {
        var map = new ConcurrentDictionary<long, Neighbor>();
        foreach (var lowResMap in lowResolutionMap)
        {
            for (int x = lowResMap.lowerLeftX; x <= lowResMap.upperRightX; x++)
            {
                for (int y = lowResMap.lowerLeftY; y <= lowResMap.upperRightY; y++)
                {
                    var n = new Neighbor()
                    {
                        X = x,
                        Y = y,
                        Difficulty = lowResMap.averageDifficulty
                    };
                    map.TryAdd(n.HashToLong(), n);
                };
            }
        }
        return map;
    }

    public int Width { get; set; }
    public int Height { get; set; }
    public List<LowResolutionMap> LowResMap { get; set; }
    public ConcurrentDictionary<long, Neighbor> VisitedNeighbors { get; set; }
    public List<Location> Targets { get; internal set; }
}
