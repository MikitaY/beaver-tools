using Autodesk.Revit.DB.Plumbing;
using B.BeaverTools.Core;

namespace B.BeaverTools.Utils.Extensions;

public static class IndependentTagExtensions
{
    public static Outline GetOutline(this IndependentTag tag)
    {
        var view = tag.Document.GetElement(tag.OwnerViewId) as View;
        var boundingBox = tag.get_BoundingBox(view);

        const double trim = 0.7;

        var minY = boundingBox.Min.Y < 0 ? boundingBox.Min.Y + trim : boundingBox.Min.Y - trim;
        var maxY = boundingBox.Max.Y < 0 ? boundingBox.Max.Y - trim : boundingBox.Max.Y + trim;

        var outline = new Outline(
            new XYZ(boundingBox.Min.X, minY, tag.TagHeadPosition.Z - 1000),
            new XYZ(boundingBox.Max.X, maxY, tag.TagHeadPosition.Z + 1000));

        return outline;
    }

    public static Outline GetOutline3D(this IndependentTag tag)
    {
        var view = tag.Document.GetElement(tag.OwnerViewId) as View;
        var boundingBox = tag.get_BoundingBox(view);

        var outline = new Outline(
            new XYZ(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z),
            new XYZ(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z));

        return outline;
    }

    public static Outline GetSuperOutline(this IndependentTag tag)
    {
        var baseTag = RevitShell.Tags.FirstOrDefault(f => f.TagId == tag.Id);
        if (baseTag == null) throw new Exception($"Tag {tag.Id} not found");

        var baseMinPoint = baseTag.StartTagOutline.MinimumPoint;
        var baseMaxPoint = baseTag.StartTagOutline.MaximumPoint;

        var headPosition = tag.TagHeadPosition;

        var moveVector = baseTag.StartTagHeadPosition - headPosition;

        return new Outline(
            baseMinPoint - moveVector,
            baseMaxPoint - moveVector);
    }

    public static XYZ GetTaggedElementCenter(this IndependentTag tag, View view)
    {
#if REVIT2022_OR_GREATER
        var taggedReferences = tag.GetTaggedReferences();
        if (taggedReferences.Count == 0) return null;
        var taggedElement = tag.Document.GetElement(taggedReferences.First());
#else
        var taggedReference = tag.GetTaggedReference();
        if (taggedReference is null) return null;
        var taggedElement = tag.Document.GetElement(taggedReference);
#endif
        var boundingBox = taggedElement.get_BoundingBox(view);

        var centerX = (boundingBox.Max.X + boundingBox.Min.X) / 2;
        var centerY = (boundingBox.Max.Y + boundingBox.Min.Y) / 2;
        var centerZ = (boundingBox.Max.Z + boundingBox.Min.Z) / 2;

        return new XYZ(centerX, centerY, centerZ);
    }

    public static XYZ GetCircleMoveVector(this IndependentTag tag, double rotationAngel = Math.PI / 6,
        double offset = 0.0)
    {
        offset = Consts.Offset;
        var document = RevitApp.ActiveUiDocument.Document;
        //TODO maybe its mo right
        //var roundCenter = tag.GetTaggedElementCenter(document.ActiveView);
#if REVIT2022_OR_GREATER
        var taggedReferences = tag.GetTaggedReferences();
        if (taggedReferences.Count == 0) throw new NullReferenceException();
        var taggedElement = tag.Document.GetElement(taggedReferences.First());
#else
        var taggedReference = tag.GetTaggedReference();
        if (taggedReference is null) throw new NullReferenceException();
        var taggedElement = tag.Document.GetElement(taggedReference);
#endif
        XYZ roundCenter;

        if (taggedElement is Pipe)
        {
            var boundingBox = taggedElement.get_BoundingBox(document.ActiveView);
            roundCenter = new XYZ(
                (boundingBox.Min.X + boundingBox.Max.X) / 2,
                (boundingBox.Min.Y + boundingBox.Max.Y) / 2,
                tag.TagHeadPosition.Z);
        }
        else
        {
            var locationPoint = taggedElement.Location as LocationPoint ?? throw new NullReferenceException();
            roundCenter = locationPoint.Point;
        }

        var startPoint = new XYZ(tag.TagHeadPosition.X, tag.TagHeadPosition.Y, 0);
        if (offset != 0)
        {
            var direction = (startPoint - roundCenter).Normalize();
            startPoint += direction * offset;
        }

        var newX = roundCenter.X + (startPoint.X - roundCenter.X) * Math.Cos(rotationAngel) -
                   (startPoint.Y - roundCenter.Y) * Math.Sin(rotationAngel);
        var newY = roundCenter.Y + (startPoint.X - roundCenter.X) * Math.Sin(rotationAngel) +
                   (startPoint.Y - roundCenter.Y) * Math.Cos(rotationAngel);
        return new XYZ(newX, newY, 0) - startPoint;
    }

    public static bool IsPipeHorizontal(this IndependentTag tag)
    {
        var document = RevitApp.ActiveUiDocument.Document;

#if REVIT2022_OR_GREATER
        var taggedPipe = tag.GetTaggedReferences();
        var pipe = tag.Document.GetElement(taggedPipe.First());
        if (pipe == null || pipe.Category.BuiltInCategory != BuiltInCategory.OST_PipeCurves) throw new Exception();
#else
        var taggedPipe = tag.GetTaggedReference();
        var pipe = tag.Document.GetElement(taggedPipe);
        if (pipe == null || pipe.Category.Id.IntegerValue is (int)BuiltInCategory.OST_PipeCurves) throw new Exception();
#endif
        if (pipe.Location is not LocationCurve locationCurve) throw new Exception();

        var start = locationCurve.Curve.GetEndPoint(0);
        var end = locationCurve.Curve.GetEndPoint(1);

        return Math.Abs(start.X - end.X) > Math.Abs(start.Y - end.Y);
    }

    public static Solid GetExtrusionSolid(this IndependentTag tag, View3D view3D)
    {
        var plane = view3D.GetViewPlane();
        var normal = plane.Normal;

        var tagPoint2D = tag.TagHeadPosition.GetProjectionOntoPlan(plane);

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
}