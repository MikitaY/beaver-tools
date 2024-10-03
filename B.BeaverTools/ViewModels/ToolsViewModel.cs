using System.Windows;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using B.BeaverTools.Core;
using B.BeaverTools.Models;
using B.BeaverTools.Utils.Extensions;
using B.BeaverTools.ViewModels.Contracts;
using Microsoft.Extensions.Logging;

namespace B.BeaverTools.ViewModels;

public sealed partial class ToolsViewModel(ILogger<ToolsViewModel> logger) : ObservableObject, IToolsViewModel
{
private readonly Document _document = RevitApp.ActiveUiDocument.Document;
    private readonly View _view = RevitApp.ActiveUiDocument.ActiveView;

    [RelayCommand]
    private void TagAll()
    {
        if (_view is not ViewPlan) return;

        if (RevitShell.Tags.Count > 0) RevitShell.Tags.Clear();

        var mechanicalEquipmentFilter =
            new ElementCategoryFilter(BuiltInCategory.OST_MechanicalEquipment);
        var pipesFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);

        var mechanicalCollector = new FilteredElementCollector(_document, _view.Id)
            .WherePasses(mechanicalEquipmentFilter);

        var pipesCollector = new FilteredElementCollector(_document, _view.Id)
            .WherePasses(pipesFilter)
            .Where(pipe => (pipe.Location as LocationCurve)?.Curve.Length > 4);

        var allElements = mechanicalCollector.ToList();
        allElements.AddRange(pipesCollector.ToList());

        foreach (var element in allElements)
        {
            var locationPoint = element.GetLocationPoint();

            using var tr = new Transaction(_document, "Add tag");
            tr.Start();
            var newTag = IndependentTag.Create(
                _document, _view.Id, new Reference(element), false, TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.Horizontal, locationPoint);
            tr.Commit();
            RevitShell.Tags.Add(new Tag(newTag));
        }

