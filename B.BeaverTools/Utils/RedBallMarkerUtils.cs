using Autodesk.Revit.DB.Structure;
using B.BeaverTools.Core;

namespace B.BeaverTools.Utils;

public static class RedBallMarkerUtils
{
    /// <summary>
    /// This method needs a transaction
    /// </summary>
    public static void DrawBallOnBoundingBox(Element element)
    {
        var document = RevitApp.Document;
        var view = RevitApp.ActiveView;
        
        var boundingBox = element.get_BoundingBox(view);
    
        var p1 = boundingBox.Min;
        var p2 = new XYZ(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z);
        var p3 = new XYZ(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z);
        var p4 = new XYZ(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z);
    
        var p5 = new XYZ(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z);
        var p6 = new XYZ(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z);
        var p7 = boundingBox.Max;
        var p8 = new XYZ(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z);
    
    
        PlaceBallFamilyAtXyz(p1);
        PlaceBallFamilyAtXyz(p2);
        PlaceBallFamilyAtXyz(p3);
        PlaceBallFamilyAtXyz(p4);
        PlaceBallFamilyAtXyz(p5);
        PlaceBallFamilyAtXyz(p6);
        PlaceBallFamilyAtXyz(p7);
        PlaceBallFamilyAtXyz(p8);
    }

    /// <summary>
    /// This method needs a transaction
    /// </summary>
    public static void PlaceBallFamilyAtXyz(XYZ location)
    {
        var document = RevitApp.Document;
        var ballElement = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_GenericModel)
            .First(f => f.Name.Equals("ball"));
    
        var ballFamilySymbol = ballElement as FamilySymbol;
        
        if (!ballFamilySymbol!.IsActive)
        {
            ballFamilySymbol.Activate();
            document.Regenerate();
        }
    
        var ballInstance = document.Create.NewFamilyInstance(
            location,
            ballFamilySymbol,
            StructuralType.NonStructural);
    }
}