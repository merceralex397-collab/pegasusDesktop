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
            .Where(grant => !(grant.Table == table && grant.Verb == verb))
            .ToHashSet();

        var writesBeforeFix = catalogue.Writes
            .Concat(RuntimeGrantCatalogue.InferHistoricalStoreWrites(root, table))
            .ToArray();
        var failures = catalogue.FindMissingGrants(
            grantsBeforeFix,
            writesBeforeFix,
            honorOptOuts: false);

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
        var withoutFixtureGrant = catalogue.WithInferredFixture(
            "Worker",
            "context.Set<ArchitectureTestUnGrantedTableEntity>().Add(new());",
            fixtureTable);
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

        var writes = catalogue.WithInferredFixture(
            "Worker",
            "context.Set<CaseDocumentEntity>().Add(new());",
            "CaseDocuments");
        var grantsWithoutTable = catalogue.Grants
            .Where(grant => !grant.Table.Equals("CaseDocuments", StringComparison.Ordinal))
            .ToHashSet();

        Assert.DoesNotContain(
            catalogue.FindMissingGrants(grantsWithoutTable, writes),
            failure => failure.Contains("CaseDocuments", StringComparison.Ordinal));
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
            @"\b(?:context|dbContext|verification|finalContext)\.(?:(?<property>[A-Z][A-Za-z0-9_]*)|Set<(?<entity>[A-Za-z_][A-Za-z0-9_]*)>\(\))\s*\.\s*(?<verb>Add|AddAsync|Remove|ExecuteDelete|ExecuteUpdate)\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantStatement = new(
            @"GRANT[^;]*;",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantTable = new(
            @"\]\s*\.\s*\[(?<table>[A-Za-z0-9_]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantRole = new(
            @"\bTO\s+\[(?<role>[^]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TupleGrant = new(
            @"\(\s*""(?<table>[A-Za-z0-9_]+)""\s*,\s*""(?<permissions>[A-Z, ]+)""\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex GrantArray = new(
            @"(?<name>[A-Za-z]*Grants)\s*=\s*\[(?<body>.*?)\];",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

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

            var parsed = ParseGrants(migrationRoot);
            return new(writes, parsed.Grants, parsed.OptedOutTables);
        }

        internal RuntimeWrite[] WithInferredFixture(
            string role,
            string source,
            string table)
        {
            var entity = ContextSet.Match(source).Groups["entity"].Value;
            if (string.IsNullOrEmpty(entity))
            {
                throw new InvalidOperationException("Fixture source did not contain a context entity set.");
            }

            var verbs = InferVerbs(source, entity);
            var fixture = new RuntimeWrite(role, table, verbs, "fixture: inferred ungranted table");
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
            var sourcePath = Path.Combine(
                root,
                "src",
                "Pegasus.Infrastructure",
                "Persistence",
                storeName + ".cs");
            var source = File.ReadAllText(sourcePath);
            var tableNames = BuildTableNames(
                File.ReadAllText(Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence", "PegasusDbContext.cs")),
                Path.Combine(root, "src", "Pegasus.Infrastructure", "Persistence"));
            var inferred = new List<RuntimeWrite>();
            foreach (var entity in ContextSet.Matches(source)
                         .Select(match => match.Groups["entity"].Value)
                         .Where(tableNames.ContainsKey)
                         .Distinct(StringComparer.Ordinal))
            {
                var verbs = InferVerbs(source, entity);
                if (verbs.Count > 0 && tableNames[entity].Equals(table, StringComparison.Ordinal))
                {
                    inferred.Add(new("Worker", table, verbs, sourcePath));
                }
            }
            foreach (var property in ContextProperty.Matches(source)
                         .Select(match => match.Groups["property"].Value)
                         .Where(tableNames.ContainsKey)
                         .Distinct(StringComparer.Ordinal))
            {
                var verbs = InferVerbs(source, property);
                if (verbs.Count > 0 && tableNames[property].Equals(table, StringComparison.Ordinal))
                {
                    inferred.Add(new("Worker", table, verbs, sourcePath));
                }
            }

            return inferred.ToArray();
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
                foreach (Match match in Regex.Matches(
                             source,
                             @"(?:modelBuilder|builder)\.Entity<(?<entity>[A-Za-z_][A-Za-z0-9_]*)>[\s\S]{0,500}?\.ToTable\(""(?<table>[A-Za-z0-9_]+)""",
                             RegexOptions.CultureInvariant))
                {
                    var table = match.Groups["table"].Value;
                    names[table] = table;
                    names[match.Groups["entity"].Value] = table;
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
                }
            }
        }

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
            if (Regex.IsMatch(
                    source,
                    $@"Set<{Regex.Escape(property)}>\(\)[\s\S]{{0,2000}}?\bcontext\.Add\s*\(",
                    RegexOptions.CultureInvariant))
            {
                verbs.Add("INSERT");
            }
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

        private static void AddVerb(HashSet<string> verbs, string verb)
        {
            verbs.Add(verb switch
            {
                "Add" or "AddAsync" => "INSERT",
                "Remove" or "ExecuteDelete" => "DELETE",
                "ExecuteUpdate" => "UPDATE",
                _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null)
            });
        }

        private static (HashSet<RuntimeGrant> Grants, HashSet<string> OptedOutTables) ParseGrants(string migrationRoot)
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

                var source = File.ReadAllText(path);
                var upStart = source.IndexOf("void Up(", StringComparison.Ordinal);
                var downStart = source.IndexOf("void Down(", StringComparison.Ordinal);
                var up = upStart >= 0
                    ? source.Substring(upStart, downStart > upStart ? downStart - upStart : source.Length - upStart)
                    : string.Empty;
                foreach (Match marker in Regex.Matches(
                             source,
                             @"//\s*no-runtime-grant:\s*(?<table>[A-Za-z0-9_]+)\b",
                             RegexOptions.CultureInvariant))
                {
                    var table = marker.Groups["table"].Value;
                    if (Regex.IsMatch(
                            up,
                            $@"CreateTable\s*\(\s*(?:name\s*:\s*)?""{Regex.Escape(table)}""",
                            RegexOptions.CultureInvariant))
                    {
                        optedOutTables.Add(table);
                    }
                }
                foreach (Match match in GrantStatement.Matches(source))
                {
                    var table = GrantTable.Match(match.Value).Groups["table"].Value;
                    var role = GrantRole.Match(match.Value).Groups["role"].Value;
                    if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(role))
                    {
                        continue;
                    }

                    foreach (var permission in Permissions(match.Value))
                    {
                        grants.Add(new(RoleName(role), table, permission, fileName));
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
    }
}
