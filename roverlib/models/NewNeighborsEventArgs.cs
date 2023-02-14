using Roverlib.Models.Responses;
namespace Roverlib.Models;

public class NewNeighborsEventArgs : EventArgs
{
    public NewNeighborsEventArgs(IEnumerable<Neighbor> neighbors)
    {
        Neighbors = neighbors;
    }
    public IEnumerable<Neighbor> Neighbors { get; set; }
}
