using Nice3point.Revit.Toolkit.External;
using B.BeaverTools.Commands;

namespace B.BeaverTools;

/// <summary>
///     Application entry point
/// </summary>
[UsedImplicitly]
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        Host.Start();
        CreateRibbon();
    }

    public override void OnShutdown()
    {
        Host.Stop();
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("BeaverTools", "Add-Ins");

        panel.AddPushButton<StartupCommand>("Execute")
            .SetImage("/B.BeaverTools;component/Resources/Icons/icon16.png")
            .SetLargeImage("/B.BeaverTools;component/Resources/Icons/icon32.png");
    }
}