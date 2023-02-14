using Roverlib.Models.Responses;

namespace Roverlib.Models;

public class Board
{
    public Board(JoinResponse response)
    {
        LowResMap = response.lowResolutionMap.ToList();
        Width = response.lowResolutionMap.Max(x => x.upperRightX);
        Height = response.lowResolutionMap.Max(x => x.upperRightY);
        Target = (response.targetX, response.targetY);
        VisitedNeighbors = new();
    }
    public int Width { get; set; }
    public int Height { get; set; }
    public (int X, int Y) Target { get; set; }
    public List<LowResolutionMap> LowResMap { get; set; }
    public Dictionary<long, Neighbor> VisitedNeighbors { get; set; }
}
