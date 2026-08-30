using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Windows.AppLifecycle;
using Pegasus.Desktop.Hosting;
using Pegasus.Desktop.Options;
using Pegasus.Desktop.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pegasus.Desktop;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static int _servicesDisposed;
    private static readonly ConcurrentQueue<AppActivationArguments> PendingActivations = [];
    private static Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

    internal static string? StartupSessionId { get; private set; }

    public static IHost Services { get; private set; } = null!;

    /// <summary>
    /// The main application window. Use <c>App.Window</c> from any class that needs
    /// the window reference (for dialogs, pickers, interop, etc.).
    /// </summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>
    /// The UI thread dispatcher. Use <c>App.DispatcherQueue</c> to marshal calls
    /// to the UI thread. Fully qualified to avoid CS0104 ambiguity with
    /// <see cref="Windows.System.DispatcherQueue"/>.
    /// </summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    /// <summary>
    /// The native window handle (HWND). Use for file pickers,
    /// <c>DataTransferManager</c>, and any WinRT interop that requires
    /// <c>InitializeWithWindow</c>.
    /// </summary>
    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeServices();
    }

    internal static void ConfigureStartup(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        StartupSessionId = sessionId;
    }

    internal static void RegisterActivationSource(AppInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        instance.Activated += OnInstanceActivated;
    }

    /// <summary>
    /// Disposes the host before requesting the WinUI application exit boundary.
    /// </summary>
    public static void ExitApplication()
    {
        DisposeServices();
        Application.Current.Exit();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Services = PegasusHost.Build(sessionId: StartupSessionId);
        Services.Start();
        var logger = Services.Services.GetRequiredService<ILogger<App>>();
        if (logger.IsEnabled(LogLevel.Information))
        {
            var channel = Services.Services.GetRequiredService<IOptions<ChannelOptions>>().Value.Channel;
            LogHostStarted(logger, channel);
        }

        Window = new MainWindow();
        Window.Closed += (_, _) => DisposeServices();
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        DispatcherQueue = _dispatcherQueue;
        Window.Activate();

        HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs());
        DrainPendingActivations();
    }

    private static void OnInstanceActivated(object? sender, AppActivationArguments args)
    {
        var dispatcherQueue = _dispatcherQueue;
        if (dispatcherQueue is null)
        {
            PendingActivations.Enqueue(args);
            return;
        }

        if (dispatcherQueue.HasThreadAccess)
        {
            HandleActivation(args);
            return;
        }

        if (!dispatcherQueue.TryEnqueue(() => HandleActivation(args)))
        {
            PendingActivations.Enqueue(args);
        }
    }

    private static void DrainPendingActivations()
    {
        while (PendingActivations.TryDequeue(out var activationArguments))
        {
            HandleActivation(activationArguments);
        }
    }

    private static void HandleActivation(AppActivationArguments activationArguments)
    {
        var router = Services?.Services.GetService<IActivationRouter>();
        router?.Route(activationArguments);
        BringWindowForward();
    }

    private static void BringWindowForward()
    {
        if (Window is null)
        {
            return;
        }

        if (Window.AppWindow.Presenter is OverlappedPresenter presenter &&
            presenter.State == OverlappedPresenterState.Minimized)
        {
            presenter.Restore(true);
            return;
        }

        Window.AppWindow.Show(true);
    }

    private static void DisposeServices()
    {
        if (Interlocked.Exchange(ref _servicesDisposed, 1) == 0)
        {
            Services?.Dispose();
        }
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Desktop host started for channel {Channel}")]
    private static partial void LogHostStarted(ILogger logger, string? channel);
}
