using Microsoft.Windows.AppLifecycle;

namespace Pegasus.Desktop.Services;

public interface IActivationRouter
{
    void Route(AppActivationArguments activationArguments);
}

// FND-033 owns the concrete shell navigation implementation. Keeping this port
// in the activation service file lets this ticket compile against the planned
// contract until that sibling service lands.
public interface INavigationService
{
    void Navigate(string route);
}
