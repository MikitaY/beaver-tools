using B.BeaverTools.ViewModels.Contracts;

namespace B.BeaverTools.Views;

public sealed partial class ToolsView
{
    public ToolsView(IToolsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}