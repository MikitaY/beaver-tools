namespace B.BeaverTools.Utils.Extensions;

public static class ElementExtensions
{
    public static XYZ GetMidPoint(this Element element, View view)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element), "Element cannot be null");

        if (view == null)
            throw new ArgumentNullException(nameof(view), "View cannot be null");

        var boundingBox = element.get_BoundingBox(view);

        if (boundingBox == null)
            throw new InvalidOperationException("BoundingBox is null");

        var minPoint = boundingBox.Min;
        var maxPoint = boundingBox.Max;

        return new XYZ(
            (minPoint.X + maxPoint.X) / 2,
            (minPoint.Y + maxPoint.Y) / 2,
            (minPoint.Z + maxPoint.Z) / 2
        );
    }

    public static XYZ GetLocationPoint(this Element element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element), "Element cannot be null.");

        var location = element.Location;

        switch (location)
        {
            case null:
                throw new InvalidOperationException("Element does not have a valid location.");
            case LocationPoint locationPoint:
                return locationPoint.Point;
            case LocationCurve locationCurve:
            {
                var curve = locationCurve.Curve;
                return curve.Evaluate(0.5, true);
            }
            default:
                throw new InvalidOperationException("Element does not have a valid location point or curve.");
        }
    }
}