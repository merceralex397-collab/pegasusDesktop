namespace Pegasus.Contracts;

/// <summary>
/// Conventions for the shared gateway and desktop wire contracts.
/// DTOs use <c>Request</c> and <c>Response</c> suffixes, never expose Core
/// records directly, serialize enum values as strings, and represent dates as
/// UTC <see cref="DateTimeOffset"/> values.
/// This type is also the stable assembly marker used by the dependency
/// architecture fact.
/// </summary>
public static class ContractConventions
{
}
