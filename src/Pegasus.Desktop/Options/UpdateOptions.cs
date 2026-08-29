using System.ComponentModel.DataAnnotations;

namespace Pegasus.Desktop.Options;

public sealed class UpdateOptions
{
    public const string ConfigurationSectionName = "Update";

    [Required]
    public Uri? FeedUri { get; set; }
}
