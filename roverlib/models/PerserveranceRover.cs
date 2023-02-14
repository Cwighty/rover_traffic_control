using Roverlib.Models.Responses;

namespace Roverlib.Models
{
    public class PerserveranceRover
    {
        public PerserveranceRover(JoinResponse response)
        {
            Location = (response.startingX, response.startingY);
            Orientation = Enum.TryParse<Orientation>(response.orientation, out var orient) ? orient : Orientation.North;
        }

        public (int X, int Y) Location { get; set; }
        public string CurrentLocation { get => $"{Location.X}, {Location.Y}"; }
        public Orientation Orientation { get; set; }
        public int Battery { get; set; }
    }
}
