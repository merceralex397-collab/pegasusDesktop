using Windows.ApplicationModel;

namespace Pegasus.Desktop.Infrastructure.Windows;

public sealed class PackageClientVersionProvider : Api.IClientVersionProvider
{
    public string GetVersion()
    {
        var version = Package.Current.Id.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
