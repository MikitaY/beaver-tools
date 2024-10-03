namespace B.BeaverTools.Utils.Extensions;

// ReSharper disable once InconsistentNaming
public static class XYZExtensions
{
    public static UV GetProjectionOntoPlan(this XYZ point3D, Plane plane)
    {
        var vectorFromOriginToPoint = point3D - plane.Origin;
        var distanceToPlane = plane.Normal.DotProduct(vectorFromOriginToPoint);

        var isPointInFrontOfPlane = (point3D - plane.Origin).DotProduct(plane.Normal) > 0;

        var projectedPoint = isPointInFrontOfPlane
            ? point3D - distanceToPlane * plane.Normal 
            : point3D + distanceToPlane * plane.Normal;
        
        var vectorFromOriginToProjectedPoint = projectedPoint - plane.Origin;
        var uCoord = vectorFromOriginToProjectedPoint.DotProduct(plane.XVec);
        var vCoord = vectorFromOriginToProjectedPoint.DotProduct(plane.YVec);
        
        return new UV(uCoord, vCoord);
    }
    
    public static Solid GetExtrusionSolid(this XYZ headPoint, View3D view3D)
    {
        var plane = view3D.GetViewPlane();
        var normal = plane.Normal;

        var tagPoint2D = headPoint.GetProjectionOntoPlan(plane);
        
        //TODO Check text width and set different boundary conditions 
        var p1 = new UV(tagPoint2D.U - 2.4, tagPoint2D.V - 1);
        var p2 = new UV(tagPoint2D.U - 2.4, tagPoint2D.V + 1);
        var p3 = new UV(tagPoint2D.U + 2.4, tagPoint2D.V + 1);
        var p4 = new UV(tagPoint2D.U + 2.4, tagPoint2D.V - 1);

        var gp1 = p1.GetXYZFromUVOnPlane(plane);
        var gp2 = p2.GetXYZFromUVOnPlane(plane);
        var gp3 = p3.GetXYZFromUVOnPlane(plane);
        var gp4 = p4.GetXYZFromUVOnPlane(plane);

        var gPoints = new XYZ[] 
        {
            gp1 - normal * 1000,
            gp2 - normal * 1000,
            gp3 - normal * 1000,
            gp4 - normal * 1000,
        };
        
        var baseProfile = new CurveLoop();
        baseProfile.Append(Line.CreateBound(gPoints[0], gPoints[1]));
        baseProfile.Append(Line.CreateBound(gPoints[1], gPoints[2]));
        baseProfile.Append(Line.CreateBound(gPoints[2], gPoints[3]));
        baseProfile.Append(Line.CreateBound(gPoints[3], gPoints[0]));
        
        var solid = GeometryCreationUtilities.CreateExtrusionGeometry(
            new List<CurveLoop> { baseProfile }, normal, 2000.0);

        return solid;
    }
    
    public static Outline GetUVOutline(this XYZ headPoint, View3D view3D)
    {
        var plane = view3D.GetViewPlane();
        var normal = plane.Normal;

        var tagPoint2D = headPoint.GetProjectionOntoPlan(plane);
        
        //TODO Check text width and set different boundary conditions 
        var pMin = new UV(tagPoint2D.U - 2.4, tagPoint2D.V - 1);
        var pMax = new UV(tagPoint2D.U + 2.4, tagPoint2D.V + 1);

        return new Outline(new XYZ(pMin.U, pMin.V, 0), new XYZ(pMax.U, pMax.V, 0));
    }
}