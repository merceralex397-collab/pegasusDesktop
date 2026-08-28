using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
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
    [InlineData("20260814092852_AddWorkerCaseCreationGrants.cs", "Cases", "INSERT")]
    [InlineData("20260821095500_GrantWorkerVehicleLookupRequests.cs", "VehicleLookupRequests", "INSERT")]
    [InlineData("20260822044425_GrantWorkerCaseDocuments.cs", "CaseDocuments", "INSERT")]
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
            new RuntimeGrant("Worker", fixtureTable, "INSERT", "fixture: grant"));
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
        Assert.True(RuntimeGrantCatalogue.IsValidOptOutMarker(
            "// no-runtime-grant: Cases - consolidated role migration", true));
        Assert.False(RuntimeGrantCatalogue.IsValidOptOutMarker("// no-runtime-grant: Cases", true));
        Assert.False(RuntimeGrantCatalogue.IsValidOptOutMarker(
            "// no-runtime-grant: Cases - consolidated role migration", false));
    }

    [Fact]
    public void GrantParserMatchesLiteralAndSharedTupleShapes()
    {
        var grants = RuntimeGrantCatalogue.ParseTupleFixture("""
            const string WebRole = "pegasus_web_runtime_role";
            const string WorkerRole = "pegasus_worker_runtime_role";
            var RuntimeRoles = new[] { "web", "worker" };
            var RuntimeGrants = new[] { ("FixtureTable", "SELECT, INSERT") };
            Grant(migrationBuilder, WebRole, WebGrants);
            migrationBuilder.Sql("GRANT UPDATE ON OBJECT::[dbo].[LiteralTable] TO [pegasus_worker_runtime_role];");
            foreach (var role in RuntimeRoles) { foreach (var grant in RuntimeGrants) { } }
            """);

        Assert.Contains(grants, grant => grant.Role == "Web" && grant.Table == "FixtureTable" && grant.Verb == "INSERT");
        Assert.Contains(grants, grant => grant.Role == "Worker" && grant.Table == "FixtureTable" && grant.Verb == "INSERT");
        Assert.Contains(grants, grant => grant.Role == "Worker" && grant.Table == "LiteralTable" && grant.Verb == "UPDATE");
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
        private static readonly Regex DbSetProperty = new(
            @"DbSet<(?<entity>[^>]+)>\s+(?<property>[A-Za-z_][A-Za-z0-9_]*)\s*=>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantStatement = new(
            @"GRANT[^;]*;",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex BracketedIdentifier = new(
            @"\[(?<name>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantRole = new(
            @"\bTO\s+\[(?<role>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex RoleVariable = new(
            @"(?<name>WebRole|WorkerRole)\s*=\s*['""](?<role>[^'""\r\n]*runtime_role)['""]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex InterpolatedRole = new(
            @"\[\{(?<name>WebRole|WorkerRole)\}\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TupleGrant = new(
            @"\(\s*['""](?<table>[A-Za-z0-9_]+)['""]\s*,\s*['""](?<permissions>[A-Z][A-Z, ]*)['""]\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantArray = new(
            @"(?<name>[A-Za-z]*Grants)\s*=\s*\[(?<body>.*?)\];",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private static readonly Regex GrantCall = new(
            @"\bGrant\s*\(\s*[^,]+,\s*(?<role>WebRole|WorkerRole)\s*,\s*(?<array>[A-Za-z][A-Za-z0-9_]*)\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> HostRoles = ["Web", "Worker"];

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
            return createsTable && marker.Success &&
                   !string.IsNullOrWhiteSpace(marker.Groups["reason"].Value.Trim(' ', '-', '\t'));
        }

        internal static HashSet<RuntimeGrant> ParseTupleFixture(string source) =>
            ParseGrantContent(source, "fixture", beforeMigration: null).Grants;

        internal static RuntimeGrantCatalogue Load(string root, string? beforeMigration = null)
        {
            var persistenceRoot = Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence");
            var migrationRoot = Path.Combine(persistenceRoot, "Migrations");
            var contextSource = File.ReadAllText(Path.Combine(persistenceRoot, "PegasusDbContext.cs"));
            var tableNames = BuildTableNames(contextSource);
            var contextMembers = BuildContextMembers(contextSource);
            var registeredStoreUsages = RuntimeGrantCompositionAnalyzer.DiscoverStoreUsages(root);
            var writes = DiscoverWrites(
                Directory.EnumerateFiles(persistenceRoot, "*.cs")
                    .Where(path => !path.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase)),
                registeredStoreUsages,
                tableNames,
                contextMembers);

            var parsed = ParseGrants(migrationRoot, beforeMigration);
            return new(writes, parsed.Grants, parsed.OptedOutTables);
        }

        internal RuntimeWrite[] WithInferredFixture(string role, string table)
        {
            var root = FindRepositoryRoot();
            var fixturePath = Path.Combine(
                root,
                "tests",
                "Pegasus.ArchitectureTests",
                "Fixtures",
                "RuntimeGrant",
                "ForwardUnGrantedTable.fixture");
            var source = ReadHashedFixture(fixturePath);
            var modelBuilder = new ModelBuilder();
            modelBuilder.Entity<ArchitectureTestUnGrantedTableEntity>().ToTable(table);
            var entityType = modelBuilder.FinalizeModel()
                .FindEntityType(typeof(ArchitectureTestUnGrantedTableEntity))
                ?? throw new InvalidOperationException("Forward fixture entity was not added to its model.");
            var tableNames = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [nameof(ArchitectureTestUnGrantedTableEntity)] = entityType.GetTableName()
                    ?? throw new InvalidOperationException("Forward fixture entity has no table mapping.")
            };
            var contextMembers = new Dictionary<string, string>(StringComparer.Ordinal);
            var stores = RuntimeGrantCompositionAnalyzer.DiscoverFixtureStores(source, fixturePath)
                .Select(store => new RuntimeStoreUsage(
                    store.ImplementationType,
                    role,
                    RuntimeGrantCompositionAnalyzer.AllMethods(source, store.ImplementationType)))
                .ToArray();
            var fixtureWrites = DiscoverWrites(
                new[] { fixturePath },
                stores,
                tableNames,
                contextMembers,
                sourceOverride: source);
            return Writes.Concat(fixtureWrites).ToArray();
        }

        internal static RuntimeWrite[] InferHistoricalStoreWrites(string root, string table)
        {
            var fixturePath = Path.Combine(
                root,
                "tests",
                "Pegasus.ArchitectureTests",
                "Fixtures",
                "RuntimeGrant",
                table switch
                {
                    "Cases" => "20260814092852_AddWorkerCaseCreationGrants.fixture",
                    "VehicleLookupRequests" => "20260821095500_GrantWorkerVehicleLookupRequests.fixture",
                    "CaseDocuments" => "20260822044425_GrantWorkerCaseDocuments.fixture",
                    _ => throw new ArgumentOutOfRangeException(nameof(table), table, null)
                });
            var source = ReadHashedFixture(fixturePath);
            var contextSource = File.ReadAllText(Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence", "PegasusDbContext.cs"));
            var stores = RuntimeGrantCompositionAnalyzer.DiscoverFixtureStores(source, fixturePath)
                .Select(store => new RuntimeStoreUsage(
                    store.ImplementationType,
                    "Worker",
                    RuntimeGrantCompositionAnalyzer.AllMethods(source, store.ImplementationType)))
                .ToArray();
            return DiscoverWrites(
                new[] { fixturePath },
                stores,
                BuildTableNames(contextSource),
                BuildContextMembers(contextSource),
                sourceOverride: source).ToArray();
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
                .Select(item => $"{item.write.Role} {item.write.Table} requires {item.verb} ({item.write.Source})")
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        private static List<RuntimeWrite> DiscoverWrites(
            IEnumerable<string> paths,
            IReadOnlyList<RuntimeStoreUsage> storeUsages,
            IReadOnlyDictionary<string, string> tableNames,
            IReadOnlyDictionary<string, string> contextMembers,
            string? sourceOverride = null)
        {
            var writes = new List<RuntimeWrite>();
            foreach (var path in paths)
            {
                var source = sourceOverride ?? File.ReadAllText(path);
                var store = storeUsages.Select(usage => usage.Store).Distinct(StringComparer.Ordinal).FirstOrDefault(name => Regex.IsMatch(
                    source,
                    $@"\bclass\s+{Regex.Escape(name)}\b",
                    RegexOptions.CultureInvariant));
                var usages = store is null
                    ? []
                    : storeUsages.Where(usage => usage.Store == store).ToArray();
                if (store is null || usages.Length == 0)
                {
                    continue;
                }

                var syntaxWrites = RuntimeGrantSyntaxEvaluator.Collect(source, contextMembers, store);
                foreach (var syntaxWrite in syntaxWrites)
                {
                    var table = ResolveTable(syntaxWrite.Target, tableNames);
                    if (table is null)
                    {
                        throw new InvalidOperationException(
                            $"Could not map syntax target '{syntaxWrite.Target}' to an EF table in {path}.");
                    }

                    foreach (var usage in usages)
                    {
                        var reachable = RuntimeGrantCompositionAnalyzer.ReachableMethods(
                            [source],
                            usage.EntryMethods,
                            store);
                        if (reachable.Contains(syntaxWrite.Method))
                        {
                            writes.Add(new(
                                usage.Role,
                                table,
                                new HashSet<string>([syntaxWrite.Verb], StringComparer.Ordinal),
                                path));
                        }
                    }
                }
            }

            return writes;
        }

        private static string? ResolveTable(string target, IReadOnlyDictionary<string, string> tableNames) =>
            tableNames.TryGetValue(target, out var table)
                ? table
                : tableNames.Values.Contains(target, StringComparer.Ordinal)
                    ? target
                    : null;

        private static Dictionary<string, string> BuildTableNames(string contextSource)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            using var context = new PegasusDbContext(new DbContextOptionsBuilder<PegasusDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ArchitectureModelOnly;")
                .Options);
            foreach (var entity in context.Model.GetEntityTypes())
            {
                if (entity.GetTableName() is { } table)
                {
                    names[entity.ClrType.Name] = table;
                }
            }

            foreach (Match match in DbSetProperty.Matches(contextSource))
            {
                var entity = match.Groups["entity"].Value.Split('.').Last();
                if (names.TryGetValue(entity, out var table))
                {
                    names[match.Groups["property"].Value] = table;
                }
            }

            return names;
        }

        private static Dictionary<string, string> BuildContextMembers(string contextSource) =>
            DbSetProperty.Matches(contextSource)
                .Cast<Match>()
                .ToDictionary(
                    match => match.Groups["property"].Value,
                    match => match.Groups["entity"].Value.Split('.').Last(),
                    StringComparer.Ordinal);

        private static (HashSet<RuntimeGrant> Grants, HashSet<string> OptedOutTables) ParseGrants(
            string migrationRoot,
            string? beforeMigration)
        {
            var grants = new HashSet<RuntimeGrant>();
            var optedOutTables = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in Directory.EnumerateFiles(migrationRoot, "*.cs"))
            {
                var fileName = Path.GetFileName(path);
                if (fileName.EndsWith(".Designer.cs", StringComparison.Ordinal) ||
                    fileName.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal) ||
                    (beforeMigration is not null && string.CompareOrdinal(fileName, beforeMigration) >= 0))
                {
                    continue;
                }

                var parsed = ParseGrantContent(File.ReadAllText(path), fileName, beforeMigration: null);
                grants.UnionWith(parsed.Grants);
                optedOutTables.UnionWith(parsed.OptedOutTables);
            }

            return (grants, optedOutTables);
        }

        private static (HashSet<RuntimeGrant> Grants, HashSet<string> OptedOutTables) ParseGrantContent(
            string source,
            string sourceFile,
            string? beforeMigration)
        {
            var grants = new HashSet<RuntimeGrant>();
            var optedOutTables = new HashSet<string>(StringComparer.Ordinal);
            var roleVariables = RoleVariable.Matches(source).Cast<Match>()
                .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["role"].Value,
                    StringComparer.Ordinal);
            var arrayRoles = GrantCall.Matches(source).Cast<Match>()
                .GroupBy(match => match.Groups["array"].Value, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(match => match.Groups["role"].Value == "WorkerRole" ? "Worker" : "Web")
                        .ToHashSet(StringComparer.Ordinal),
                    StringComparer.Ordinal);
            var arrays = GrantArray.Matches(source).Cast<Match>().ToArray();
            var up = source;
            var upStart = source.IndexOf("void Up(", StringComparison.Ordinal);
            var downStart = source.IndexOf("void Down(", StringComparison.Ordinal);
            if (upStart >= 0)
            {
                up = source.Substring(upStart, downStart > upStart ? downStart - upStart : source.Length - upStart);
            }

            foreach (Match marker in Regex.Matches(
                         source,
                         @"//\s*no-runtime-grant:\s*(?<table>[A-Za-z0-9_]+)\b(?<reason>.*)$",
                         RegexOptions.CultureInvariant | RegexOptions.Multiline))
            {
                var table = marker.Groups["table"].Value;
                if (IsValidOptOutMarker(marker.Value, Regex.IsMatch(
                        up,
                        $@"CreateTable\s*\(\s*(?:name\s*:\s*)?['""]{Regex.Escape(table)}['""]",
                        RegexOptions.CultureInvariant)))
                {
                    optedOutTables.Add(table);
                }
            }

            foreach (Match match in GrantStatement.Matches(source))
            {
                var table = GrantTargetTable(match.Value);
                var role = ResolveGrantRole(match.Value, roleVariables);
                if (table.Length == 0 || role.Length == 0)
                {
                    continue;
                }

                foreach (var permission in Permissions(match.Value))
                {
                    grants.Add(new(RoleName(role), table, permission, sourceFile));
                }
            }

            foreach (Match tuple in TupleGrant.Matches(source))
            {
                var array = arrays.FirstOrDefault(candidate =>
                    tuple.Index >= candidate.Groups["body"].Index &&
                    tuple.Index + tuple.Length <= candidate.Groups["body"].Index + candidate.Groups["body"].Length);
                var roles = array is not null && arrayRoles.TryGetValue(array.Groups["name"].Value, out var mappedRoles)
                    ? mappedRoles
                    : array?.Groups["name"].Value.Contains("Worker", StringComparison.Ordinal) == true
                        ? new HashSet<string>(["Worker"], StringComparer.Ordinal)
                        : array?.Groups["name"].Value.Contains("Web", StringComparison.Ordinal) == true
                            ? new HashSet<string>(["Web"], StringComparer.Ordinal)
                            : source.Contains("foreach (var role in RuntimeRoles)", StringComparison.Ordinal)
                                ? HostRoles
                                : HostRoles;
                foreach (var role in roles)
                {
                    foreach (var permission in Permissions(tuple.Groups["permissions"].Value))
                    {
                        grants.Add(new(role, tuple.Groups["table"].Value, permission, sourceFile));
                    }
                }
            }

            return (grants, optedOutTables);
        }

        private static string ResolveGrantRole(string statement, Dictionary<string, string> roleVariables)
        {
            if (GrantRole.Match(statement) is { Success: true } direct)
            {
                var role = direct.Groups["role"].Value;
                if (role.Contains("runtime_role", StringComparison.OrdinalIgnoreCase))
                {
                    return role;
                }
            }

            var variable = InterpolatedRole.Match(statement);
            if (variable.Success && roleVariables.TryGetValue(variable.Groups["name"].Value, out var resolved))
            {
                return resolved;
            }

            return statement.Contains("{WorkerRole}", StringComparison.Ordinal) ? "pegasus_worker_runtime_role" :
                statement.Contains("{WebRole}", StringComparison.Ordinal) ? "pegasus_web_runtime_role" : string.Empty;
        }

        private static string[] Permissions(string value)
        {
            var match = Regex.Match(
                value,
                @"GRANT\s+(?<permissions>(?:SELECT|INSERT|UPDATE|DELETE)(?:\s*,\s*(?:SELECT|INSERT|UPDATE|DELETE))*)\s+ON\b",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            var permissions = (match.Success ? match.Groups["permissions"].Value : value)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(permission => permission.ToUpperInvariant())
                .Where(permission => permission is "SELECT" or "INSERT" or "UPDATE" or "DELETE")
                .ToArray();
            return permissions;
        }

        private static string RoleName(string role) =>
            role.Contains("worker", StringComparison.OrdinalIgnoreCase) ? "Worker" : "Web";

        private static string GrantTargetTable(string statement)
        {
            var toMatch = Regex.Match(
                statement,
                @"\bTO\b",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!toMatch.Success)
            {
                return string.Empty;
            }

            return BracketedIdentifier.Matches(statement[..toMatch.Index])
                .Cast<Match>()
                .Select(match => match.Groups["name"].Value)
                .LastOrDefault() ?? string.Empty;
        }

        private static string ReadHashedFixture(string path)
        {
            var expected = File.ReadAllText(path + ".sha256").Trim();
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            Assert.Equal(expected, actual);
            return File.ReadAllText(path);
        }
    }

    private sealed class ArchitectureTestUnGrantedTableEntity
    {
        public int Id { get; set; }
    }
}
