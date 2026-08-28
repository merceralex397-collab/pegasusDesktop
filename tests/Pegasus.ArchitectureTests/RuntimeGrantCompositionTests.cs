using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Infrastructure.Persistence;
namespace Pegasus.ArchitectureTests;

public sealed class RuntimeGrantCompositionTests
{
    [Fact]
    public void CompositionRootWritesAreCoveredByRuntimeRoleGrants()
    {
        var root = FindRepositoryRoot();
        var catalogue = RuntimeGrantCatalogue.Load(root);

        var failures = catalogue.FindMissingGrants(catalogue.Grants);

        Assert.True(
            failures.Length == 0,
            "Composition-root runtime grants are incomplete:\n" + string.Join("\n", failures));
    }

    [Theory]
    [InlineData(
        "20260814092852_AddWorkerCaseCreationGrants.cs",
        "Cases",
        "INSERT")]
    [InlineData(
        "20260821095500_GrantWorkerVehicleLookupRequests.cs",
        "VehicleLookupRequests",
        "INSERT")]
    [InlineData(
        "20260822044425_GrantWorkerCaseDocuments.cs",
        "CaseDocuments",
        "INSERT")]
    public void HistoricalGrantRegressionsNameTheMissingTableAndVerb(
        string grantMigration,
        string table,
        string verb)
    {
        var root = FindRepositoryRoot();
        var catalogue = RuntimeGrantCatalogue.Load(root, grantMigration);
        var historicalWrites = catalogue.Writes
            .Concat(RuntimeGrantCatalogue.InferHistoricalStoreWrites(root, table))
            .ToArray();
        var failures = catalogue.FindMissingGrants(catalogue.Grants, historicalWrites, honorOptOuts: false);

        Assert.Contains(
            failures,
            failure => failure.Contains($"Worker {table} requires {verb}", StringComparison.Ordinal));
    }

    [Fact]
    public void UngrantedNewTableFixtureFailsAndGrantSatisfiesIt()
    {
        var root = FindRepositoryRoot();
        var catalogue = RuntimeGrantCatalogue.Load(root);
        var fixtureTable = "ArchitectureTestUnGrantedTable";
        var withoutFixtureGrant = catalogue.WithInferredFixture("Worker", fixtureTable);
        var failures = catalogue.FindMissingGrants(catalogue.Grants, withoutFixtureGrant);
        Assert.Contains(
            failures,
            failure => failure.Contains($"Worker {fixtureTable} requires INSERT", StringComparison.Ordinal));

        var withFixtureGrant = catalogue.Grants.Append(
            new RuntimeGrant(
                "Worker",
                fixtureTable,
                "INSERT",
                "fixture: grant"));
        Assert.DoesNotContain(
            catalogue.FindMissingGrants(withFixtureGrant.ToHashSet(), withoutFixtureGrant),
            failure => failure.Contains(fixtureTable, StringComparison.Ordinal));
    }

    [Fact]
    public void NoRuntimeGrantMarkerIsAcceptedWithAReason()
    {
        var catalogue = RuntimeGrantCatalogue.Load(FindRepositoryRoot());
        Assert.Contains("CaseDocuments", catalogue.OptedOutTables);

        var writes = catalogue.WithInferredFixture("Worker", "CaseDocuments");
        var grantsWithoutTable = catalogue.Grants
            .Where(grant => !grant.Table.Equals("CaseDocuments", StringComparison.Ordinal))
            .ToHashSet();

        Assert.DoesNotContain(
            catalogue.FindMissingGrants(grantsWithoutTable, writes),
            failure => failure.Contains("CaseDocuments", StringComparison.Ordinal));
    }

    [Fact]
    public void NoRuntimeGrantMarkerRequiresCreateAndReason()
    {
        Assert.True(RuntimeGrantCatalogue.IsValidOptOutMarker("// no-runtime-grant: Cases - consolidated role migration", true));
        Assert.False(RuntimeGrantCatalogue.IsValidOptOutMarker("// no-runtime-grant: Cases", true));
        Assert.False(RuntimeGrantCatalogue.IsValidOptOutMarker("// no-runtime-grant: Cases - consolidated role migration", false));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pegasus.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate Pegasus.slnx.");
    }

