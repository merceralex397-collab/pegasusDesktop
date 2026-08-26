namespace Pegasus.Contracts.Responses;

public sealed record ClientCompatibilityResponse(
    string MinimumVersion,
    string CurrentVersion,
    string Channel,
    string? MaintenanceMessage,
    int ValidForSeconds);
