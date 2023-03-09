namespace Roverlib.Models.Responses
{
    public class Location
    {
        public Location(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; private set; }
        public int Y { get; private set; }


        public override bool Equals(object obj)
        {
            if (obj is Location loc)
            {
                return loc.X == this.X && loc.Y == this.Y;
            }
            return false;
        }
    }
}