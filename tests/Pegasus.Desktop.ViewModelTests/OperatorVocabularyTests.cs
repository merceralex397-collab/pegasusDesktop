using System.Reflection;
using System.Text.RegularExpressions;
using Pegasus.Core.Cases;
using Pegasus.Core.Documents;
using Pegasus.Core.ImageIntake;
using Pegasus.Core.Intake;
using Pegasus.Core.Intake.Unidentified;
using Pegasus.Core.Identity;
using Pegasus.Core.Tasks;
using Pegasus.Core.Vehicle;
using Pegasus.Core.Workflow;
using Pegasus.Desktop.Presentation;
using Pegasus.Desktop.ViewModels;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class OperatorVocabularyTests
{
    private static readonly IReadOnlyDictionary<string, string> CaseStageLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(CaseLifecycleState.NotReady)] = "Not ready",
            [nameof(CaseLifecycleState.Held)] = "Held",
            [nameof(CaseLifecycleState.Review)] = "Review",
            [nameof(CaseLifecycleState.ReportPreparation)] = "Report preparation",
            [nameof(CaseLifecycleState.PostReport)] = "Post report",
            [nameof(CaseLifecycleState.PostReportComplete)] = "Post-report complete",
            [nameof(CaseLifecycleState.ProviderCancelled)] = "Provider cancelled",
            [nameof(CaseLifecycleState.CollisionEngineersRejected)] = "Collision Engineers rejected",
            [nameof(CaseLifecycleState.CreatedInError)] = "Created in error",
            [nameof(CaseLifecycleState.SourceEmailUnlinked)] = "Cancelled — email unlinked"
        };

    private static readonly IReadOnlyDictionary<string, string> DocumentRoleLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(DocumentSemanticRole.OriginalSource)] = "Original source",
            [nameof(DocumentSemanticRole.Instruction)] = "Instruction",
            [nameof(DocumentSemanticRole.Image)] = "Image",
            [nameof(DocumentSemanticRole.Correspondence)] = "Correspondence",
            [nameof(DocumentSemanticRole.EngineerReport)] = "Engineer report",
            [nameof(DocumentSemanticRole.AuditReport)] = "Audit report",
            [nameof(DocumentSemanticRole.FeeNote)] = "Fee note",
            [nameof(DocumentSemanticRole.Other)] = "Other"
        };

    private static readonly IReadOnlyDictionary<string, string> DocumentOriginLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(DocumentSource.Intake)] = "E-mail",
            [nameof(DocumentSource.StaffUpload)] = "Staff upload",
            [nameof(DocumentSource.RequestUpload)] = "Upload link",
            [nameof(DocumentSource.ExternalCorrespondence)] = "Correspondence",
            [nameof(DocumentSource.Generated)] = "Generated",
            [nameof(DocumentSource.Automation)] = "Automatic"
        };

    private static readonly IReadOnlyDictionary<string, string> IntakeDecisionLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(IntakeDecision.CaseCreated)] = "Ready for case allocation",
            [nameof(IntakeDecision.NeedsSorting)] = "Unidentified",
            [nameof(IntakeDecision.BlockedIntake)] = "Blocked",
            [nameof(IntakeDecision.Unsupported)] = "Unsupported",
            [nameof(IntakeDecision.OcrRequired)] = "Needs text extraction",
            [nameof(IntakeDecision.TechnicalFailure)] = "Failed",
            [nameof(IntakeDecision.ImageIntakeRegistered)] = "Vehicle images registered"
        };

    private static readonly IReadOnlyDictionary<string, string> VrmOutcomeLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(VrmRecognitionOutcomeKind.Suggested)] = "Suggested EJ17 NBZ (90% confidence)",
            [nameof(VrmRecognitionOutcomeKind.NoReadableResult)] = "No readable registration",
            [nameof(VrmRecognitionOutcomeKind.TechnicalFailure)] = "Technical failure",
            [nameof(VrmRecognitionOutcomeKind.Unavailable)] = "Recognition unavailable"
        };

    [Fact]
    [Trait("Category", "ViewModel")]
    public void SharedMapOwnsTheOperatorWords()
    {
        Assert.Equal("Review", OperatorText.CaseStage(nameof(CaseLifecycleState.Review)));
        Assert.Equal("Inspection and audit", OperatorText.CaseTypeName(nameof(CaseType.InspectionAndAudit)));
        Assert.Equal("E-mail", OperatorText.DocumentOrigin(nameof(DocumentSource.Intake)));
        Assert.Equal("Stored", OperatorText.CustodyState(nameof(DocumentCustodyStatus.Confirmed)));
        Assert.Equal("Vehicle images registered", OperatorText.IntakeDecisionLabel(nameof(IntakeDecision.ImageIntakeRegistered)));
        Assert.Equal("Case created", OperatorText.HistoryEvent("case_accepted"));
        Assert.Equal("km", OperatorText.MileageUnit(nameof(VehicleMileageUnit.Kilometres)));
    }

    [Theory]
    [MemberData(nameof(CaseStages))]
    [Trait("Category", "ViewModel")]
    public void EveryCaseStageHasAnExplicitLabel(string value, string expected)
    {
        Assert.Equal(expected, OperatorText.CaseStage(value));
    }

    [Theory]
    [MemberData(nameof(DocumentRoles))]
    [Trait("Category", "ViewModel")]
    public void EveryDocumentRoleHasAnExplicitLabel(string value, string expected)
    {
        Assert.Equal(expected, OperatorText.DocumentRole(value));
    }

    [Theory]
    [MemberData(nameof(DocumentOrigins))]
    [Trait("Category", "ViewModel")]
    public void EveryDocumentOriginHasAnExplicitLabel(string value, string expected)
    {
        Assert.Equal(expected, OperatorText.DocumentOrigin(value));
    }

    [Theory]
    [MemberData(nameof(IntakeDecisions))]
    [Trait("Category", "ViewModel")]
    public void EveryIntakeDecisionHasAnExplicitLabel(string value, string expected)
    {
        Assert.Equal(expected, OperatorText.IntakeDecisionLabel(value));
    }

    [Theory]
    [MemberData(nameof(VrmOutcomes))]
    [Trait("Category", "ViewModel")]
    public void EveryVrmOutcomeHasAnExplicitLabel(string value, string expected)
    {
        Assert.Equal(expected, OperatorText.VrmRecognitionOutcomeLabel(value, "EJ17 NBZ", "90%"));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void AddingAValueWithoutARegisteredLabelChangesTheExpectedEnumSet()
    {
        Assert.Equal(Enum.GetNames<CaseLifecycleState>().Order(), CaseStageLabels.Keys.Order());
        Assert.Equal(Enum.GetNames<DocumentSemanticRole>().Order(), DocumentRoleLabels.Keys.Order());
        Assert.Equal(Enum.GetNames<DocumentSource>().Order(), DocumentOriginLabels.Keys.Order());
        Assert.Equal(Enum.GetNames<IntakeDecision>().Order(), IntakeDecisionLabels.Keys.Order());
        Assert.Equal(Enum.GetNames<VrmRecognitionOutcomeKind>().Order(), VrmOutcomeLabels.Keys.Order());
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void EuropeLondonFormattingIsUsed()
    {
        var summer = new DateTimeOffset(2026, 6, 15, 13, 0, 0, TimeSpan.Zero);
        var winter = new DateTimeOffset(2026, 1, 15, 13, 0, 0, TimeSpan.Zero);

        Assert.Equal("15 Jun 2026 14:00", OperatorText.OfficeTime(summer));
        Assert.Equal("14:00", OperatorText.OfficeClock(summer));
        Assert.Equal("15 Jan 2026 13:00", OperatorText.OfficeTime(winter));
        Assert.Equal("London", OperatorText.OfficeTimeZoneLabel);
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NumericDisplayValuesArePreformattedBeforeXaml()
    {
        var viewModel = new MainPageViewModel();

        Assert.Equal(typeof(string), typeof(MainPageViewModel).GetProperty(nameof(MainPageViewModel.Counter))?.PropertyType);
        Assert.Equal("0", viewModel.Counter);
        viewModel.IncrementCommand.Execute(null);
        Assert.Equal("1", viewModel.Counter);
        Assert.Equal("1.0 MB", OperatorText.FileSize(1024 * 1024));
        Assert.Equal("1,234", OperatorText.Count(1234));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NoRawValueReachesTheView()
    {
        foreach (var viewModelType in BoundViewModelTypes())
        {
            foreach (var property in viewModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.False(
                    IsRawPresentationType(property.PropertyType),
                    $"{viewModelType.Name}.{property.Name} exposes {property.PropertyType.Name} to XAML.");
            }
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NoIdentifierEntryReachesTheView()
    {
        foreach (var viewModelType in BoundViewModelTypes())
        {
            foreach (var property in viewModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.SetMethod is not null
                    && (property.Name.EndsWith("Id", StringComparison.Ordinal)
                        || property.Name.EndsWith("Identifier", StringComparison.Ordinal)
                        || property.Name.EndsWith("Hash", StringComparison.Ordinal)))
                {
                    Assert.Fail($"{viewModelType.Name}.{property.Name} is a typed identifier input.");
                }
            }
        }

        foreach (var xaml in DesktopXamlFiles())
        {
            var content = File.ReadAllText(xaml);
            foreach (Match match in Regex.Matches(content, "<TextBox\\b[\\s\\S]*?/>|<TextBox\\b[\\s\\S]*?</TextBox>", RegexOptions.CultureInvariant))
            {
                Assert.DoesNotMatch("(?i)(?:Header|AutomationProperties\\.Name)\\s*=\\s*\"[^\"]*(?:id|identifier|guid|sha-?256|hash)[^\"]*\"", match.Value);
            }
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NoRawAggregateIdentifierReachesATargetColumn()
    {
        foreach (var xaml in DesktopXamlFiles())
        {
            var content = File.ReadAllText(xaml);
            Assert.DoesNotMatch("(?i)(?:Target|Reference)[^\\r\\n]*(?:Id|Guid|Hash|Aggregate)", content);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NoBannedWordReachesTheOperator()
    {
        var banned = new Regex(
            "(?i)\\b(?:intake|bounded|projection|lease|opaque|ingress|composed|artifact|durable|aggregate|caller|correlation identifier|bytes)\\b",
            RegexOptions.CultureInvariant);

        foreach (var viewModelType in BoundViewModelTypes())
        {
            var viewModel = Activator.CreateInstance(viewModelType)!;
            foreach (var property in viewModelType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType == typeof(string)
                    && property.GetValue(viewModel) is string value)
                {
                    Assert.False(banned.IsMatch(value), $"{viewModelType.Name}.{property.Name} exposes banned wording.");
                }
            }
        }
    }

    public static IEnumerable<object[]> CaseStages() =>
        CaseStageLabels.Select(pair => new object[] { pair.Key, pair.Value });

    public static IEnumerable<object[]> DocumentRoles() =>
        DocumentRoleLabels.Select(pair => new object[] { pair.Key, pair.Value });

    public static IEnumerable<object[]> DocumentOrigins() =>
        DocumentOriginLabels.Select(pair => new object[] { pair.Key, pair.Value });

    public static IEnumerable<object[]> IntakeDecisions() =>
        IntakeDecisionLabels.Select(pair => new object[] { pair.Key, pair.Value });

    public static IEnumerable<object[]> VrmOutcomes() =>
        VrmOutcomeLabels.Select(pair => new object[] { pair.Key, pair.Value });

    private static IEnumerable<Type> BoundViewModelTypes() =>
        typeof(MainPageViewModel).Assembly
            .GetTypes()
            .Where(type => type.IsPublic
                && type.Namespace?.StartsWith("Pegasus.Desktop.ViewModels", StringComparison.Ordinal) == true);

    private static IEnumerable<string> DesktopXamlFiles()
    {
        var root = FindRepositoryRoot();
        return Directory.EnumerateFiles(
            Path.Combine(root, "src", "Pegasus.Desktop"),
            "*.xaml",
            SearchOption.AllDirectories);
    }

    private static bool IsRawPresentationType(Type type) =>
        type == typeof(Guid)
        || type == typeof(Guid?)
        || type == typeof(byte[])
        || type.IsEnum
        || type == typeof(int)
        || type == typeof(long)
        || type == typeof(double)
        || type == typeof(decimal);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Pegasus.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root.");
    }
}
