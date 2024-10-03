using System.Windows;
using Autodesk.Revit.Attributes;
using B.BeaverTools.Utils.Extensions;
using Nice3point.Revit.Toolkit.External;

namespace B.BeaverTools.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ClashCheckerCommand : ExternalCommand
{
    public override void Execute()
    {
        if (Document.ActiveView is not View3D view3D)
        {
            MessageBox.Show("Only for 3D views can be executed");
            return;
        }

        var id = UiApplication.ActiveUIDocument.Selection.GetElementIds().First();
        if (Document.GetElement(id) is not IndependentTag tag) return;

        var solid = tag.GetExtrusionSolid(view3D);

        var solidFilter = new ElementIntersectsSolidFilter(solid);

        var elementIds = new FilteredElementCollector(Document, ActiveView.Id)
            .WherePasses(solidFilter)
            .ToElementIds();

        MessageBox.Show(string.Join("\n", elementIds));
    }
}