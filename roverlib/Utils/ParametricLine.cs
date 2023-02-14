using System.Drawing;
using Roverlib.Models.Responses;

namespace Roverlib.Utils;

public class ParametricLine
{
    PointF p1;
    PointF p2;

    public ParametricLine(PointF p1, PointF p2)
    {
        this.p1 = p1;
        this.p2 = p2;
    }

    public PointF Fraction(float frac)
    {
        return new PointF(p1.X + frac * (p2.X - p1.X),
                           p1.Y + frac * (p2.Y - p1.Y));
    }

    public List<(int X, int Y)> GetDiscretePointsAlongLine()
    {
        var points = Enumerable.Range(0, Convert.ToInt32(GetDistance() / 2)).Select(p => Fraction((float)p / Convert.ToInt32(GetDistance() / 2)));
        return points.Select(p => (Convert.ToInt32(p.X), Convert.ToInt32(p.Y))).ToList();
    }

    public double GetDistance()
    {
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }
}


