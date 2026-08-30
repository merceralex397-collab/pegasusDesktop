using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Pegasus.Desktop.Infrastructure.Diagnostics;
using Windows.ApplicationModel.Activation;
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs;

namespace Pegasus.Desktop.Services;

public sealed partial class ActivationRouter : IActivationRouter
{
    private const string ProtocolScheme = "pegasus";
    private readonly INavigationService _navigationService;
    private readonly ILogger<ActivationRouter> _logger;

    public ActivationRouter(
        INavigationService navigationService,
        ILogger<ActivationRouter> logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Route(AppActivationArguments activationArguments)
    {
        ArgumentNullException.ThrowIfNull(activationArguments);
        LogActivationReceived(activationArguments);

        if (TryGetRoute(activationArguments, out var route, out var target))
        {
            Navigate(route, target);
            return;
        }

        LogIgnored(ActivationLog.GetArgumentHash(activationArguments));
    }

    public void Route(string? activationArgument)
    {
        if (TryGetRoute(activationArgument, out var route, out var target))
        {
            Navigate(route, target);
            return;
        }

        LogIgnored(ActivationLog.GetArgumentHash(activationArgument));
    }

    public void RouteFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            LogIgnored(ActivationLog.GetArgumentHash(filePath));
            return;
        }

        Navigate($"/Documents/{filePath}", "document");
    }

    private static bool TryGetRoute(
        AppActivationArguments activationArguments,
        out string route,
        out string target)
    {
        route = string.Empty;
        target = string.Empty;

        return activationArguments.Kind switch
        {
            ExtendedActivationKind.Protocol =>
                activationArguments.Data is IProtocolActivatedEventArgs protocol &&
                TryGetRoute(protocol.Uri, out route, out target),
            ExtendedActivationKind.File =>
                activationArguments.Data is IFileActivatedEventArgs file &&
                file.Files.Count > 0 &&
                TryGetFileRoute(file.Files[0].Path, out route, out target),
            ExtendedActivationKind.Launch =>
                activationArguments.Data is LaunchActivatedEventArgs launch &&
                TryGetRoute(launch.Arguments, out route, out target),
            _ => false
        };
    }

    private static bool TryGetRoute(
        string? activationArgument,
        out string route,
        out string target)
    {
        route = string.Empty;
        target = string.Empty;

        if (string.IsNullOrWhiteSpace(activationArgument) ||
            !Uri.TryCreate(activationArgument, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return TryGetRoute(uri, out route, out target);
    }

    private static bool TryGetRoute(
        Uri uri,
        out string route,
        out string target)
    {
        route = string.Empty;
        target = string.Empty;

        if (!uri.Scheme.Equals(ProtocolScheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = new List<string>();
        if (!string.IsNullOrWhiteSpace(uri.Host))
        {
            segments.Add(uri.Host);
        }

        segments.AddRange(uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString));

        if (segments.Count != 2 || string.IsNullOrWhiteSpace(segments[1]))
        {
            return false;
        }

        var identifier = Uri.UnescapeDataString(segments[1]);
        switch (segments[0].ToLowerInvariant())
        {
            case "case":
            case "cases":
                route = $"/Cases/{identifier}";
                target = "case";
                return true;
            case "document":
            case "documents":
                route = $"/Documents/{identifier}";
                target = "document";
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetFileRoute(
        string? filePath,
        out string route,
        out string target)
    {
        route = string.Empty;
        target = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        route = $"/Documents/{filePath}";
        target = "document";
        return true;
    }

    private void Navigate(string route, string target)
    {
        _navigationService.Navigate(route);
        LogActivationRouted(_logger, target);
    }

    private void LogActivationReceived(AppActivationArguments activationArguments)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            var argumentHash = ActivationLog.GetArgumentHash(activationArguments);
            LogActivationReceived(
                _logger,
                activationArguments.Kind,
                argumentHash);
        }
    }

    private void LogIgnored(string argumentHash)
    {
        LogActivationIgnored(_logger, argumentHash);
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Activation received with kind {ActivationKind} and argument hash {ArgumentHash}")]
    private static partial void LogActivationReceived(
        ILogger logger,
        ExtendedActivationKind activationKind,
        string argumentHash);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Activation routed to {ActivationTarget}")]
    private static partial void LogActivationRouted(ILogger logger, string activationTarget);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Activation ignored with argument hash {ArgumentHash}")]
    private static partial void LogActivationIgnored(ILogger logger, string argumentHash);
}

internal static class ActivationLog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    internal static string GetArgumentHash(AppActivationArguments activationArguments)
    {
        ArgumentNullException.ThrowIfNull(activationArguments);
        return GetArgumentHash(GetRawArgument(activationArguments));
    }

    internal static string GetArgumentHash(string? activationArgument) =>
        ComputeArgumentHash(activationArgument ?? string.Empty);

    internal static void WriteRedirect(
        IDiagnosticsWriter writer,
        string sessionId,
        AppActivationArguments activationArguments)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(activationArguments);

        var entry = new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Information.ToString(),
            Category = "Pegasus.Desktop.Activation",
            EventId = 2003,
            EventName = "ActivationRedirected",
            SessionId = sessionId,
            CorrelationId = (string?)null,
            Message = "Activation redirected to existing instance",
            Exception = (string?)null,
            Properties = new
            {
                ActivationKind = activationArguments.Kind.ToString(),
                ArgumentHash = GetArgumentHash(activationArguments)
            }
        };

        writer.Write(JsonSerializer.Serialize(entry, SerializerOptions));
    }

    private static string ComputeArgumentHash(string argument)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(argument));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetRawArgument(AppActivationArguments activationArguments) =>
        activationArguments.Data switch
        {
            IProtocolActivatedEventArgs protocol => protocol.Uri.AbsoluteUri,
            IFileActivatedEventArgs file => string.Join(
                "|",
                file.Files.Select(static item => item.Path)),
            LaunchActivatedEventArgs launch => launch.Arguments,
            _ => activationArguments.Kind.ToString()
        };
}
