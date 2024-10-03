namespace B.BeaverTools.Utils.Extensions;

public static class View3DExtensions
{
    public static Plane GetViewPlane(this View3D view3D)
    {
        if (view3D.IsPerspective) throw new Exception("The view cannot be perspective");
        
        var viewDirection = view3D.ViewDirection;
        var upDirection = view3D.UpDirection;

        viewDirection = viewDirection.Normalize();
        upDirection = upDirection.Normalize();

        var xVec = upDirection.CrossProduct(viewDirection);
        xVec = xVec.Normalize();
        
        return Plane.CreateByOriginAndBasis(view3D.Origin, xVec, upDirection);
    }
}