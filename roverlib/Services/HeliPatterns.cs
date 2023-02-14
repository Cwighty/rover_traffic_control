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

 
  public static List<(int X, int Y)> RotateList(List<(int X, int Y)> list, int rotations){
    var rotated = new List<(int X, int Y)>();
    for (int i = 0; i < list.Count; i++)
    {
      var index = (i + rotations) % list.Count;
      rotated.Add(list[index]);
    }
    return rotated;
  }
}