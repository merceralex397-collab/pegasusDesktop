namespace Pegasus.Contracts.ProblemDetails;

public static class PegasusProblemTypes
{
    public const string Prefix = "urn:pegasus:problem:";
    public const string Validation = Prefix + "validation";
    public const string NotAuthorized = Prefix + "not-authorized";
    public const string VersionConflict = Prefix + "version-conflict";
    public const string LeaseConflict = Prefix + "lease-conflict";
    public const string LeaseExpired = Prefix + "lease-expired";
    public const string OperationConflict = Prefix + "operation-conflict";
    public const string ClientUnsupported = Prefix + "client-unsupported";
    public const string PasswordChangeRequired = Prefix + "password-change-required";
    public const string AccountDisabled = Prefix + "account-disabled";
    public const string ProviderUnavailable = Prefix + "provider-unavailable";
    public const string NotFound = Prefix + "not-found";
    public const string VehicleSuggestionUnavailable = Prefix + "vehicle-suggestion-unavailable";
    public const string VehicleRegistrationRequired = Prefix + "vehicle-registration-required";
    public const string VehicleRegistrationConflict = Prefix + "vehicle-registration-conflict";
    public const string VehicleFieldConflict = Prefix + "vehicle-field-conflict";
    public const string RateLimited = Prefix + "rate-limited";
    public const string Maintenance = Prefix + "maintenance";
}
