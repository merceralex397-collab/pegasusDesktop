using System.Text.RegularExpressions;
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
        var catalogue = RuntimeGrantCatalogue.Load(root);
        var grantsBeforeFix = catalogue.Grants
            .Where(grant => !string.Equals(grant.SourceFile, grantMigration, StringComparison.Ordinal))
            .ToHashSet();
        var historicalWrite = new RuntimeWrite(
            "Worker",
            table,
            new HashSet<string>(StringComparer.Ordinal) { verb },
            $"fixture: {grantMigration}");

        var failures = catalogue.FindMissingGrants(
            grantsBeforeFix,
            catalogue.Writes.Append(historicalWrite).ToArray());

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
        var fixtureWrite = new RuntimeWrite(
            "Worker",
            fixtureTable,
            new HashSet<string>(StringComparer.Ordinal) { "INSERT" },
            "fixture: ungranted table");

        var withoutFixtureGrant = catalogue.Writes.Append(fixtureWrite).ToArray();
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
        const string migration = "// no-runtime-grant: ArchitectureTestReadOnly\n" +
                                  "// read-only reference table; no runtime role writes it.";

        Assert.True(RuntimeGrantCatalogue.HasOptOut(migration, "ArchitectureTestReadOnly"));
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
            @"services\.Add(?:Scoped|Singleton|Transient)<(?<args>[^>]+)>",
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

        private static readonly Regex LiteralGrant = new(
            @"GRANT\s+(?<permissions>[A-Z, ]+)\s+ON OBJECT::\[dbo\]\.\[(?<table>[A-Za-z0-9_]+)\]\s+TO\s+\[(?<role>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TupleGrant = new(
            @"\(\s*""(?<table>[A-Za-z0-9_]+)""\s*,\s*""(?<permissions>[A-Z, ]+)""\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantArray = new(
            @"(?<name>[A-Za-z]*Grants)\s*=\s*\[(?<body>.*?)\];",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private RuntimeGrantCatalogue(
            IReadOnlyList<RuntimeWrite> writes,
            IReadOnlySet<RuntimeGrant> grants)
        {
            Writes = writes;
            Grants = grants;
        }

        internal IReadOnlyList<RuntimeWrite> Writes { get; }

        internal IReadOnlySet<RuntimeGrant> Grants { get; }

        internal static RuntimeGrantCatalogue Load(string root)
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
            }

            return new(writes, ParseGrants(migrationRoot).ToHashSet());
        }

        internal string[] FindMissingGrants(
            IReadOnlySet<RuntimeGrant> grants,
            IReadOnlyList<RuntimeWrite>? writes = null)
        {
            var granted = grants
                .Select(grant => (grant.Role, grant.Table, grant.Verb))
                .ToHashSet();
            return (writes ?? Writes)
                .SelectMany(write => write.Verbs.Select(verb => (write, verb)))
                .Where(item => !granted.Contains((item.write.Role, item.write.Table, item.verb)))
                .Select(item =>
                    $"{item.write.Role} {item.write.Table} requires {item.verb} ({item.write.Source})")
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        internal static bool HasOptOut(string migration, string table) =>
            Regex.IsMatch(
                migration,
                $@"//\s*no-runtime-grant:\s*{Regex.Escape(table)}\b[^\r\n]*(?:\r?\n\s*//[^\r\n]*)+",
                RegexOptions.CultureInvariant);

        private static Dictionary<string, string> BuildTableNames(string contextSource, string persistenceRoot)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in DbSetProperty.Matches(contextSource))
            {
                var entity = match.Groups["entity"].Value;
                var property = match.Groups["property"].Value;
                names[property] = property;
                names[entity] = property;
            }

            foreach (var sourcePath in Directory.EnumerateFiles(persistenceRoot, "*ModelConfiguration.cs"))
            {
                var source = File.ReadAllText(sourcePath);
                var configuredEntity = Regex.Match(
                    source,
                    @"(?:IEntityTypeConfiguration<|(?:modelBuilder|builder)\.Entity<)(?<entity>[A-Za-z_][A-Za-z0-9_]*)",
                    RegexOptions.CultureInvariant).Groups["entity"].Value;
                foreach (Match match in Regex.Matches(
                             source,
                              @"entity\.ToTable\(""(?<table>[A-Za-z0-9_]+)""",
                             RegexOptions.CultureInvariant))
                {
                    var table = match.Groups["table"].Value;
                    names[table] = table;
                    if (!string.IsNullOrEmpty(configuredEntity))
                    {
                        names[configuredEntity] = table;
                    }
                }
            }

            return names;
        }

        private static Dictionary<string, HashSet<string>> RegisteredStoreRoles(string root)
        {
            var source = File.ReadAllText(Path.Combine(root, "src", "Pegasus.Infrastructure", "DependencyInjection.cs"));
            var names = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (Match match in RegisteredEfType.Matches(source))
            {
                var arguments = match.Groups["args"].Value.Split(',').Select(value => value.Trim()).ToArray();
                foreach (var type in arguments)
                {
                    if (type.StartsWith('E') && type.StartsWith("Ef", StringComparison.Ordinal) ||
                        type.Equals("EvaHandoffStore", StringComparison.Ordinal))
                    {
                        var interfaceName = arguments.FirstOrDefault(value => value.StartsWith('I'));
                        if (interfaceName is null)
                        {
                            continue;
                        }

                        var roles = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var role in new[] { "Web", "Worker" })
                        {
                            var directory = role == "Web" ? "Pages" : "Functions";
                            var hostRoot = Path.Combine(root, "src", role == "Web" ? "Pegasus.Web" : "Pegasus.Worker", directory);
                            if (Directory.Exists(hostRoot) && Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
                                .Any(path => File.ReadAllText(path).Contains(interfaceName, StringComparison.Ordinal)))
                            {
                                roles.Add(role);
                            }
                        }

                        if (roles.Count > 0)
                        {
                            names[type] = roles;
                        }
                    }
                }
            }

            return names;
        }

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
            var propertyOccurrences = Regex.Matches(
                source,
                $@"\b{Regex.Escape(property)}\b",
                RegexOptions.CultureInvariant);
            foreach (Match occurrence in propertyOccurrences)
            {
                var start = Math.Max(0, occurrence.Index - 250);
                var length = Math.Min(source.Length - start, 500);
                var window = source.Substring(start, length);
                if (Regex.IsMatch(window, @"\.(?:Add|AddAsync)\s*\(", RegexOptions.CultureInvariant))
                {
                    verbs.Add("INSERT");
                }
                if (Regex.IsMatch(window, @"\.(?:Remove|ExecuteDelete)\s*\(", RegexOptions.CultureInvariant))
                {
                    verbs.Add("DELETE");
                }
                if (Regex.IsMatch(window, @"ExecuteUpdate\s*\(", RegexOptions.CultureInvariant))
                {
                    verbs.Add("UPDATE");
                }
            }

            return verbs;
        }

        private static HashSet<RuntimeGrant> ParseGrants(string migrationRoot)
        {
            var grants = new HashSet<RuntimeGrant>();
            foreach (var path in Directory.EnumerateFiles(migrationRoot, "*.cs"))
            {
                var fileName = Path.GetFileName(path);
                if (fileName.EndsWith(".Designer.cs", StringComparison.Ordinal) ||
                    fileName.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var source = File.ReadAllText(path);
                foreach (Match match in LiteralGrant.Matches(source))
                {
                    foreach (var permission in Permissions(match.Groups["permissions"].Value))
                    {
                        grants.Add(new(RoleName(match.Groups["role"].Value), match.Groups["table"].Value, permission, fileName));
                    }
                }

                foreach (Match match in GrantArray.Matches(source))
                {
                    var role = match.Groups["name"].Value.Contains("Worker", StringComparison.Ordinal)
                        || fileName.Contains("Worker", StringComparison.Ordinal)
                        ? "Worker"
                        : "Web";
                    foreach (Match tuple in TupleGrant.Matches(match.Groups["body"].Value))
                    {
                        foreach (var permission in Permissions(tuple.Groups["permissions"].Value))
                        {
                            grants.Add(new(role, tuple.Groups["table"].Value, permission, fileName));
                        }
                    }
                }
            }

            return grants;
        }

        private static string[] Permissions(string value) =>
            value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        private static string RoleName(string role) =>
            role.Contains("worker", StringComparison.OrdinalIgnoreCase) ? "Worker" : "Web";
    }
}
