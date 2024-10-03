namespace B.BeaverTools.Models;

public class BoundingBox2D(double minX, double minY, double maxX, double maxY)
{
    public double MinX { get; } = minX;
    public double MinY { get; } = minY;
    public double MaxX { get; } = maxX;
    public double MaxY { get; } = maxY;
}