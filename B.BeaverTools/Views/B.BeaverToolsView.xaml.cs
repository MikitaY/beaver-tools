using B.BeaverTools.ViewModels;

namespace B.BeaverTools.Views;

public sealed partial class B.BeaverToolsView
{
public B.BeaverToolsView(B.BeaverToolsViewModel viewModel)
{
    DataContext = viewModel;
    InitializeComponent();
}
}