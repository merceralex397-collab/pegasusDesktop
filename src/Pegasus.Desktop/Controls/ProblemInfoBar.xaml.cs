using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pegasus.Desktop.Presentation;
using Windows.ApplicationModel.DataTransfer;

namespace Pegasus.Desktop.Controls;

public sealed partial class ProblemInfoBar : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ProblemProperty =
        DependencyProperty.Register(
            nameof(Problem),
            typeof(ProblemPresentation),
            typeof(ProblemInfoBar),
            new PropertyMetadata(null, OnPresentationChanged));

    public static readonly DependencyProperty AutomationIdPrefixProperty =
        DependencyProperty.Register(
            nameof(AutomationIdPrefix),
            typeof(string),
            typeof(ProblemInfoBar),
            new PropertyMetadata("ProblemInfoBar", OnPresentationChanged));

    public ProblemInfoBar()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProblemPresentation? Problem
    {
        get => (ProblemPresentation?)GetValue(ProblemProperty);
        set => SetValue(ProblemProperty, value);
    }

    public string AutomationIdPrefix
    {
        get => (string)GetValue(AutomationIdPrefixProperty);
        set => SetValue(AutomationIdPrefixProperty, value);
    }

    public bool HasProblem => Problem is not null;

    public InfoBarSeverity Severity => Problem?.Severity switch
    {
        ProblemSeverity.Warning => InfoBarSeverity.Warning,
        ProblemSeverity.Error => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational
    };

    public string Message => Problem?.Sentence ?? string.Empty;

    public string Reference => Problem?.Reference ?? string.Empty;

    public Visibility ReferenceVisibility =>
        string.IsNullOrWhiteSpace(Problem?.Reference)
            ? Visibility.Collapsed
            : Visibility.Visible;

    public static string ReferenceLabel => ProblemPresentation.ReferenceLabel;

    public static string CopyReferenceLabel => ProblemPresentation.CopyReferenceLabel;

    public static string CopyButtonLabel => ProblemPresentation.CopyButtonLabel;

    public string ProblemAutomationId => $"{AutomationIdPrefix}.Problem";

    public string ReferenceAutomationId => $"{AutomationIdPrefix}.Reference";

    public string ReferenceValueAutomationId => $"{AutomationIdPrefix}.ReferenceValue";

    public string CopyReferenceAutomationId => $"{AutomationIdPrefix}.CopyReference";

    private static void OnPresentationChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        var control = (ProblemInfoBar)dependencyObject;
        control.OnPropertyChanged(nameof(HasProblem));
        control.OnPropertyChanged(nameof(Severity));
        control.OnPropertyChanged(nameof(Message));
        control.OnPropertyChanged(nameof(Reference));
        control.OnPropertyChanged(nameof(ReferenceVisibility));
        control.OnPropertyChanged(nameof(ProblemAutomationId));
        control.OnPropertyChanged(nameof(ReferenceAutomationId));
        control.OnPropertyChanged(nameof(ReferenceValueAutomationId));
        control.OnPropertyChanged(nameof(CopyReferenceAutomationId));
    }

    private void CopyReference_Click(object sender, RoutedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(Problem?.Reference))
        {
            return;
        }

        var reference = Problem!.Reference!;
        var package = new DataPackage();
        package.SetText(reference);
        Clipboard.SetContent(package);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
