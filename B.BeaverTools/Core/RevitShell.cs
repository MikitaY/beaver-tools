using B.BeaverTools.Models;
using B.BeaverTools.Utils.Extensions;

namespace B.BeaverTools.Core;

public static class RevitShell
{
      /// <summary>
    /// This method needs a transaction
    /// </summary>
    /// <param name="view">View for position tags</param>
    public static void TaggedAllEquipmentAndPipes(View view)
    {
        if (view is not View3D { IsLocked: true }) return;
        var document = view.Document;

        var mechanicalEquipmentFilter =
            new ElementCategoryFilter(BuiltInCategory.OST_MechanicalEquipment);
        var pipesFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);

        var mechanicalCollector = new FilteredElementCollector(document, view.Id)
            .WherePasses(mechanicalEquipmentFilter);

        var pipesCollector = new FilteredElementCollector(document, view.Id)
            .WherePasses(pipesFilter)
            .Where(pipe => (pipe.Location as LocationCurve)?.Curve.Length > 7);

        var allElements = mechanicalCollector.ToList();
        allElements.AddRange(pipesCollector.ToList());

        foreach (var element in allElements)
        {
            var locationPoint = element.GetLocationPoint();

            var newTag = IndependentTag.Create(
                document,
                view.Id,
                new Reference(element),
                false,
                TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.Horizontal,
                locationPoint);
        }
    }

    /// <summary>
    /// This method needs a transaction
    /// </summary>
    /// <param name="view">View where tags move</param>
    public static void MoveTagFirstStep(View view)
    {
        if (view is not View3D { IsLocked: true }) return;
        var document = view.Document;

        var viewScale = document.ActiveView.Scale;
        var viewCentralPoint = GetViewCenter(document.ActiveView);
        var tagCollector = new FilteredElementCollector(document, view.Id)
            .OfClass(typeof(IndependentTag));
#if REVIT2023_OR_GREATER
        var equipmentTagIds = tagCollector
            .Where(tag => tag.Category.BuiltInCategory is BuiltInCategory.OST_MechanicalEquipmentTags)
            .Select(tag => tag.Id)
            .ToList();

        var pipeTagIds = tagCollector
            .Where(tag => tag.Category.BuiltInCategory is BuiltInCategory.OST_PipeTags)
            .Select(tag => tag.Id)
            .ToList();
#else
        var equipmentTagIds = tagCollector
            .Where(tag => tag.Category.Id.IntegerValue is (int)BuiltInCategory.OST_MechanicalEquipmentTags)
            .Select(tag => tag.Id)
            .ToList();

        var pipeTagIds = tagCollector
            .Where(tag => tag.Category.Id.IntegerValue is (int)BuiltInCategory.OST_PipeTags)
            .Select(tag => tag.Id)
            .ToList();
#endif
        var baseOffset = Consts.BaseOffset * viewScale;

        // Move mechanical equipment tags
        foreach (var tag in equipmentTagIds.Select(tagId => document.GetElement(tagId)).OfType<IndependentTag>())
        {
            Move3DTag(view, tag, baseOffset);
        }

        var pipeTags = pipeTagIds
            .Select(tagId => document.GetElement(tagId))
            .OfType<IndependentTag>()
            .ToList();

        while (pipeTags.Count != 0)
        {
            var pipeTag = pipeTags.First();
            pipeTags.Remove(pipeTag);

            Move3DTag(view, pipeTag, -baseOffset);
        }
    }

    private static void Move3DTag(View view, IndependentTag iTag, double baseOffset)
    {
        if (view is not View3D view3D) return;
        var orientation = view3D.GetOrientation();

        var eyePosition = orientation.EyePosition;
        var forwardDirection = orientation.ForwardDirection;
        var upDirection = orientation.UpDirection;

        var moveUpDistance = baseOffset;
        var moveLeftDistance = baseOffset;

        var rightDirection = forwardDirection.CrossProduct(upDirection).Normalize();
        var upDirectionNormalized = upDirection.Normalize();

        var moveUp = upDirectionNormalized * moveUpDistance;
        var moveLeft = -rightDirection * moveLeftDistance;

        var currentPosition = iTag.TagHeadPosition;
        var newPosition = currentPosition + moveUp + moveLeft;
        iTag!.Location.Move(iTag.TagHeadPosition - newPosition);
    }

    public static IList<Tag> Tags { get; set; } = [];

    public static Element GetNearestPipesTag(Outline outline)
    {
        var minPoint = new XYZ(outline.MinimumPoint.X - 1, outline.MinimumPoint.Y - 1, outline.MinimumPoint.Z);
        var maxPoint = new XYZ(outline.MaximumPoint.X - 1, outline.MaximumPoint.Y - 1, outline.MaximumPoint.Z);
        var document = RevitApp.ActiveUiDocument.Document;
        var view = document.ActiveView;
#if REVIT2023_OR_GREATER
        var collector = new FilteredElementCollector(document, view.Id)
            .OfClass(typeof(IndependentTag))
            .Where(element => element.Category.BuiltInCategory is BuiltInCategory.OST_PipeTags);
#else
        var collector = new FilteredElementCollector(document, view.Id)
            .OfClass(typeof(IndependentTag))
            .Where(element => element.Category.Id.IntegerValue is (int)BuiltInCategory.OST_PipeTags);
#endif
        return (from tag in collector.OfType<IndependentTag>()
            let boundingBox = tag.get_BoundingBox(view)
            where boundingBox != null
            let tagCenter = (boundingBox.Min + boundingBox.Max) / 2
            where tagCenter.X >= minPoint.X && tagCenter.X <= maxPoint.X && tagCenter.Y >= minPoint.Y &&
                  tagCenter.Y <= maxPoint.Y
            select tag).Cast<Element>().FirstOrDefault();
    }

    public static IList<Element> GetTagsInsideRectangle(Outline outline)
    {
        var minPoint = outline.MinimumPoint;
        var maxPoint = outline.MaximumPoint;

        var document = RevitApp.Document;
        var view = document.ActiveView;

        var collector = new FilteredElementCollector(document, view.Id)
            .OfClass(typeof(IndependentTag));

        return (from tag in collector.OfType<IndependentTag>()
            let boundingBox = tag.get_BoundingBox(view)
            where boundingBox != null
            let tagCenter = (boundingBox.Min + boundingBox.Max) / 2
            where tagCenter.X >= minPoint.X && tagCenter.X <= maxPoint.X && tagCenter.Y >= minPoint.Y &&
                  tagCenter.Y <= maxPoint.Y
            select tag).Cast<Element>().ToList();
    }

    public static XYZ GetViewCenter(View view)
    {
        var cropBox = view.CropBox;
        var center = (cropBox.Max + cropBox.Min) / 2;
        return center;
    }

    public static XYZ Get3DViewCenter(View view)
    {
        if (view is not View3D view3D) throw new Exception("Активный вид не является 3D видом");

        var bbox = view3D.GetSectionBox();

        return (bbox.Min + bbox.Max) / 2;
    }

    public static XYZ GetFirstEquipmentPoint(XYZ viewCentralPoint, XYZ tagPoint, int viewScale)
    {
        var baseOffset = Consts.BaseOffset * viewScale;
        var isRight = viewCentralPoint.X < tagPoint.X;
        var isAbove = viewCentralPoint.Y < tagPoint.Y;

        return isRight switch
        {
            true when isAbove => new XYZ(baseOffset, baseOffset, 0),
            false when isAbove => new XYZ(-baseOffset, baseOffset, 0),
            false when !isAbove => new XYZ(-baseOffset, -baseOffset, 0),
            true when !isAbove => new XYZ(baseOffset, -baseOffset, 0),
            _ => tagPoint
        };
    }

    public static XYZ GetUpPipePoint(XYZ tagPoint, int viewScale)
    {
        var baseOffset = Consts.BaseOffset * viewScale;
        return new XYZ(0, baseOffset, 0);
    }

    public static XYZ GetDownPipePoint(XYZ tagPoint, int viewScale)
    {
        var baseOffset = Consts.BaseOffset * viewScale;
        return new XYZ(0, -baseOffset, 0);
    }

    public static XYZ GetLeftPipePoint(XYZ tagPoint, int viewScale)
    {
        var baseOffset = Consts.BaseOffset * viewScale;
        return new XYZ(-baseOffset, 0, 0);
    }

    public static XYZ GetRightPipePoint(XYZ tagPoint, int viewScale)
    {
        var baseOffset = Consts.BaseOffset * viewScale;
        return new XYZ(baseOffset, 0, 0);
    }
}