using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Desktop.Presentation;

namespace Pegasus.Desktop.ViewModels;

/// <summary>
/// Sample ViewModel using CommunityToolkit.Mvvm partial property syntax.
/// Uses <see cref="ObservableProperty"/> for change notification and
/// <see cref="RelayCommand"/> for command binding.
/// </summary>
public partial class MainPageViewModel : ObservableObject
{
    public ProblemPresentation ValidationProblem { get; } = CreateProblem(
        PegasusProblemTypes.Validation,
        "reference-validation");

    public ProblemPresentation UnavailableProblem { get; } = CreateProblem(
        PegasusProblemTypes.ProviderUnavailable,
        "reference-unavailable");

    public ProblemPresentation InformationalProblem { get; } = CreateProblem(
        PegasusProblemTypes.NotFound,
        "reference-information");

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Hello, WinUI!";

    [ObservableProperty]
    public partial int Counter { get; set; }

    [RelayCommand]
    private void Increment()
    {
        Counter++;
    }

    [RelayCommand]
    private void Decrement()
    {
        Counter--;
    }

    private static ProblemPresentation CreateProblem(string type, string reference) =>
        ProblemPresentation.FromProblem(
            new PegasusProblem(type, string.Empty, 400, null, null, reference));
}
