namespace B.BeaverTools.ViewModels.Contracts;

public interface IToolsViewModel
{
    IRelayCommand TagAllCommand { get; }
    IRelayCommand PlaceCommand { get; }
    IRelayCommand SolveAllTagCommand { get; }
    IRelayCommand SolveSomeTagCommand { get; }
    IRelayCommand CheckSuperTagCommand { get; }
    IRelayCommand SolveTagCommand { get; }
    IRelayCommand SolveTagsCommand { get; }
    IRelayCommand Move3DTagCommand { get; }
    IRelayCommand GetCoordCommand { get; }
}