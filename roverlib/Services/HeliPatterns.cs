public class HeliPatterns {

  public static List<(int X, int Y)> GetStartingCircle((int, int) center, int radius, int points){
    var circle = new List<(int X, int Y)>();
    for (int i = 0; i < points; i++)
    {
      var angle = (2 * Math.PI / points) * i;
      var x = (int)(center.Item1 + radius * Math.Cos(angle));
      var y = (int)(center.Item2 + radius * Math.Sin(angle));
      circle.Add((x, y));
    }
    return circle;
  }
}