using Autodesk.Revit.UI;

namespace B.BeaverTools.Core;

public static class RevitApp
{
    public static UIApplication UiApplication { get; set; }
    public static UIDocument ActiveUiDocument => UiApplication.ActiveUIDocument;
    public static Document Document => UiApplication.ActiveUIDocument.Document;

    public static View ActiveView => UiApplication.ActiveUIDocument.ActiveView;
}