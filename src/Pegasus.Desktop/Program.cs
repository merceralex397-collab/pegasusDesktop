using System.Runtime.ExceptionServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Pegasus.Desktop.Hosting;
using Pegasus.Desktop.Services;

namespace Pegasus.Desktop;

internal static class Program
{
    internal const string InstanceKey = "pegasus-desktop-single-instance";

    internal static string SessionId { get; } = Guid.NewGuid().ToString("N");

    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var activatedArguments = AppInstance.GetCurrent().GetActivatedEventArgs();
        var instance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (!instance.IsCurrent)
        {
            ActivationLog.WriteRedirect(
                PegasusHost.CreateDiagnosticsWriter(),
                SessionId,
                activatedArguments);
            RedirectWithoutBlockingSta(instance, activatedArguments);
            return;
        }

        App.ConfigureStartup(SessionId);
        App.RegisterActivationSource(instance);

        Application.Start(applicationInitializationCallbackParams =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    private static void RedirectWithoutBlockingSta(
        AppInstance instance,
        AppActivationArguments activatedArguments)
    {
        using var redirectCompleted = new ManualResetEventSlim(false);
        Exception? redirectException = null;

        _ = Task.Run(async () =>
        {
            try
            {
                await instance.RedirectActivationToAsync(activatedArguments);
            }
            catch (Exception exception)
            {
                redirectException = exception;
            }
            finally
            {
                redirectCompleted.Set();
            }
        });

        redirectCompleted.Wait();

        if (redirectException is not null)
        {
            ExceptionDispatchInfo.Capture(redirectException).Throw();
        }
    }
}
