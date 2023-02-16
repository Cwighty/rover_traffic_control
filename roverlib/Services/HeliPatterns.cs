public class HeliPatterns
{

  public static List<(int X, int Y)> GenerateCircle((int, int) center, int radius, int points)
  {
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

  public static List<(int x, int y)> GenerateSpiral((int X, int Y) center, int angle, int radius, int numPoints, int distanceBetween)
  {
    List<(int x, int y)> points = new List<(int x, int y)>();

    for (int i = 0; i < numPoints; i++)
    {
      double radians = angle * Math.PI / 180.0;
      int x = center.X + (int)(radius * Math.Cos(radians));
      int y = center.Y + (int)(radius * Math.Sin(radians));
      points.Add((x, y));
      angle += distanceBetween / radius;
      radius += distanceBetween / radius;
    }

    return points;
  }

  

  public static List<(int X, int Y)> GeneratePhyllotaxisSpiral((int X, int Y) center, int angle, int numPoints, int distanceBetween = 6)
{
    //https://www.desmos.com/calculator/risuha09iw
    List<(int X, int Y)> points = new List<(int X, int Y)>();

    double angleRadians = angle * Math.PI / 180.0;
    double divergenceAngle = Math.PI * (3 - Math.Sqrt(5));

    for (int a = 0; a < numPoints; a++)
    {
        double r = distanceBetween * Math.Sqrt(a);
        double phi = a * divergenceAngle;

        int x = (int)(r * Math.Cos(phi + angleRadians)) + center.X;
        int y = (int)(r * Math.Sin(phi + angleRadians)) + center.Y;

        points.Add((x, y));
    }

    return points;
}

  public static List<(int x, int y)> GenerateClockHand((int X, int Y) center, int radius, int angle, int numPoints)
  {
    List<(int x, int y)> points = new List<(int x, int y)>();
    double radians = angle * Math.PI / 180.0;
    int endX = center.X + (int)(radius * Math.Cos(radians));
    int endY = center.Y + (int)(radius * Math.Sin(radians));
    int deltaX = Math.Abs(endX - center.X);
    int deltaY = Math.Abs(endY - center.Y);
    int steps = numPoints;
    double xIncrement = (double)(endX - center.X) / steps;
    double yIncrement = (double)(endY - center.Y) / steps;

    for (int i = 0; i <= steps; i++)
    {
      int x = center.X + (int)(i * xIncrement);
      int y = center.Y + (int)(i * yIncrement);
      points.Add((x, y));
    }

    return points;
  }

  public static List<(int x, int y)> GenerateTwoArmSpiral((int X, int Y) center, int innerRadius, int outerRadius, int numPoints) {
    // 
    List<(int x, int y)> points = new List<(int x, int y)>();
    
    for (int i = 0; i <= numPoints; i++) {
        double t = (double)i / numPoints;
        double angle = t * Math.PI * 4;
        
        int x = (int)(center.X + (innerRadius + (outerRadius - innerRadius) * t) * Math.Cos(angle));
        int y = (int)(center.Y + (innerRadius + (outerRadius - innerRadius) * t) * Math.Sin(angle));
        
        if (i % (numPoints / 4) == 0) {
            angle += Math.PI / 2;
            int cx = (int)(center.X + outerRadius * Math.Cos(angle));
            int cy = (int)(center.Y + outerRadius * Math.Sin(angle));
            points.Add((cx, cy));
        }
        
        points.Add((x, y));
    }
    
    return points;
}


  public static List<(int X, int Y)> RotateList(List<(int X, int Y)> list, int rotations)
  {
    var rotated = new List<(int X, int Y)>();
    for (int i = 0; i < list.Count; i++)
    {
      var index = (i + rotations) % list.Count;
      rotated.Add(list[index]);
    }
    return rotated;
  }
}