namespace Roverlib.Services
{
    public static class ListExtensions
    {
        public static Queue<(int X, int Y)> ToQueue(this IEnumerable<(int x, int y)> points)
        {
            Queue<(int x, int y)> queue = new();
            foreach (var p in points)
            {
                queue.Enqueue(p);
            }
            return queue;
        }
    }

}
