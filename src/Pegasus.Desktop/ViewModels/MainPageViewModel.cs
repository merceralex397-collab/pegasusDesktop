using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pegasus.Desktop.Presentation;

namespace Pegasus.Desktop.ViewModels;

/// <summary>
/// Sample ViewModel using CommunityToolkit.Mvvm partial property syntax.
/// Uses <see cref="ObservableProperty"/> for change notification and
/// <see cref="RelayCommand"/> for command binding.
/// </summary>
public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Hello, WinUI!";

    private int counter;

    public string Counter => OperatorText.Count(counter);

    [RelayCommand]
    private void Increment()
    {
        counter++;
        OnPropertyChanged(nameof(Counter));
    }

    [RelayCommand]
    private void Decrement()
    {
        counter--;
        OnPropertyChanged(nameof(Counter));
    }
}
