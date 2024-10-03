using B.BeaverTools.Core;

namespace B.BeaverTools.Utils;

public static class DrawingAnnotationsUtils
{
    /// <summary>
    /// This method needs a transaction
    /// </summary>
    /// <param name="point">Start path point</param>
    private static void DrawRoundPath(XYZ point)
    {
        var radius = 1;
        var stops = 4;

        for (var i = 0; i <= 10; i++)
        {
            for (var j = 0; j < stops; j++)
            {
                var angle = j * (2 * Math.PI) / stops;
                var pX = radius * Math.Cos(angle);
                var pY = radius * Math.Sin(angle);

                DrawPoint(new XYZ(pX, pY, point.Z));
            }

            stops += 4;
            radius += 1;
        }
    }

    private static void DrawPoint(XYZ point)
    {
        var document = RevitApp.Document;
        var view = RevitApp.ActiveView;
        const double radius = 0.1;

        using var tr = new Transaction(document, "Create Circle Annotation");

        tr.Start();
        Curve circle = Arc.Create(point, radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY);
        var detailLine = document.Create.NewDetailCurve(view, circle) as DetailLine;
        RevitApp.UiApplication.ActiveUIDocument.RefreshActiveView();
        tr.Commit();
    }
}