    private sealed record RuntimeWrite(
        string Role,
        string Table,
        IReadOnlySet<string> Verbs,
        string Source);

    private sealed record RuntimeGrant(string Role, string Table, string Verb, string SourceFile);

    private sealed class RuntimeGrantCatalogue
    {
        private static readonly Regex RegisteredEfType = new(
            @"(?:services|builder\.Services)\.Add(?:Scoped|Singleton|Transient)<(?<args>[^>]+)>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex FactoryRegistration = new(
            @"(?:services|builder\.Services)\.Add(?:Scoped|Singleton|Transient)<(?<interface>I[A-Za-z0-9_]+)>[\s\S]{0,220}?GetRequiredService<(?<store>Ef[A-Za-z0-9_]+)>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DbSetProperty = new(
            @"DbSet<(?<entity>[^>]+)>\s+(?<property>[A-Za-z_][A-Za-z0-9_]*)\s*=>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ContextProperty = new(
            @"\b(?:context|dbContext|verification|finalContext)\.(?<property>[A-Z][A-Za-z0-9_]*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ContextSet = new(
            @"\b(?:context|dbContext|verification|finalContext)\.Set<(?<entity>[A-Za-z_][A-Za-z0-9_]*)>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ContextMutation = new(
            @"\b(?:context|dbContext|verification|finalContext)\.(?:(?<property>[A-Z][A-Za-z0-9_]*)|Set<(?<entity>[A-Za-z_][A-Za-z0-9_]*)>\(\))\s*\.\s*(?<verb>Add|AddAsync|Remove|RemoveRange|ExecuteDelete|ExecuteUpdate)(?:Async)?\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DirectContextMutation = new(
            @"\b(?:context|dbContext|verification|finalContext)\.(?<verb>Add|AddAsync|Remove|RemoveRange)\s*\(\s*(?:new\s+)?(?<entity>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex VariableContextMutation = new(
            @"\b(?:context|dbContext|verification|finalContext)\.(?<verb>Add|AddAsync|Remove|RemoveRange)\s*\(\s*(?<variable>[a-z][A-Za-z0-9_]*)\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex VariableEntity = new(
            @"\b(?<entity>[A-Za-z_][A-Za-z0-9_]*Entity)\s+(?<variable>[a-z][A-Za-z0-9_]*)\s*=",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TrackedEntityMutation = new(
            @"\b(?<entity>[A-Za-z_][A-Za-z0-9_]*Entity)\s+(?<variable>[a-z][A-Za-z0-9_]*)\b[\s\S]*?\b\k<variable>\.[A-Z][A-Za-z0-9_]*\s*=",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantStatement = new(
            @"GRANT[^;]*;",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex BracketedIdentifier = new(
            @"\[(?<name>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantRole = new(
            @"\bTO\s+\[(?<role>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex RoleVariable = new(
            @"(?<name>WebRole|WorkerRole)\s*=\s*""(?<role>[^""\r\n]*runtime_role)""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex InterpolatedGrant = new(
            @"GRANT\s+(?<permissions>[A-Z][A-Z, ]*)\s+ON[^;]*?(?:\[[^]]+\]\.)?\[(?<table>[^]]+)\][^;]*?\{(?<role>WebRole|WorkerRole)\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DirectGrant = new(
            @"GRANT\s+(?<permissions>[A-Z][A-Z, ]*)\s+ON\s+OBJECT::(?:\[[^]]+\]\.)?\[(?<table>[^]]+)\]\s+TO\s+\[(?<role>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TupleGrant = new(
            @"\(\s*""(?<table>[A-Za-z0-9_]+)""\s*,\s*""(?<permissions>[A-Z][A-Z, ]*)""\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantArray = new(
            @"(?<name>[A-Za-z]*Grants)\s*=\s*\[(?<body>.*?)\];",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private static readonly HashSet<string> InsertOnly = new(StringComparer.Ordinal) { "INSERT" };
        private static readonly HashSet<string> UpdateOnly = new(StringComparer.Ordinal) { "UPDATE" };
        private static readonly string[] HostRoles = ["Web", "Worker"];
        private static readonly string[] WebOnly = ["Web"];

        private RuntimeGrantCatalogue(
            IReadOnlyList<RuntimeWrite> writes,
            IReadOnlySet<RuntimeGrant> grants,
            IReadOnlySet<string> optedOutTables)
        {
            Writes = writes;
            Grants = grants;
            OptedOutTables = optedOutTables;
        }

        internal IReadOnlyList<RuntimeWrite> Writes { get; }

        internal IReadOnlySet<RuntimeGrant> Grants { get; }

        internal IReadOnlySet<string> OptedOutTables { get; }

        internal static bool IsValidOptOutMarker(string line, bool createsTable)
        {
            var marker = Regex.Match(line,
                @"//\s*no-runtime-grant:\s*(?<table>[A-Za-z0-9_]+)\b(?<reason>.*)$",
                RegexOptions.CultureInvariant);
            return createsTable && marker.Success && !string.IsNullOrWhiteSpace(marker.Groups["reason"].Value.Trim(' ', '-', '\t'));
        }

        internal static RuntimeGrantCatalogue Load(string root, string? beforeMigration = null)
        {
            var persistenceRoot = Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence");
            var migrationRoot = Path.Combine(persistenceRoot, "Migrations");
            var contextSource = File.ReadAllText(Path.Combine(persistenceRoot, "PegasusDbContext.cs"));
            var tableNames = BuildTableNames(contextSource, persistenceRoot);
            var registeredStoreRoles = RegisteredStoreRoles(root);

            var writes = new List<RuntimeWrite>();
            foreach (var sourcePath in Directory.EnumerateFiles(persistenceRoot, "*.cs"))
            {
                var source = File.ReadAllText(sourcePath);
                var storeName = registeredStoreRoles.Keys.FirstOrDefault(
                    name => Regex.IsMatch(
                        source,
                        $@"\bclass\s+{Regex.Escape(name)}\b|\b{name}\s*\(",
                        RegexOptions.CultureInvariant));
                if (storeName is null || !IsWriteStore(source))
                {
                    continue;
                }

                var properties = ContextProperty.Matches(source)
                    .Select(match => match.Groups["property"].Value)
                    .Where(tableNames.ContainsKey)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                var entities = ContextSet.Matches(source)
                    .Select(match => match.Groups["entity"].Value)
                    .Where(tableNames.ContainsKey)
                    .Select(entity => (Entity: entity, Table: tableNames[entity]))
                    .Distinct()
                    .ToArray();
                foreach (var property in properties)
                {
                    var verbs = InferVerbs(source, property);
                    if (verbs.Count == 0)
                    {
                        continue;
                    }

                    AddRoleWrites(writes, registeredStoreRoles, storeName, tableNames[property], verbs, sourcePath);
                }
                foreach (var entity in entities)
                {
                    var verbs = InferVerbs(source, entity.Entity);
                    if (verbs.Count == 0)
                    {
                        continue;
                    }

                    AddRoleWrites(writes, registeredStoreRoles, storeName, entity.Table, verbs, sourcePath);
                }
                if (source.Contains("ExecuteSql", StringComparison.Ordinal))
                {
                    foreach (var table in tableNames.Values.Distinct(StringComparer.Ordinal)
                                 .Where(table => source.Contains(table, StringComparison.Ordinal)))
                    {
                        AddRoleWrites(
                            writes,
                            registeredStoreRoles,
                            storeName,
                            table,
                            UpdateOnly,
                            sourcePath);
                    }
                }
            }

            var parsed = ParseGrants(migrationRoot, beforeMigration);
            return new(writes, parsed.Grants, parsed.OptedOutTables);
        }

        internal RuntimeWrite[] WithInferredFixture(string role, string table)
        {
            var services = new ServiceCollection();
            services.AddScoped<ArchitectureTestUnGrantedTableStore>();
            var registration = services.Single(descriptor => descriptor.ServiceType == typeof(ArchitectureTestUnGrantedTableStore));
            if (registration.ImplementationType != typeof(ArchitectureTestUnGrantedTableStore))
            {
                throw new InvalidOperationException("Fixture store was not registered as the expected concrete type.");
            }

            var modelBuilder = new ModelBuilder();
            modelBuilder.Entity<ArchitectureTestUnGrantedTableEntity>().ToTable(table);
            var entity = modelBuilder.FinalizeModel().FindEntityType(typeof(ArchitectureTestUnGrantedTableEntity));
            var fixtureTable = entity?.GetTableName()
                ?? throw new InvalidOperationException("Fixture entity did not have an EF table mapping.");
            var fixtureFile = Path.Combine(FindRepositoryRoot(), "tests", "Pegasus.ArchitectureTests", "RuntimeGrantCompositionTests.cs");
            var fixtureSource = File.ReadAllText(fixtureFile);
            var fixtureStart = fixtureSource.IndexOf("private sealed class ArchitectureTestUnGrantedTableStore", StringComparison.Ordinal);
            fixtureSource = fixtureStart >= 0 ? fixtureSource[fixtureStart..] : throw new InvalidOperationException("Fixture store source was not found.");
            var fixture = new RuntimeWrite(
                role,
                fixtureTable,
                InferVerbs(fixtureSource, nameof(ArchitectureTestUnGrantedTableEntity)),
                "fixture: registered store and EF IModel entity");
            return Writes.Append(fixture).ToArray();
        }

        internal static RuntimeWrite[] InferHistoricalStoreWrites(string root, string table)
        {
            var storeName = table switch
            {
                "Cases" => "EfCaseAcceptanceStore",
                "VehicleLookupRequests" => "EfVehicleWorkflowStore",
                "CaseDocuments" => "EfDocumentCustodyStore",
                _ => throw new ArgumentOutOfRangeException(nameof(table), table, null)
            };
            var sourcePath = Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence", storeName + ".cs");
            var source = File.ReadAllText(sourcePath);
            var contextPath = Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence", "PegasusDbContext.cs");
            var tableNames = BuildTableNames(File.ReadAllText(contextPath), Path.GetDirectoryName(contextPath)!);
            return ContextSet.Matches(source)
                .Select(match => match.Groups["entity"].Value)
                .Concat(ContextProperty.Matches(source).Select(match => match.Groups["property"].Value))
                .Where(tableNames.ContainsKey)
                .Distinct(StringComparer.Ordinal)
                .Where(name => tableNames[name].Equals(table, StringComparison.Ordinal))
                .Select(name => (Name: name, Verbs: InferVerbs(source, name)))
                .Where(item => item.Verbs.Count > 0)
                .Select(item => new RuntimeWrite("Worker", table, item.Verbs, sourcePath))
                .ToArray();
        }

        internal string[] FindMissingGrants(
            IReadOnlySet<RuntimeGrant> grants,
            IReadOnlyList<RuntimeWrite>? writes = null,
            bool honorOptOuts = true)
        {
            var granted = grants
                .Select(grant => (grant.Role, grant.Table, grant.Verb))
                .ToHashSet();
            return (writes ?? Writes)
                .SelectMany(write => write.Verbs.Select(verb => (write, verb)))
                .Where(item => !honorOptOuts || !OptedOutTables.Contains(item.write.Table))
                .Where(item => !granted.Contains((item.write.Role, item.write.Table, item.verb)))
                .Select(item =>
                    $"{item.write.Role} {item.write.Table} requires {item.verb} ({item.write.Source})")
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        private static Dictionary<string, string> BuildTableNames(string contextSource, string persistenceRoot)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            using var context = new PegasusDbContext(new DbContextOptionsBuilder<PegasusDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ArchitectureModelOnly;")
                .Options);
            var mappings = context.Model.GetEntityTypes()
                .Select(entity => (Entity: entity.ClrType.Name, Table: entity.GetTableName()))
                .Where(mapping => mapping.Table is not null)
                .ToDictionary(mapping => mapping.Entity, mapping => mapping.Table!, StringComparer.Ordinal);

            foreach (var mapping in mappings)
            {
                names[mapping.Key] = mapping.Value;
            }

            foreach (Match match in DbSetProperty.Matches(contextSource))
            {
                var entity = match.Groups["entity"].Value;
                var property = match.Groups["property"].Value;
                if (mappings.TryGetValue(entity, out var table))
                {
                    names[property] = table;
                }
            }

            return names;
        }

        private static Dictionary<string, HashSet<string>> RegisteredStoreRoles(string root)
        {
            var source = File.ReadAllText(Path.Combine(root, "src", "Pegasus.Infrastructure", "DependencyInjection.cs"));
            var names = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            AddRegistrations(names, source, null, root);
            AddRegistrations(names, File.ReadAllText(Path.Combine(root, "src", "Pegasus.Web", "Program.cs")), "Web", root);
            AddRegistrations(names, File.ReadAllText(Path.Combine(root, "src", "Pegasus.Worker", "WorkerDependencyInjection.cs")), "Worker", root);

            return names;
        }

        private static void AddRegistrations(
            Dictionary<string, HashSet<string>> names,
            string source,
            string? directRole,
            string root)
        {
            foreach (Match match in RegisteredEfType.Matches(source))
            {
                var arguments = match.Groups["args"].Value.Split(',').Select(value => value.Trim()).ToArray();
                foreach (var type in arguments.Where(IsStoreType))
                {
                    if (!names.TryGetValue(type, out var roles))
                    {
                        roles = new HashSet<string>(StringComparer.Ordinal);
                        names[type] = roles;
                    }

                    if (directRole is not null)
                    {
                        roles.Add(directRole);
                        continue;
                    }

                    var interfaceName = arguments.FirstOrDefault(value => value.StartsWith('I'));
                    if (interfaceName is null)
                    {
                        continue;
                    }

                    foreach (var role in HostRoles)
                    {
                        var directory = role == "Web" ? "Pages" : "Functions";
                        var hostRoot = Path.Combine(root, "src", role == "Web" ? "Pegasus.Web" : "Pegasus.Worker", directory);
                        if (Directory.Exists(hostRoot) && Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
                            .Any(path =>
                            {
                                var hostSource = File.ReadAllText(path);
                                return HasWriteCall(hostSource, interfaceName);
                            }))
                        {
                            roles.Add(role);
                        }
                    }
                }
            }
            foreach (Match match in FactoryRegistration.Matches(source))
            {
                if (!names.TryGetValue(match.Groups["store"].Value, out var roles))
                {
                    roles = new HashSet<string>(StringComparer.Ordinal);
                    names[match.Groups["store"].Value] = roles;
                }
                AddInterfaceRoles(roles, match.Groups["interface"].Value, directRole, root);
            }
        }

        private static void AddInterfaceRoles(HashSet<string> roles, string interfaceName, string? directRole, string root)
        {
            if (directRole is not null)
            {
                roles.Add(directRole);
                return;
            }
            foreach (var role in HostRoles)
            {
                var directory = role == "Web" ? "Pages" : "Functions";
                var hostRoot = Path.Combine(root, "src", role == "Web" ? "Pegasus.Web" : "Pegasus.Worker", directory);
                if (Directory.Exists(hostRoot) && Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
                    .Any(path =>
                    {
                        var hostSource = File.ReadAllText(path);
                        return HasWriteCall(hostSource, interfaceName);
                    }))
                {
                    roles.Add(role);
                }
            }
        }

        private static bool HasWriteCall(string source, string interfaceName)
        {
            var variable = Regex.Match(source, $@"\b{Regex.Escape(interfaceName)}\s+(?<variable>[a-z][A-Za-z0-9_]*)\b")
                .Groups["variable"].Value;
            return variable.Length > 0 && Regex.IsMatch(
                source,
                $@"\b{Regex.Escape(variable)}\s*{WriteCallPattern}",
                RegexOptions.CultureInvariant);
        }

        private const string WriteCallPattern = @"\.(?:Register|Create|Add|Save|Update|Delete|Remove|Resolve)[A-Za-z0-9_]*(?:Async)?\s*\(";

        private static bool IsStoreType(string type) =>
            type.StartsWith("Ef", StringComparison.Ordinal) || type.Equals("EvaHandoffStore", StringComparison.Ordinal);

        private static void AddRoleWrites(
            List<RuntimeWrite> writes,
            Dictionary<string, HashSet<string>> storeRoles,
            string storeName,
            string table,
            HashSet<string> verbs,
            string sourcePath)
        {
            if (!storeRoles.TryGetValue(storeName, out var roles))
            {
                return;
            }

            foreach (var role in roles)
            {
                writes.Add(new(role, table, verbs, sourcePath));
            }
        }

        private static bool IsWriteStore(string source) =>
            source.Contains("SaveChanges", StringComparison.Ordinal) ||
            source.Contains("ExecuteUpdate", StringComparison.Ordinal) ||
            source.Contains("ExecuteDelete", StringComparison.Ordinal) ||
            source.Contains("ExecuteSql", StringComparison.Ordinal);

        private static HashSet<string> InferVerbs(string source, string property)
        {
            var verbs = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match mutation in ContextMutation.Matches(source))
            {
                if (!string.Equals(mutation.Groups["property"].Value, property, StringComparison.Ordinal)
                    && !string.Equals(mutation.Groups["entity"].Value, property, StringComparison.Ordinal))
                {
                    continue;
                }

                AddVerb(verbs, mutation.Groups["verb"].Value);
            }
            foreach (Match mutation in DirectContextMutation.Matches(source))
            {
                if (mutation.Groups["entity"].Value.Equals(property, StringComparison.Ordinal))
                {
                    AddVerb(verbs, mutation.Groups["verb"].Value);
                }
            }
            foreach (Match mutation in VariableContextMutation.Matches(source))
            {
                var variable = mutation.Groups["variable"].Value;
                if (VariableEntity.Matches(source).Cast<Match>().Any(declaration =>
                        declaration.Groups["variable"].Value.Equals(variable, StringComparison.Ordinal) &&
                        declaration.Groups["entity"].Value.Equals(property, StringComparison.Ordinal)))
                {
                    AddVerb(verbs, mutation.Groups["verb"].Value);
                }
            }
            if (Regex.IsMatch(
                    source,
                    $@"Set<{Regex.Escape(property)}>\(\)[\s\S]{{0,2000}}?\bcontext\.Add\s*\(",
                    RegexOptions.CultureInvariant))
            {
                verbs.Add("INSERT");
            }
            if (property.EndsWith("Entity", StringComparison.Ordinal) && TrackedEntityMutation.IsMatch(source))
            {
                verbs.Add("UPDATE");
            }

            return verbs;
        }

        private static void AddVerb(HashSet<string> verbs, string verb)
        {
            verbs.Add(verb switch
            {
                "Add" or "AddAsync" => "INSERT",
                "Remove" or "RemoveRange" or "ExecuteDelete" => "DELETE",
                "ExecuteUpdate" => "UPDATE",
                _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null)
            });
        }

        private static (HashSet<RuntimeGrant> Grants, HashSet<string> OptedOutTables) ParseGrants(
            string migrationRoot,
            string? beforeMigration)
        {
            // Keep this parser local because the PowerShell script is the CI-side
            // executable; this mirrors its literal GRANT and interpolated tuple
            // shapes while retaining role/verb detail for this architecture gate.
            var grants = new HashSet<RuntimeGrant>();
            var optedOutTables = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in Directory.EnumerateFiles(migrationRoot, "*.cs"))
            {
                var fileName = Path.GetFileName(path);
                if (fileName.EndsWith(".Designer.cs", StringComparison.Ordinal) ||
                    fileName.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
                {
                    continue;
                }
                if (beforeMigration is not null && string.CompareOrdinal(fileName, beforeMigration) >= 0)
                {
                    continue;
                }

                var source = File.ReadAllText(path);
                var roleVariables = RoleVariable.Matches(source).Cast<Match>()
                    .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["role"].Value,
                        StringComparer.Ordinal);
                var upStart = source.IndexOf("void Up(", StringComparison.Ordinal);
                var downStart = source.IndexOf("void Down(", StringComparison.Ordinal);
                var up = upStart >= 0
                    ? source.Substring(upStart, downStart > upStart ? downStart - upStart : source.Length - upStart)
                    : string.Empty;
                foreach (Match marker in Regex.Matches(
                             source,
                             @"//\s*no-runtime-grant:\s*(?<table>[A-Za-z0-9_]+)\b(?<reason>.*)$",
                             RegexOptions.CultureInvariant | RegexOptions.Multiline))
                {
                    var table = marker.Groups["table"].Value;
                    if (IsValidOptOutMarker(marker.Value, Regex.IsMatch(
                            up,
                            $@"CreateTable\s*\(\s*(?:name\s*:\s*)?""{Regex.Escape(table)}""",
                            RegexOptions.CultureInvariant)))
                    {
                        optedOutTables.Add(table);
                    }
                }
                foreach (Match match in GrantStatement.Matches(source))
                {
                    var table = GrantTargetTable(match.Value);
                    var roleMatch = GrantRole.Match(match.Value);
                    var role = roleMatch.Success
                        ? roleMatch.Groups["role"].Value
                        : Regex.Match(match.Value, @"\[\{(?<name>WebRole|WorkerRole)\}\]", RegexOptions.CultureInvariant)
                            .Groups["name"].Value is { Length: > 0 } variable && roleVariables.TryGetValue(variable, out var resolvedRole)
                            ? resolvedRole
                            : string.Empty;
                    if (string.IsNullOrEmpty(role) && match.Value.Contains("{WebRole}", StringComparison.Ordinal))
                    {
                        role = "pegasus_web_runtime_role";
                    }
                    if (string.IsNullOrEmpty(role) && match.Value.Contains("{WorkerRole}", StringComparison.Ordinal))
                    {
                        role = "pegasus_worker_runtime_role";
                    }
                    if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(role))
                    {
                        continue;
                    }

                    foreach (var permission in Permissions(match.Value))
                    {
                        grants.Add(new(RoleName(role), table, permission, fileName));
                    }
                }

                foreach (Match tuple in TupleGrant.Matches(source))
                {
                    var containingArray = GrantArray.Matches(source).Cast<Match>().FirstOrDefault(array =>
                        tuple.Index >= array.Groups["body"].Index &&
                        tuple.Index + tuple.Length <= array.Groups["body"].Index + array.Groups["body"].Length);
                    var roles = source.Contains("RuntimeRoles", StringComparison.Ordinal) &&
                                source.Contains("foreach (var role in RuntimeRoles)", StringComparison.Ordinal)
                        ? HostRoles
                        : containingArray?.Groups["name"].Value.Contains("Worker", StringComparison.Ordinal) == true
                            || fileName.Contains("Worker", StringComparison.Ordinal) ? new[] { "Worker" } : WebOnly;
                    foreach (var role in roles)
                    {
                        foreach (var permission in Permissions(tuple.Groups["permissions"].Value))
                        {
                            grants.Add(new(role, tuple.Groups["table"].Value, permission, fileName));
                        }
                    }
                }

                foreach (Match match in InterpolatedGrant.Matches(source))
                {
                    var role = match.Groups["role"].Value == "WorkerRole" ? "Worker" : "Web";
                    foreach (var permission in Permissions(match.Groups["permissions"].Value))
                    {
                        grants.Add(new(role, match.Groups["table"].Value, permission, fileName));
                    }
                }
                foreach (Match match in DirectGrant.Matches(source))
                {
                    foreach (var permission in Permissions(match.Groups["permissions"].Value))
                    {
                        grants.Add(new(RoleName(match.Groups["role"].Value), match.Groups["table"].Value, permission, fileName));
                    }
                }
            }

            return (grants, optedOutTables);
        }

        private static string[] Permissions(string value) =>
            (Regex.Match(value, @"GRANT\s+(?<permissions>[A-Z, ]+?)\s+ON\b", RegexOptions.CultureInvariant)
                .Groups["permissions"].Value is { Length: > 0 } grantPermissions
                ? grantPermissions
                : value)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        private static string RoleName(string role) =>
            role.Contains("worker", StringComparison.OrdinalIgnoreCase) ? "Worker" : "Web";

        private static string GrantTargetTable(string statement)
        {
            var target = statement[..statement.IndexOf("TO", StringComparison.OrdinalIgnoreCase)];
            var identifiers = BracketedIdentifier.Matches(target)
                .Cast<Match>()
                .Select(match => match.Groups["name"].Value)
                .ToArray();
            return identifiers.LastOrDefault() ?? string.Empty;
        }
    }

    private sealed class ArchitectureTestUnGrantedTableStore
    {
        public static void Write(PegasusDbContext context)
        {
            context.Set<ArchitectureTestUnGrantedTableEntity>().Add(new());
        }
    }

    private sealed class ArchitectureTestUnGrantedTableEntity
    {
        public int Id { get; set; }
    }
}
