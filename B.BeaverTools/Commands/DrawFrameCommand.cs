using System.Windows;
using Autodesk.Revit.Attributes;
using B.BeaverTools.Core;
using B.BeaverTools.Utils.Extensions;
using Nice3point.Revit.Toolkit.External;

namespace B.BeaverTools.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class DrawFrameCommand : ExternalCommand
{
    public override void Execute()
    {
        var document = RevitApp.Document;
        var viewPlan = document.ActiveView;
        if (viewPlan is not ViewPlan) return;
        var tagIds = RevitApp.ActiveUiDocument.Selection.GetElementIds();

        foreach (var tagId in tagIds)
        {
            if (tagId == null || document?.GetElement(tagId) is not IndependentTag tag)
            {
                MessageBox.Show("Please select a tag");
                return;
            }
            
            var outline = tag.GetSuperOutline();
            var minPoint = outline.MinimumPoint;
            var maxPoint = outline.MaximumPoint;
            var zCoord = (minPoint.Z + maxPoint.Z) / 2;

            var plane = Plane.CreateByNormalAndOrigin(viewPlan.ViewDirection, viewPlan.Origin);

            var curves = new CurveArray();
            curves.Append(Line.CreateBound(
                new XYZ(minPoint.X, minPoint.Y, zCoord),
                new XYZ(minPoint.X, maxPoint.Y, zCoord)));
            curves.Append(Line.CreateBound(
                new XYZ(minPoint.X, maxPoint.Y, zCoord),
                new XYZ(maxPoint.X, maxPoint.Y, zCoord)));
            curves.Append(Line.CreateBound(
                new XYZ(maxPoint.X, maxPoint.Y, zCoord),
                new XYZ(maxPoint.X, minPoint.Y, zCoord)));
            curves.Append(Line.CreateBound(
                new XYZ(maxPoint.X, minPoint.Y, zCoord),
                new XYZ(minPoint.X, minPoint.Y, zCoord)));
            using var tr = new Transaction(document, "Draw Frame");
            tr.Start();
            var detailCurve = document.Create.NewDetailCurveArray(viewPlan, curves);
            tr.Commit();
            RevitApp.ActiveUiDocument.RefreshActiveView();
        }
    }
}