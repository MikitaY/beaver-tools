using System.Windows;
using B.BeaverTools.ViewModels.Contracts;
using Microsoft.Extensions.Logging;

namespace B.BeaverTools.ViewModels;

public sealed partial class ToolsViewModel(ILogger<ToolsViewModel> logger) : ObservableObject, IToolsViewModel
{
    [RelayCommand]
    private void Start()
    {
        MessageBox.Show("It's alive!");
    }
}