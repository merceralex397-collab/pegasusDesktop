using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Pegasus.Core;
using Pegasus.Core.Cases;
using Pegasus.Core.Intake;
using Pegasus.Core.Custody;
using Pegasus.Worker.Functions;
using Pegasus.Core.Documents;
using Pegasus.Core.Eva;
using Pegasus.Web.Pages.Cases;
using Pegasus.Web.Pages.Uploads;
using Pegasus.Core.ReferenceData;
using Pegasus.Infrastructure;
using Pegasus.Infrastructure.Custody;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Contracts.Vocabulary;
using Pegasus.Web.Pages;
using Pegasus.Web.Presentation;

namespace Pegasus.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    private static readonly string[] ForbiddenCoreDependencyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Azure",
        "Microsoft.Graph",
        "Box",
        "MimeKit",
        "DocumentFormat.OpenXml",
        "UglyToad.PdfPig",
        "Microsoft.Data.SqlClient",
        "System.Net.Http",
        "OpenIddict",
        "ModelContextProtocol",
        "Pegasus.Infrastructure",
        "Pegasus.Web",
        "Pegasus.Worker"
    ];

    private static readonly string[] ForbiddenContractsDependencyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.WindowsAppSDK",
        "Microsoft.UI.Xaml",
        "Pegasus.Core",
        "Pegasus.Infrastructure",
        "Pegasus.Web",
        "Pegasus.Worker"
    ];

    [Fact]
    public void CoreHasNoInfrastructureOrHostDependencies()
    {
        var references = typeof(CoreAssembly).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => IsForbiddenCoreDependency(reference.Name ?? string.Empty));
    }

    [Theory]
    [InlineData("Azure.Storage.Blobs", true)]
    [InlineData("Microsoft.AspNetCore", true)]
    [InlineData("Microsoft.AspNetCore.Mvc", true)]
    [InlineData("Microsoft.EntityFrameworkCore", true)]
    [InlineData("Microsoft.Graph", true)]
    [InlineData("Box.V2", true)]
    [InlineData("DocumentFormat.OpenXml", true)]
    [InlineData("MimeKit", true)]
    [InlineData("UglyToad.PdfPig", true)]
    [InlineData("Microsoft.Data.SqlClient", true)]
    [InlineData("System.Net.Http", true)]
    [InlineData("Pegasus.Infrastructure", true)]
    [InlineData("Pegasus.Web", true)]
    [InlineData("Pegasus.Worker", true)]
    [InlineData("Azureish.Storage", false)]
    [InlineData("Microsoft.AspNetCorey", false)]
    [InlineData("Microsoft.EntityFrameworkCoreExtensions", false)]
    [InlineData("Microsoft.Graphical", false)]
    [InlineData("Boxed", false)]
    [InlineData("MimeKitten", false)]
    [InlineData("UglyToad.PdfPigment", false)]
    [InlineData("Microsoft.Data.SqlClientFactory", false)]
    [InlineData("System.Net.Httpish", false)]
    [InlineData("System.Collections", false)]
    public void CoreDependencyGuardDetectsForbiddenAndAllowedExamples(string assemblyName, bool expected)
    {
        Assert.Equal(expected, IsForbiddenCoreDependency(assemblyName));
    }

    [Fact]
    public void CoreProjectHasNoForbiddenDirectDependencies()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(root, "src/Pegasus.Core/Pegasus.Core.csproj"));

        Assert.Empty(ForbiddenDirectDependencies(document));
    }

    [Fact]
    public void ContractsProjectHasNoDependencies()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(root, "src/Pegasus.Contracts/Pegasus.Contracts.csproj"));

        Assert.DoesNotContain(
            document.Descendants(),
            element => element.Name.LocalName is
                "PackageReference" or "ProjectReference" or "FrameworkReference");
        Assert.Empty(ProjectReferences(root, "src/Pegasus.Contracts/Pegasus.Contracts.csproj"));

        var references = typeof(Pegasus.Contracts.ContractConventions).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            reference => IsForbiddenContractsDependency(reference.Name ?? string.Empty));
    }

    [Fact]
    public void OperatorVocabularyHasOneContractOwnerAndWebOnlyAdapter()
    {
        var root = FindRepositoryRoot();
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(path => new
            {
                Path = Path.GetRelativePath(root, path).Replace('\\', '/'),
                Content = File.ReadAllText(path)
            })
            .ToArray();

        Assert.Equal(
            ["src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs"],
            sourceFiles
                .Where(source => Regex.IsMatch(
                    source.Content,
                    @"\bclass\s+OperatorVocabulary\b",
                    RegexOptions.CultureInvariant))
                .Select(source => source.Path)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["src/Pegasus.Web/Presentation/OperatorLabels.cs"],
            sourceFiles
                .Where(source => Regex.IsMatch(
                    source.Content,
                    @"\bclass\s+OperatorLabels\b",
                    RegexOptions.CultureInvariant))
                .Select(source => source.Path)
                .Order(StringComparer.Ordinal));

        var vocabularyText = sourceFiles.Single(source => source.Path ==
            "src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs").Content;
        var sharedLabels = new[]
        {
            "Ready for case allocation",
            "Inspection and audit",
            "Inspection and Audit",
            "Needs text extraction",
            "Vehicle images registered",
            "No readable registration",
            "Recognition unavailable",
            "New instructions and Triage mail (Inbox)",
            "Exact report and Triage evidence (Sent Items)"
        };
        foreach (var label in sharedLabels)
        {
            Assert.Contains(label, vocabularyText, StringComparison.Ordinal);
        }

        var duplicateSources = sourceFiles
            .Where(source => source.Path != "src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs")
            .Where(source => sharedLabels.Any(label =>
                source.Content.Contains($"\"{label}\"", StringComparison.Ordinal)))
            .Select(source => source.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(duplicateSources);

        var movedEnumMapPattern = new Regex(
            @"\b(?:UnidentifiedReasonCode|UnidentifiedState|UnidentifiedMediaKind|CaseLifecycleState|CaseType|CaseDueWorkState|MailOperationalDestination|RepairSpecificationSourceRoute|DocumentSemanticRole|DocumentSource|ImageInitiatedCaseState|DocumentCustodyStatus|CaseCustodyState|RequestUploadStatus|IntakeDecision|ApprovedMailboxRouteScope|CaseInspectionMode|VehicleMileageEvidenceClass|VehicleMileageUnit|IntakeSourceChannel|VrmRecognitionOutcomeKind)\.[A-Za-z0-9_]+\s*=>\s*""[A-Z]",
            RegexOptions.CultureInvariant);
        var duplicateEnumMaps = sourceFiles
            .Where(source => source.Path.StartsWith("src/Pegasus.Web/", StringComparison.Ordinal))
            .Where(source => source.Path != "src/Pegasus.Web/Presentation/OperatorLabels.cs")
            .Where(source => movedEnumMapPattern.IsMatch(source.Content))
            .Select(source => source.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(duplicateEnumMaps);

        var movedMapNames = new[]
        {
            "AttachmentSearchability", "UnidentifiedReason", "UnidentifiedState",
            "UnidentifiedMediaKind", "EmailHandle", "AssociatedWithCase", "CaseStage",
            "CaseTypeName", "AttemptedCaseTypeName", "ChaseState", "ImageChaseState",
            "MailOperationalDestinationLabel", "RepairSpecificationRoute", "EstimateLineType",
            "DocumentRole", "DocumentOrigin", "ImageIntakeLifecycleState", "CustodyState",
            "CustodyFolderState", "UploadRequestState", "IntakeFailure", "IntakeDecisionLabel",
            "IntakeCannotBecomeCaseReason", "HistoryEvent", "RouteScope", "ChaseReason",
            "InspectionMode", "MileageEvidence", "MileageUnit", "SourceChannel",
            "VrmRecognitionOutcomeLabel", "MailClassification", "Humanise"
        };
        var duplicateNamedMaps = sourceFiles
            .Where(source => source.Path.StartsWith("src/Pegasus.Web/", StringComparison.Ordinal))
            .Where(source => source.Path != "src/Pegasus.Web/Presentation/OperatorLabels.cs")
            .Where(source => movedMapNames.Any(name => Regex.IsMatch(
                source.Content,
                $@"\b(?:public|private|protected|internal)\s+(?:static\s+)?[^;{{\r\n]+\b{name}\s*\([^)]*\)[\s\S]{{0,120}}\bswitch\b",
                RegexOptions.CultureInvariant)))
            .Select(source => source.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(duplicateNamedMaps);

        var details = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Pegasus.Web",
            "Pages",
            "Intake",
            "Details.cshtml.cs"));
        var message = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Pegasus.Web",
            "Pages",
            "Mail",
            "Message.cshtml.cs"));

        Assert.DoesNotContain("=> decision switch", details, StringComparison.Ordinal);
        Assert.DoesNotContain("=> suggestion.Outcome switch", details, StringComparison.Ordinal);
        Assert.DoesNotContain("=> decision switch", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Document text required", details, StringComparison.Ordinal);
        Assert.DoesNotContain("Technical failure", details, StringComparison.Ordinal);
        Assert.DoesNotContain("Document text required", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Technical failure", message, StringComparison.Ordinal);
    }

    [Fact]
    public void CoreDirectDependencyGuardDetectsForbiddenAndAllowedFixtures()
    {
        var document = XDocument.Parse(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Azure.Storage.Blobs" Version="1.0.0" />
                <PackageReference Update="MimeKit" Version="1.0.0" />
                <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="1.0.0" />
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
                <Reference Include="System.Net.Http" />
              </ItemGroup>
            </Project>
            """);

        Assert.Equal(
            ["Azure.Storage.Blobs", "Microsoft.AspNetCore.App", "MimeKit", "System.Net.Http"],
            ForbiddenDirectDependencies(document));
    }

    [Fact]
    public void ProjectReferencesFollowTheModularMonolithDirection()
    {
        var root = FindRepositoryRoot();

        Assert.Empty(ProjectReferences(root, "src/Pegasus.Core/Pegasus.Core.csproj"));
        Assert.Equal(
            ["Pegasus.Core"],
            ProjectReferences(root, "src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj"));
        Assert.Equal(
            ["Pegasus.Contracts", "Pegasus.Core", "Pegasus.Infrastructure"],
            ProjectReferences(root, "src/Pegasus.Web/Pegasus.Web.csproj"));
        Assert.Equal(
            ["Pegasus.Core", "Pegasus.Infrastructure"],
            ProjectReferences(root, "src/Pegasus.Worker/Pegasus.Worker.csproj"));
    }

    [Fact]
    public void ApplicationSolutionExcludesSourceWorkspaces()
    {
        var root = FindRepositoryRoot();
        var solution = XDocument.Load(Path.Combine(root, "Pegasus.slnx"));
        var projectPaths = solution
            .Descendants("Project")
            .Select(element => (string?)element.Attribute("Path"))
            .Where(path => path is not null)
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            // FND-029, FND-030, FND-031, FND-038, and TEST-001 extend this
            // exact list as their projects are added to Pegasus.slnx.
            [
                "src/Pegasus.Contracts/Pegasus.Contracts.csproj",
                "src/Pegasus.Core/Pegasus.Core.csproj",
                "src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj",
                "src/Pegasus.Web/Pegasus.Web.csproj",
                "src/Pegasus.Worker/Pegasus.Worker.csproj",
                "tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj",
                "tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj",
                "tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj",
                "tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj"
            ],
            projectPaths);
        Assert.DoesNotContain(projectPaths, path =>
            path.StartsWith("workspaces/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ServerSolutionFilterContainsExactlyTheServerProjects()
    {
        var projectPaths = ReadServerSolutionFilterProjects(FindRepositoryRoot());

        Assert.Equal(
            [
                "src/Pegasus.Contracts/Pegasus.Contracts.csproj",
                "src/Pegasus.Core/Pegasus.Core.csproj",
                "src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj",
                "src/Pegasus.Web/Pegasus.Web.csproj",
                "src/Pegasus.Worker/Pegasus.Worker.csproj",
                "tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj",
                "tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj",
                "tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj"
            ],
            projectPaths);
    }

    [Fact]
    public void ServerSolutionFilterExcludesWindowsTargetedProjects()
    {
        var projectPaths = ReadServerSolutionFilterProjects(FindRepositoryRoot());

        Assert.DoesNotContain(projectPaths, path =>
            path.StartsWith("src/Pegasus.Desktop", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("tests/Pegasus.Desktop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplicationProjectsDoNotReferenceSourceWorkspaces()
    {
        var root = FindRepositoryRoot();
        var solution = XDocument.Load(Path.Combine(root, "Pegasus.slnx"));

        foreach (var projectPath in solution
            .Descendants("Project")
            .Select(element => (string?)element.Attribute("Path"))
            .Where(path => path is not null)
            .Cast<string>())
        {
            var project = XDocument.Load(Path.Combine(root, projectPath));
            Assert.DoesNotContain(
                project.Descendants("ProjectReference"),
                reference => ((string?)reference.Attribute("Include"))?
                    .Contains("workspaces", StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    [Fact]
    public void ReportRenderingHasOneCorePortAndOneInfrastructureAdapter()
    {
        var core = typeof(Pegasus.Core.Reports.IAssessmentReportRenderer).Assembly;
        var infrastructure = typeof(Pegasus.Infrastructure.DependencyInjection).Assembly;
        var port = typeof(Pegasus.Core.Reports.IAssessmentReportRenderer);

        Assert.DoesNotContain(core.GetReferencedAssemblies(), x =>
            x.Name is "Scriban" or "Microsoft.Playwright" or "PdfSharp");
        Assert.Single(infrastructure.GetTypes(), type =>
            !type.IsAbstract && port.IsAssignableFrom(type));
        Assert.DoesNotContain(infrastructure.GetTypes(), type =>
            type.FullName?.Contains("CollisionRenderer", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void IntakeOrchestrationUsesOneExplicitExtractionPolicyBoundary()
    {
        var constructor = Assert.Single(typeof(ProcessIntake).GetConstructors());
        var parameters = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(IIntakeSourceReader), parameters);
        Assert.Contains(typeof(IIntakeReceiptStore), parameters);
        Assert.Contains(typeof(IIntakeArtifactStore), parameters);
        Assert.Contains(typeof(IInstructionExtractionPolicy), parameters);
        Assert.DoesNotContain(typeof(QdosInstructionExtractionPolicy), parameters);

        var implementations = typeof(CoreAssembly).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IInstructionExtractionPolicy).IsAssignableFrom(type))
            .ToArray();
        Assert.Equal([typeof(QdosInstructionExtractionPolicy)], implementations);
    }

    [Fact]
    public void WebCompositionDoesNotOwnTheWorkerIntakeProcessor()
    {
        var webAssembly = typeof(Pegasus.Web.Pages.Cases.CreateModel).Assembly;

        Assert.DoesNotContain(
            webAssembly.GetReferencedAssemblies(),
            reference => reference.Name is "Pegasus.Worker" or "Azure.Storage.Queues");
        Assert.DoesNotContain(
            webAssembly.GetTypes(),
            type => typeof(IProcessQueuedIntake).IsAssignableFrom(type));
    }

    [Fact]
    public void ProviderReferenceRuntimeKeepsWorkbookAuthoringOutsideApplicationProjects()
    {
        var root = FindRepositoryRoot();
        var runtimeProjects = new[]
        {
            "src/Pegasus.Core/Pegasus.Core.csproj",
            "src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj",
            "src/Pegasus.Web/Pegasus.Web.csproj",
            "src/Pegasus.Worker/Pegasus.Worker.csproj"
        };

        foreach (var project in runtimeProjects)
        {
            var document = XDocument.Load(Path.Combine(root, project));
            Assert.DoesNotContain(
                document.Descendants().Select(element => (string?)element.Attribute("Include")),
                include => include is not null &&
                    (include.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     include.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ||
                     include.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)));
        }

        var resources = typeof(InfrastructureAssembly).Assembly.GetManifestResourceNames();
        Assert.Contains(
            "Pegasus.Infrastructure.Persistence.ReferenceData.provider-domains.v1.json",
            resources);
        Assert.DoesNotContain(resources, name => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProviderReferenceCatalogBoundaryHasOneInfrastructureImplementation()
    {
        Assert.Equal(typeof(CoreAssembly).Assembly, typeof(IProviderReferenceCatalog).Assembly);

        var implementations = typeof(InfrastructureAssembly).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IProviderReferenceCatalog).IsAssignableFrom(type))
            .ToArray();
        var implementation = Assert.Single(implementations);
        Assert.False(implementation.IsPublic);
        Assert.Equal("EfProviderReferenceCatalog", implementation.Name);
    }

    [Fact]
    public async Task ExternalWorkFunctionsRouteOnlyValidIdentifiersToOwningPorts()
    {
        var processor = new RecordingCustodyProcessor();
        var workStore = new RecordingExternalWorkStore();
        var workId = Guid.NewGuid();

        await new ExternalWorkFunction(processor).RunAsync(workId.ToString("D"), CancellationToken.None);
        var poisonReconciler = new ReconcilePoisonedQueueWork(
            null!,
            new ReconcilePoisonedExternalWork(workStore, TimeProvider.System));
        await new ExternalPoisonFunction(poisonReconciler)
            .RunAsync(workId.ToString("N"), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new ExternalWorkFunction(processor).RunAsync("not-a-work-id", CancellationToken.None));

        Assert.Equal([workId], processor.ProcessedIds);
        Assert.Equal([workId], workStore.PoisonedIds);
    }

    private sealed class RecordingCustodyProcessor : IProcessQueuedExternalWork
    {
        public List<Guid> ProcessedIds { get; } = [];

        public Task ExecuteAsync(Guid workId, CancellationToken cancellationToken)
        {
            ProcessedIds.Add(workId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingExternalWorkStore : IExternalWorkStore
    {
        public List<Guid> PoisonedIds { get; } = [];

        public Task<ExternalWorkDispatchClaim?> ClaimDispatchAsync(
            DateTimeOffset nowUtc,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) =>
            Task.FromResult<ExternalWorkDispatchClaim?>(null);

        public Task MarkDispatchedAsync(
            Guid workItemId,
            string leaseToken,
            DateTimeOffset dispatchedAtUtc,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReleaseDispatchAsync(
            Guid workItemId,
            string leaseToken,
            DateTimeOffset dueAtUtc,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task MarkPoisonedAsync(
            Guid workItemId,
            DateTimeOffset failedAtUtc,
            CancellationToken cancellationToken)
        {
            PoisonedIds.Add(workItemId);
            return Task.CompletedTask;
        }

        public Task<bool> HoldsProcessingLeaseAsync(
            Guid workItemId,
            string leaseToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task FailProcessingAsync(
            Guid workItemId,
            string leaseToken,
            DateTimeOffset failedAtUtc,
            string failureCode,
            string failureReason,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    [Fact]
    public void WebCustodialPagesHaveNoDormantTransportPath()
    {
        var casePageDependencies = TypeInspection.OnlyConstructorParameterTypes(typeof(DetailsModel));
        var custodyPageDependencies = TypeInspection.OnlyConstructorParameterTypes(typeof(CustodyModel));
        var requestPageDependencies = TypeInspection.OnlyConstructorParameterTypes(typeof(RequestModel));

        Assert.Contains(typeof(IGetCase), casePageDependencies);
        Assert.Contains(typeof(IAddCaseDocument), custodyPageDependencies);
        Assert.Contains(typeof(ICreateRequestUploadLink), custodyPageDependencies);
        Assert.Contains(typeof(IRevokeRequestUploadLink), custodyPageDependencies);
        Assert.Contains(typeof(IGetRequestUpload), requestPageDependencies);
        Assert.Contains(typeof(IUploadToRequest), requestPageDependencies);
    }

    [Fact]
    public void WebPagesHaveOneStaffActorAndOperationKeyOwnerPerConcept()
    {
        var pagesRoot = Path.Combine(FindRepositoryRoot(), "src", "Pegasus.Web", "Pages");
        var sources = Directory
            .EnumerateFiles(pagesRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = Path.GetRelativePath(pagesRoot, path).Replace('\\', '/'),
                Content = File.ReadAllText(path)
            })
            .ToArray();

        Assert.Equal(
            ["StaffPageModel.cs"],
            sources
                .Where(source => source.Content.Contains(
                    "StaffActorFactory.TryCreate",
                    StringComparison.Ordinal))
                .Select(source => source.Path)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["StaffPageModel.cs", "Upload.cshtml.cs"],
            sources
                .SelectMany(source => Regex
                    .Matches(source.Content, "Guid\\.NewGuid\\(\\)\\.ToString\\(\"N\"\\)")
                    .Select(_ => source.Path))
                .Order(StringComparer.Ordinal));
        Assert.NotNull(typeof(RequestModel).GetCustomAttribute<
            Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>());
        Assert.False(typeof(StaffPageModel).IsAssignableFrom(typeof(RequestModel)));
    }

    [Fact]
    public void CustodyAndEvaPoliciesHaveOneCoreOwnerAndAdaptersRemainAtBoundaries()
    {
        Assert.Equal(typeof(IGenerateEvaHandoff).Assembly, typeof(GenerateEvaHandoff).Assembly);
        Assert.Equal(typeof(IRetryCaseCustody).Assembly, typeof(RetryCaseCustody).Assembly);
        Assert.Equal(typeof(EvaHandoffPolicy).Assembly, typeof(GenerateEvaHandoff).Assembly);

        Assert.Equal(typeof(DependencyInjection).Assembly, typeof(EvaHandoffStore).Assembly);
        var boxAdapter = Assert.Single(
            typeof(DependencyInjection).Assembly.GetTypes(),
            type => type.FullName == "Pegasus.Infrastructure.Custody.BoxCaseCustody");
        Assert.Equal(typeof(DependencyInjection).Assembly, boxAdapter.Assembly);
        Assert.Contains(typeof(IEvaHandoffPersistence), typeof(EvaHandoffStore).GetInterfaces());
        Assert.Contains(typeof(ICaseCustody), boxAdapter.GetInterfaces());
        Assert.All(
            typeof(IEvaHandoffPersistence).GetMethods(),
            method => Assert.Contains(
                method.GetParameters(),
                parameter => parameter.ParameterType == typeof(EvaHandoffPolicyAuthority)));
        Assert.Contains(
            typeof(ICustodyRecoveryPersistence).GetMethod("RetryAsync")!.GetParameters(),
            parameter => parameter.ParameterType == typeof(CustodyRetryPolicyAuthority));
        Assert.Empty(typeof(EvaHandoffPolicyAuthority).GetConstructors());
        Assert.Empty(typeof(CustodyRetryPolicyAuthority).GetConstructors());

        var repositoryRoot = FindRepositoryRoot();
        var evaPersistence = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Pegasus.Infrastructure",
            "Persistence",
            "EvaHandoffStore.cs"));
        var custodyPersistence = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Pegasus.Infrastructure",
            "Persistence",
            "EfExternalWorkStore.cs"));
        Assert.Contains("policy.Evaluate", evaPersistence, StringComparison.Ordinal);
        Assert.Contains("policy.SelectEligibleImages", evaPersistence, StringComparison.Ordinal);
        Assert.Contains("policy.DecideRevision", evaPersistence, StringComparison.Ordinal);
        Assert.Contains("policy.Decide", custodyPersistence, StringComparison.Ordinal);
        Assert.DoesNotContain(
            typeof(EvaHandoffStore).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "Pegasus.Web" or "Pegasus.Worker");
    }

    [Fact]
    public void WorkerFunctionAppUsesItsAssignedIdentityForKeyVaultReferences()
    {
        var platformBicep = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "infra",
            "modules",
            "platform.bicep"));
        var workerApp = Regex.Match(
            platformBicep,
            @"resource\s+workerApp\b[\s\S]*?(?=\r?\nresource\s+webHttp5xxAlert\b)",
            RegexOptions.CultureInvariant);

        Assert.True(workerApp.Success, "The Worker Function App resource was not found.");
        Assert.Matches(
            @"identity:\s*\{[\s\S]*?userAssignedIdentities:\s*\{\s*'\$\{workerIdentity\.id\}':\s*\{\s*\}\s*\}",
            workerApp.Value);
        Assert.Matches(
            @"keyVaultReferenceIdentity:\s*workerIdentity\.id",
            workerApp.Value);
    }

    [Fact]
    public void TransientIntakeTagLifecycleUsesContainerScopedBlobDataOwner()
    {
        var platformBicep = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "infra",
            "modules",
            "platform.bicep"));

        foreach (var resourceName in new[]
                 {
                     "workerTransientCustodyOwner",
                     "webTransientCustodyOwner"
                 })
        {
            var assignment = Regex.Match(
                platformBicep,
                $@"resource\s+{resourceName}\b[\s\S]*?\n\}}",
                RegexOptions.CultureInvariant);

            Assert.True(assignment.Success, $"{resourceName} was not found.");
            Assert.Contains("scope: transientIntakeContainer", assignment.Value);
            Assert.Contains("roleDefinitionId: blobDataOwnerRole", assignment.Value);
            Assert.DoesNotContain("blobDataContributorRole", assignment.Value);
        }
    }

    private static bool IsForbiddenCoreDependency(string assemblyName) =>
        ForbiddenCoreDependencyPrefixes.Any(prefix =>
            assemblyName.Equals(prefix, StringComparison.Ordinal) ||
            assemblyName.StartsWith($"{prefix}.", StringComparison.Ordinal));

    private static bool IsForbiddenContractsDependency(string assemblyName) =>
        ForbiddenContractsDependencyPrefixes.Any(prefix =>
            assemblyName.Equals(prefix, StringComparison.Ordinal) ||
            assemblyName.StartsWith($"{prefix}.", StringComparison.Ordinal));

    private static string[] ForbiddenDirectDependencies(XDocument document) =>
        document
            .Descendants()
            .Where(element => element.Name.LocalName is
                "PackageReference" or "FrameworkReference" or "Reference")
            .Select(element =>
                (string?)element.Attribute("Include") ??
                (string?)element.Attribute("Update") ??
                $"(unnamed {element.Name.LocalName})")
            .Where(IsForbiddenCoreDependency)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] ProjectReferences(string root, string relativeProjectPath)
    {
        var document = XDocument.Load(Path.Combine(root, relativeProjectPath));

        return document
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            // MSBuild Include paths use backslashes, which are not path separators
            // on Linux; normalize before taking the file name so both platforms agree.
            .Select(include => Path.GetFileNameWithoutExtension(include?.Replace('\\', '/')))
            .Where(name => name is not null)
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] ReadServerSolutionFilterProjects(string root)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Pegasus.Server.slnf")));

        return document
            .RootElement
            .GetProperty("solution")
            .GetProperty("projects")
            .EnumerateArray()
            .Select(project => project.GetString()?.Replace('\\', '/')
                ?? throw new InvalidOperationException("Server solution filter contains a non-string project entry."))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pegasus.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate Pegasus.slnx.");
    }
}
