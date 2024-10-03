namespace B.BeaverTools.Utils.Extensions;

// ReSharper disable once InconsistentNaming
public static class UVExtensions
{
    public static XYZ GetXYZFromUVOnPlane(this UV uv, Plane plane)
    {
        var xVec = plane.XVec;
        var yVec = plane.YVec;
        
        var origin = plane.Origin;
        var pointOnPlane = origin + uv.U * xVec + uv.V * yVec;

        return pointOnPlane;
    }
}