        var coll = RevitShell.Tags;
    }

    [RelayCommand]
    private void Place()
    {
        if (_view is not ViewPlan) return;

        var viewScale = _document.ActiveView.Scale;
        var viewCentralPoint = RevitShell.GetViewCenter(_document.ActiveView);
        var tagCollector = new FilteredElementCollector(_document, _view.Id)
            .OfClass(typeof(IndependentTag));
#if REVIT2023_OR_GREATER
        var equipmentTagIds = tagCollector
            .Where(tag => tag.Category.BuiltInCategory == BuiltInCategory.OST_MechanicalEquipmentTags)
            .Select(tag => tag.Id)
            .ToList();

        var pipeTagIds = tagCollector
            .Where(tag => tag.Category.BuiltInCategory == BuiltInCategory.OST_PipeTags)
            .Select(tag => tag.Id)
            .ToList();

#else
        var equipmentTagIds = tagCollector
            .Where(e => e.Category.Id.IntegerValue is (int)BuiltInCategory.OST_MechanicalEquipment)
            .Select(tag => tag.Id)
            .ToList();

        var pipeTagIds = tagCollector
            .Where(e => e.Category.Id.IntegerValue is (int)BuiltInCategory.OST_PipeCurves)
            .Select(tag => tag.Id)
            .ToList();
#endif

        // Move mechanical equipmen tags
        foreach (var tag in equipmentTagIds.Select(tagId => _document.GetElement(tagId)).OfType<IndependentTag>())
        {
            var newTagPoint = RevitShell.GetFirstEquipmentPoint(viewCentralPoint, tag.TagHeadPosition, viewScale);

            using var tr = new Transaction(_document, "Move tag");
            tr.Start();
            tag!.Location.Move(newTagPoint);
            tr.Commit();
        }

        var pipeTag = pipeTagIds
            .Select(tagId => _document.GetElement(tagId))
            .OfType<IndependentTag>()
            .ToList();

        while (pipeTag.Count != 0)
        {
            var mainTag = pipeTag.Last();
            pipeTag.RemoveAt(pipeTag.Count - 1);

            var outline = mainTag.GetOutline();
            var nearestTag = RevitShell.GetNearestPipesTag(outline) as IndependentTag ?? null;

            if (nearestTag == null || (mainTag.IsPipeHorizontal() != nearestTag.IsPipeHorizontal()))
            {
                var newTagPoint = mainTag.IsPipeHorizontal()
                    ? RevitShell.GetUpPipePoint(mainTag.TagHeadPosition, viewScale)
                    : RevitShell.GetLeftPipePoint(mainTag.TagHeadPosition, viewScale);

                using var tr = new Transaction(_document, "Move tag");
                tr.Start();
                mainTag.Move(newTagPoint);
                tr.Commit();
                continue;
            }

            if (mainTag.IsPipeHorizontal() && nearestTag.IsPipeHorizontal())
            {
                var newMainTagPoint = RevitShell.GetUpPipePoint(mainTag.TagHeadPosition, viewScale);
                var newNearestTagPoint = RevitShell.GetDownPipePoint(mainTag.TagHeadPosition, viewScale);

                using var tr = new Transaction(_document, "Move tag");
                tr.Start();
                mainTag.Move(newMainTagPoint);
                tr.Commit();
                tr.Start();
                nearestTag.Move(newNearestTagPoint);
                tr.Commit();
                pipeTag = pipeTag.Where(tag => tag.Id != nearestTag.Id).ToList();
            }
            else
            {
                var newMainTagPoint = RevitShell.GetLeftPipePoint(mainTag.TagHeadPosition, viewScale);
                var newNearestTagPoint = RevitShell.GetRightPipePoint(mainTag.TagHeadPosition, viewScale);

                using var tr = new Transaction(_document, "Move tag");
                tr.Start();
                mainTag.Move(newMainTagPoint);
                tr.Commit();

                tr.Start();
                nearestTag.Move(newNearestTagPoint);
                tr.Commit();

                pipeTag = pipeTag.Where(tag => tag.Id != nearestTag.Id).ToList();
            }
        }
    }

    [RelayCommand]
    private void SolveAllTag()
    {
        try
        {
            if (_view is not ViewPlan) return;

            var tagCollector = new FilteredElementCollector(_document, _view.Id)
                .OfClass(typeof(IndependentTag))
                .ToElements();

            foreach (var element in tagCollector)
            {
                if (element is not IndependentTag tag) continue;
                var outline = tag.GetSuperOutline();
                var intersectsFilter = new BoundingBoxIntersectsFilter(outline);
                var overlayTags = RevitShell.Tags
                    .Where(w => w.StartTagOutline.Intersects(outline, 0.001))
                    .ToList();
#if REVIT2023_OR_GREATER
                var selectedElements = new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .WherePasses(intersectsFilter)
                    .ToElements()
                    .Where(e => e.Category.BuiltInCategory
                        is BuiltInCategory.OST_PipeCurves
                        or BuiltInCategory.OST_MechanicalEquipment);
#else
                var selectedElements = new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .WherePasses(intersectsFilter)
                    .ToElements()
                    .Where(e => e.Category.Id.IntegerValue
                        is (int)BuiltInCategory.OST_PipeCurves
                        or (int)BuiltInCategory.OST_MechanicalEquipment);
#endif

                if (!selectedElements.Any() && overlayTags.Count <= 1) continue;

                var needMove = true;
                var moveLimit = 5;
                var roundLimit = 11;
                var counter = 0;
#if REVIT2022_OR_GREATER
                var taggedReferences = tag.GetTaggedReferences();
                if (taggedReferences.Count == 0) throw new NullReferenceException();
                var taggedElement = tag.Document.GetElement(taggedReferences.First());
#else
                var taggedReference = tag.GetTaggedReference();
                if (taggedReference == null) throw new NullReferenceException();
                var taggedElement = tag.Document.GetElement(taggedReference);
#endif

                XYZ roundCenter;
                if (taggedElement is Pipe)
                {
                    var boundingBox = taggedElement.get_BoundingBox(_document.ActiveView);
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

                var baseOffset = Consts.BaseOffset * _view.Scale;
                var offset = 0.0;

                while (needMove)
                {
                    if (counter > roundLimit && counter <= roundLimit * 2) offset += baseOffset;
                    if (counter > roundLimit * 2) offset += 2 * baseOffset;

                    var moveVector = tag.GetCircleMoveVector();

                    if (offset != 0)
                    {
                        Consts.Offset = offset;
                        //var direction = (moveVector - roundCenter).Normalize();
                        //moveVector += direction * offset;
                    }
                    else
                    {
                        Consts.Offset = 0.0;
                    }

                    using var tr = new Transaction(_document, "Rotate tag");
                    tr.Start();
                    tag!.Location.Move(moveVector);
                    RevitApp.ActiveUiDocument.RefreshActiveView();
                    tr.Commit();

                    outline = tag.GetOutline();
                    intersectsFilter = new BoundingBoxIntersectsFilter(outline);
#if REVIT2023_OR_GREATER
                    selectedElements = new FilteredElementCollector(_document, _document.ActiveView.Id)
                        .WherePasses(intersectsFilter)
                        .ToElements()
                        .Where(e => e.Category.BuiltInCategory is BuiltInCategory.OST_PipeCurves
                            or BuiltInCategory.OST_MechanicalEquipment);
#else
                    selectedElements = new FilteredElementCollector(_document, _document.ActiveView.Id)
                        .WherePasses(intersectsFilter)
                        .ToElements()
                        .Where(e => e.Category.Id.IntegerValue
                            is (int)BuiltInCategory.OST_PipeCurves
                            or (int)BuiltInCategory.OST_MechanicalEquipment);
#endif
                    //overlayTags = RevitShell.GetTagsInsideRectangle(outline);
                    //if (selectedElements.Count() == 0 && overlayTags.Count <= 1) needMove = false;
                    if (moveLimit-- == 0) needMove = false;
                    //counter++;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private void SolveSomeTag()
    {
        try
        {
            var tagIds = RevitApp.ActiveUiDocument.Selection.GetElementIds();

            foreach (var tagId in tagIds)
            {
                if (_document.GetElement(tagId) is not IndependentTag tag) continue;

                var outline = tag.GetSuperOutline();

                var overlayTags = RevitShell.Tags
                    .Where(w => w.CurrentTagOutline.Intersects(outline, 0.001))
                    .Select(s => s.TagId);


                foreach (var overlayTag in overlayTags)
                {
                    var independentTag = _document.GetElement(overlayTag) as IndependentTag;
                    TaskDialog.Show("OverlayTag", $"Tag: {independentTag?.TagText}");
                }

                var pipeFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);
                var equipmentFilter = new ElementCategoryFilter(BuiltInCategory.OST_MechanicalEquipment);

                var multiCategoryFilter = new LogicalOrFilter(new List<ElementFilter> { pipeFilter, equipmentFilter });

                var intersectsFilter = new BoundingBoxIntersectsFilter(outline);
                var overlayElements = new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .WherePasses(multiCategoryFilter)
                    .WherePasses(intersectsFilter)
                    .ToElements();

                foreach (var element in overlayElements)
                {
                    TaskDialog.Show("Выбранный элемент",
                        $"ID: {element.Id} - Name: {element.Name} {element.Category}");
                }

                var selectedMechanicalEquipment = overlayElements?.Cast<MechanicalEquipment>().ToList();
                var selectedPipe = overlayElements?.Cast<Pipe>().ToList();
            }

            // if (selectedElements.Any() || overlayTags.Count > 1)
            // {
            //     var moveVector = tag.GetCircleMoveVector();
            //
            //     _externalHandler.Raise(application =>
            //     {
            //         using var tr = new Transaction(document, "Rotate tag");
            //         tr.Start();
            //         tag!.Location.Move(moveVector);
            //         tr.Commit();
            //     });
            // }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private void CheckSuperTag()
    {
        var tagId = RevitApp.ActiveUiDocument.Selection.GetElementIds().First();

        var tag = _document.GetElement(tagId) as IndependentTag;

        var superTag = RevitShell.Tags.First(s => s.TagId == tag.Id);

        MessageBox.Show(
            $"Coord {superTag.TagId}: {superTag.CurrentTagOutline.MinimumPoint} - {superTag.CurrentTagOutline.MaximumPoint}");
    }

    [RelayCommand]
    private void SolveTags()
    {
        if (_view is not ViewPlan) return;

        var allSuperTag = new List<Tag>(RevitShell.Tags);

        var elements = new FilteredElementCollector(_document, _view.Id)
            .OfClass(typeof(IndependentTag))
            .ToElements();

        foreach (var element in elements)
        {
            if (element is not IndependentTag iTag) continue;

            var tags = new List<Tag>(RevitShell.Tags);
            var tag = tags.First(tag => tag.TagId == iTag.Id);
            tags.Remove(tag);

            if (IsFreePlace(tag, tag.CurrentTagHeadPosition)) continue;

            FindFreePlace(iTag, tag);
        }
    }

    [RelayCommand]
    private void SolveTag()
    {
        var tagId = RevitApp.ActiveUiDocument.Selection.GetElementIds().First();
        if (_document.GetElement(tagId) is not IndependentTag iTag) return;

        var tags = new List<Tag>(RevitShell.Tags);
        var tag = tags.First(tag => tag.TagId == iTag.Id);
        tags.Remove(tag);

        if (IsFreePlace(tag, tag.CurrentTagHeadPosition)) return;

        FindFreePlace(iTag, tag);
    }

    [RelayCommand]
    private void GetCoord()
    {
        var elementId = RevitApp.ActiveUiDocument.Selection.GetElementIds().First();
        var element = _document.GetElement(elementId);
        if (_view is not View3D view3D) return;
        var plane = GetViewPlane(view3D);
        var viewTransform = _view.CropBox.Transform;

        switch (element)
        {
            case IndependentTag iTag:
            {
                var tagViewPoint3D = iTag.TagHeadPosition;
                var tagViewPoint2D = viewTransform.OfPoint(tagViewPoint3D);
                var tagViewPoint2Di = viewTransform.Inverse.OfPoint(tagViewPoint3D);
                MessageBox.Show(
                    $"3D Coord: {tagViewPoint3D.X}, {tagViewPoint3D.Y}, {tagViewPoint3D.Z}" +
                    $"\n2D Coord: {tagViewPoint2D.X}, {tagViewPoint2D.Y}, {tagViewPoint2D.Z}" +
                    $"\n2DI Coord: {tagViewPoint2Di.X}, {tagViewPoint2Di.Y}, {tagViewPoint2Di.Z}");
                break;
            }
            case TextNote textNote:
            {
                var textViewPoint3D = textNote.Coord;
                var textViewPoint2D = viewTransform.OfPoint(textViewPoint3D);
                var textViewPoint2Di = viewTransform.Inverse.OfPoint(textViewPoint3D);
                MessageBox.Show(
                    $"3D Coord: {textViewPoint3D.X}, {textViewPoint3D.Y}, {textViewPoint3D.Z}" +
                    $"\n2D Coord: {textViewPoint2D.X}, {textViewPoint2D.Y}, {textViewPoint2D.Z}" +
                    $"\n2DI Coord: {textViewPoint2Di.X}, {textViewPoint2Di.Y}, {textViewPoint2Di.Z}");
                break;
            }
            default:
                return;
        }
    }

    private Plane GetViewPlane(View3D view3D)
    {
        var eyePosition = view3D.GetOrientation().EyePosition;
        var forwardDirection = view3D.GetOrientation().ForwardDirection;
        var viewPlane = Plane.CreateByNormalAndOrigin(forwardDirection, eyePosition);

        return viewPlane;
    }

    [RelayCommand]
    private void Move3DTag()
    {
        var tagId = RevitApp.ActiveUiDocument.Selection.GetElementIds().First();
        if (_document.GetElement(tagId) is not IndependentTag iTag) return;
        if (_view is not View3D view3D) return;
        var orientation = view3D.GetOrientation();

        var eyePosition = orientation.EyePosition;
        var forwardDirection = orientation.ForwardDirection;
        var upDirection = orientation.UpDirection;

        var moveUpDistance = 3.0;
        var moveLeftDistance = 3.0;

        var rightDirection = forwardDirection.CrossProduct(upDirection).Normalize();
        var upDirectionNormalized = upDirection.Normalize();

        var moveUp = upDirectionNormalized * moveUpDistance;
        var moveLeft = -rightDirection * moveLeftDistance;

        using var tr = new Transaction(_document, "Move Tags");

        tr.Start();
        var currentPosition = iTag.TagHeadPosition;
        var newPosition = currentPosition + moveUp + moveLeft;
        iTag!.Location.Move(iTag.TagHeadPosition - newPosition);
        tr.Commit();
    }

    private void FindFreePlace(IndependentTag iTag, Tag tag)
    {
        var startPoint = iTag.TagHeadPosition;
        var pZ = tag.CurrentTagHeadPosition.Z;
        var radius = 1;
        var stops = 4;

        for (var i = 0; i <= 10; i++)
        {
            for (var j = 0; j < stops; j++)
            {
                var angle = j * (2 * Math.PI) / stops;
                var pX = radius * Math.Cos(angle);
                var pY = radius * Math.Sin(angle);

                if (!IsFreePlace(tag, startPoint + new XYZ(pX, pY, 0))) continue;
                using var tr = new Transaction(_document, "Moving a tag");

                tr.Start();
                iTag!.Location.Move(new XYZ(pX, pY, pZ));
                RevitApp.ActiveUiDocument.RefreshActiveView();
                tr.Commit();
                return;
            }

            stops += 4;
            radius += 1;
        }
    }

    private bool IsFreePlace(Tag tag, XYZ point)
    {
        var tags = new List<Tag>(RevitShell.Tags);
        tags.Remove(tag);

        var outline = tag.GetOutlineByPoint(point);

        var overlayTags = tags
            .Where(w => w.CurrentTagOutline.Intersects(outline, 0.001))
            .Select(s => s.TagId);

        var intersectsFilter = new BoundingBoxIntersectsFilter(outline);
#if REVIT2023_OR_GREATER
        var selectedElements =
            new FilteredElementCollector(_document, _document.ActiveView.Id)
                .WherePasses(intersectsFilter)
                .ToElements()
                .Where(e => e.Category.BuiltInCategory
                    is BuiltInCategory.OST_PipeCurves
                    or BuiltInCategory.OST_MechanicalEquipment
                    or BuiltInCategory.OST_Walls);
#else
        var selectedElements =
            new FilteredElementCollector(_document, _document.ActiveView.Id)
                .WherePasses(intersectsFilter)
                .ToElements()
                .Where(e => e.Category.Id.IntegerValue
                    is (int)BuiltInCategory.OST_PipeCurves
                    or (int)BuiltInCategory.OST_MechanicalEquipment);
#endif

        var outlineSolid = CreateSolidFromOutline(outline);

        var linkedDocs = new FilteredElementCollector(_document)
            .OfCategory(BuiltInCategory.OST_RvtLinks)
            .ToElements()
            .OfType<RevitLinkInstance>()
            .Select(rli => rli.GetLinkDocument())
            .Where(linkedDoc => linkedDoc != null);

        List<Element> linkedElements = [];
        var visibilityFilter = new VisibleInViewFilter(_document, _document.ActiveView.Id);

        foreach (var linkedDoc in linkedDocs)
        {
#if REVIT2023_OR_GREATER
            var linkedElementsF = new FilteredElementCollector(linkedDoc)
                .WherePasses(new ElementIntersectsSolidFilter(outlineSolid))
                .Where(e => e.Category.BuiltInCategory is BuiltInCategory.OST_Walls);
#else
            var linkedElementsF = new FilteredElementCollector(linkedDoc)
                .WherePasses(new ElementIntersectsSolidFilter(outlineSolid))
                .Where(e => e.Category.Id.IntegerValue
                    is (int)BuiltInCategory.OST_Walls);
#endif
            linkedElements.AddRange(linkedElementsF);
        }

        return !selectedElements.Any() && !overlayTags.Any() && linkedElements.Count == 0;
    }

    private Solid CreateSolidFromOutline(Outline outline)
    {
        var viewPlan = _view as ViewPlan;
        var viewRange = viewPlan!.GetViewRange();

        var cutLevelElevation = ((Level)_document.GetElement(viewRange.GetLevelId(PlanViewPlane.CutPlane))).Elevation;

        var p0 = new XYZ(outline.MinimumPoint.X, outline.MinimumPoint.Y, cutLevelElevation);
        var p1 = new XYZ(outline.MaximumPoint.X, outline.MinimumPoint.Y, cutLevelElevation);
        var p2 = new XYZ(outline.MaximumPoint.X, outline.MaximumPoint.Y, cutLevelElevation);
        var p3 = new XYZ(outline.MinimumPoint.X, outline.MaximumPoint.Y, cutLevelElevation);

        var baseProfile = new CurveLoop();
        baseProfile.Append(Line.CreateBound(p0, p1));
        baseProfile.Append(Line.CreateBound(p1, p2));
        baseProfile.Append(Line.CreateBound(p2, p3));
        baseProfile.Append(Line.CreateBound(p3, p0));

        const double height = 0.01;

        var outlineSolid =
            GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { baseProfile }, XYZ.BasisZ, height);

        return outlineSolid;
    }
}