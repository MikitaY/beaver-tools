using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using B.BeaverTools.Views;

namespace B.BeaverTools.Commands;

/// <summary>
///     External command entry point invoked from the Revit interface
/// </summary>
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        var view = Host.GetService<B.BeaverToolsView>();
        view.Show(UiApplication.MainWindowHandle);
    }
}