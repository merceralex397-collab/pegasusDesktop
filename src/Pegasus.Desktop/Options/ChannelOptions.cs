using System.ComponentModel.DataAnnotations;

namespace Pegasus.Desktop.Options;

public sealed class ChannelOptions
{
    public const string ConfigurationKey = "Channel";

    [Required]
    [RegularExpression("^(local|pilot|production)$")]
    public string? Channel { get; set; }
}
