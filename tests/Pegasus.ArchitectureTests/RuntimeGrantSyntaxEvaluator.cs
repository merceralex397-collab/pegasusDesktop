using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pegasus.ArchitectureTests;

internal readonly record struct RuntimeSyntaxWrite(string Target, string Verb, string Method);

internal static class RuntimeGrantSyntaxEvaluator
{
    private static readonly HashSet<string> ContextNames =
    [
        "context",
        "dbContext",
        "verification",
        "finalContext",
        "finalDbContext"
    ];

    private static readonly Regex SqlTarget = new(
        @"\b(?<verb>INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(?:(?:\[[^]]+\]|[A-Za-z_][A-Za-z0-9_]*)\.)?\[?(?<table>[A-Za-z_][A-Za-z0-9_]*)\]?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static IReadOnlyList<RuntimeSyntaxWrite> Collect(
        string source,
        IReadOnlyDictionary<string, string> contextMembers,
        string? containingType = null)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var syntaxNodes = root.DescendantNodes()
            .Where(node => containingType is null ||
                node.Ancestors().OfType<TypeDeclarationSyntax>()
                    .Any(type => type.Identifier.ValueText == containingType))
            .ToArray();
        var navigationMembers = NavigationMembers(root);
        var variableTypes = VariableTypes(root, contextMembers, navigationMembers);
        var stringValues = StringValues(root);
        var writes = new List<RuntimeSyntaxWrite>();

        foreach (var invocation in syntaxNodes.OfType<InvocationExpressionSyntax>())
        {
            var member = invocation.Expression as MemberAccessExpressionSyntax;
            var name = member?.Name.Identifier.ValueText;
            var method = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>()?.Identifier.ValueText
                ?? "<top-level>";
            if (name is not
                ("Add" or "AddAsync" or "AddRange" or "AddRangeAsync" or
                "Remove" or "RemoveRange" or "ExecuteDelete" or "ExecuteDeleteAsync" or
                "ExecuteUpdate" or "ExecuteUpdateAsync"))
            {
                if (name is not null && name.StartsWith("ExecuteSql", StringComparison.Ordinal))
                {
                    foreach (var argument in invocation.ArgumentList.Arguments)
                    {
                        var text = ConstantText(argument.Expression, stringValues);
                        foreach (Match rawTarget in SqlTarget.Matches(text))
                        {
                            writes.Add(new(
                                rawTarget.Groups["table"].Value,
                                rawTarget.Groups["verb"].Value.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                                    ? "INSERT"
                                    : rawTarget.Groups["verb"].Value.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
                                        ? "DELETE"
                                        : "UPDATE",
                                method));
                        }
                    }
                }

                continue;
            }

            var verb = name switch
            {
                "Add" or "AddAsync" or "AddRange" or "AddRangeAsync" => "INSERT",
                "Remove" or "RemoveRange" or "ExecuteDelete" or "ExecuteDeleteAsync" => "DELETE",
                "ExecuteUpdate" or "ExecuteUpdateAsync" => "UPDATE",
                _ => throw new InvalidOperationException($"Unsupported write method '{name}'.")
            };

            var mutationTarget = member is null
                ? null
                : ResolveMutationTarget(member, invocation.ArgumentList.Arguments, variableTypes, contextMembers,
                    navigationMembers);
            if (mutationTarget is not null)
            {
                writes.Add(new(mutationTarget, verb, method));
            }
        }

        foreach (var assignment in syntaxNodes.OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            if (member.Name.Identifier.ValueText == "State" &&
                member.Expression is InvocationExpressionSyntax entry &&
                entry.Expression is MemberAccessExpressionSyntax entryMember &&
                entryMember.Name.Identifier.ValueText == "Entry")
            {
                var entryTarget = entry.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                var target = entryTarget is null
                    ? null
                    : ResolveEntity(entryTarget, variableTypes, contextMembers, navigationMembers);
                if (target is not null)
                {
                    writes.Add(new(
                        target,
                        "UPDATE",
                        assignment.FirstAncestorOrSelf<MethodDeclarationSyntax>()?.Identifier.ValueText
                            ?? "<top-level>"));
                }

                continue;
            }

            var entity = ResolveEntity(member.Expression, variableTypes, contextMembers, navigationMembers);
            if (entity is not null && entity.EndsWith("Entity", StringComparison.Ordinal))
            {
                writes.Add(new(
                    entity,
                    "UPDATE",
                    assignment.FirstAncestorOrSelf<MethodDeclarationSyntax>()?.Identifier.ValueText
                        ?? "<top-level>"));
            }
        }

        return writes
            .Distinct()
            .ToArray();
    }

    private static string? ResolveMutationTarget(
        MemberAccessExpressionSyntax member,
        IReadOnlyList<ArgumentSyntax> arguments,
        IReadOnlyDictionary<string, string> variableTypes,
        IReadOnlyDictionary<string, string> contextMembers,
        IReadOnlyDictionary<(string Owner, string Member), string> navigationMembers)
    {
        var receiver = member.Expression;
        var receiverTarget = ResolveEntity(receiver, variableTypes, contextMembers, navigationMembers);
        if (receiverTarget is not null && IsEntityName(receiverTarget))
        {
            return receiverTarget;
        }

        if (IsContext(receiver))
        {
            return arguments
                .Select(argument => ResolveEntity(argument.Expression, variableTypes, contextMembers, navigationMembers))
                .FirstOrDefault(IsEntityName);
        }

        return arguments
            .Select(argument => ResolveEntity(argument.Expression, variableTypes, contextMembers, navigationMembers))
            .FirstOrDefault(IsEntityName);
    }

    private static string? ResolveEntity(
        ExpressionSyntax expression,
        IReadOnlyDictionary<string, string> variableTypes,
        IReadOnlyDictionary<string, string> contextMembers,
        IReadOnlyDictionary<(string Owner, string Member), string> navigationMembers)
    {
        expression = Unwrap(expression);

        if (expression is ObjectCreationExpressionSyntax creation)
        {
            return TypeName(creation.Type);
        }

        if (expression is ImplicitObjectCreationExpressionSyntax)
        {
            return null;
        }

        if (expression is IdentifierNameSyntax identifier &&
            variableTypes.TryGetValue(identifier.Identifier.ValueText, out var variableType) &&
            (variableType.EndsWith("Entity", StringComparison.Ordinal) ||
             contextMembers.Values.Contains(variableType, StringComparer.Ordinal)))
        {
            return variableType;
        }

        if (expression is MemberAccessExpressionSyntax member)
        {
            if (IsContext(member.Expression) && contextMembers.TryGetValue(member.Name.Identifier.ValueText, out var contextType))
            {
                return contextType;
            }

            var owner = ResolveEntity(member.Expression, variableTypes, contextMembers, navigationMembers);
            if (owner is not null && navigationMembers.TryGetValue((owner, member.Name.Identifier.ValueText), out var navigationType))
            {
                return navigationType;
            }

            return ResolveEntity(member.Expression, variableTypes, contextMembers, navigationMembers);
        }

        if (expression is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is GenericNameSyntax generic &&
                generic.Identifier.ValueText == "Set" &&
                generic.TypeArgumentList.Arguments.Count == 1)
            {
                return TypeName(generic.TypeArgumentList.Arguments[0]);
            }

            if (invocation.Expression is MemberAccessExpressionSyntax invocationMember)
            {
                if (invocationMember.Name is GenericNameSyntax setName &&
                    setName.Identifier.ValueText == "Set" &&
                    setName.TypeArgumentList.Arguments.Count == 1)
                {
                    return TypeName(setName.TypeArgumentList.Arguments[0]);
                }

                if (invocationMember.Name.Identifier.ValueText == "Entry")
                {
                    return invocation.ArgumentList.Arguments.FirstOrDefault() is { } argument
                        ? ResolveEntity(argument.Expression, variableTypes, contextMembers, navigationMembers)
                        : null;
                }

                return ResolveEntity(invocationMember.Expression, variableTypes, contextMembers, navigationMembers);
            }
        }

        if (expression is ArrayCreationExpressionSyntax array && array.Initializer is not null)
        {
            return array.Initializer.Expressions
                .Select(value => ResolveEntity(value, variableTypes, contextMembers, navigationMembers))
                .FirstOrDefault(value => value is not null);
        }

        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            return implicitArray.Initializer.Expressions
                .Select(value => ResolveEntity(value, variableTypes, contextMembers, navigationMembers))
                .FirstOrDefault(value => value is not null);
        }

        return null;
    }

    private static Dictionary<string, string> VariableTypes(
        CompilationUnitSyntax root,
        IReadOnlyDictionary<string, string> contextMembers,
        IReadOnlyDictionary<(string Owner, string Member), string> navigationMembers)
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
        {
            if (parameter.Type is not null)
            {
                variables[parameter.Identifier.ValueText] = TypeName(parameter.Type);
            }
        }

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var type = TypeName(field.Declaration.Type);
            foreach (var variable in field.Declaration.Variables)
            {
                variables[variable.Identifier.ValueText] = type;
            }
        }

        foreach (var declaration in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            foreach (var variable in declaration.Declaration.Variables)
            {
                var type = TypeName(declaration.Declaration.Type);
                if (!string.Equals(type, "var", StringComparison.Ordinal))
                {
                    variables[variable.Identifier.ValueText] = type;
                }
            }
        }

        for (var pass = 0; pass < 3; pass++)
        {
            foreach (var declaration in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
            {
                if (!string.Equals(TypeName(declaration.Declaration.Type), "var", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var variable in declaration.Declaration.Variables)
                {
                    if (variable.Initializer is null || IsCollectionQuery(variable.Initializer.Value))
                    {
                        continue;
                    }

                    var type = ResolveEntity(variable.Initializer.Value, variables, contextMembers, navigationMembers);
                    if (type is not null)
                    {
                        variables[variable.Identifier.ValueText] = type;
                    }
                }
            }
        }

        return variables;
    }

    private static bool IsCollectionQuery(ExpressionSyntax expression) =>
        expression.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText)
            .Any(name => name is "ToList" or "ToListAsync" or "ToArray" or "ToArrayAsync" or
                "ToHashSet" or "ToHashSetAsync");

    private static Dictionary<(string Owner, string Member), string> NavigationMembers(CompilationUnitSyntax root)
    {
        var result = new Dictionary<(string Owner, string Member), string>();
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var entity = CollectionElement(property.Type);
            if (entity is null || property.Parent is not TypeDeclarationSyntax owner)
            {
                continue;
            }

            result[(owner.Identifier.ValueText, property.Identifier.ValueText)] = entity;
        }

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var entity = CollectionElement(field.Declaration.Type);
            if (entity is null || field.Parent is not TypeDeclarationSyntax owner)
            {
                continue;
            }

            foreach (var variable in field.Declaration.Variables)
            {
                result[(owner.Identifier.ValueText, variable.Identifier.ValueText)] = entity;
            }
        }

        return result;
    }

    private static Dictionary<string, string> StringValues(CompilationUnitSyntax root)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var declaration in root.DescendantNodes().OfType<VariableDeclarationSyntax>())
        {
            foreach (var variable in declaration.Variables)
            {
                if (variable.Initializer is not null)
                {
                    var text = ConstantText(variable.Initializer.Value, values);
                    if (text.Length > 0)
                    {
                        values[variable.Identifier.ValueText] = text;
                    }
                }
            }
        }

        return values;
    }

    private static string ConstantText(ExpressionSyntax expression, Dictionary<string, string> values)
    {
        expression = Unwrap(expression);
        return expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
            InterpolatedStringExpressionSyntax interpolated => interpolated.ToString(),
            IdentifierNameSyntax identifier when values.TryGetValue(identifier.Identifier.ValueText, out var value) => value,
            _ => string.Empty
        };
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        if (expression is AwaitExpressionSyntax awaited)
        {
            return Unwrap(awaited.Expression);
        }

        return expression;
    }

    private static bool IsContext(ExpressionSyntax expression) =>
        expression is IdentifierNameSyntax identifier && ContextNames.Contains(identifier.Identifier.ValueText);

    private static bool IsEntityName(string? value) =>
        value is not null && value.EndsWith("Entity", StringComparison.Ordinal);

    private static string TypeName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => TypeName(qualified.Right),
        NullableTypeSyntax nullable => TypeName(nullable.ElementType),
        PredefinedTypeSyntax predefined => predefined.Keyword.ValueText,
        _ => type.ToString().Split('.').Last()
    };

    private static string? CollectionElement(TypeSyntax type)
    {
        if (type is not GenericNameSyntax generic ||
            generic.Identifier.ValueText is not
                ("ICollection" or "IList" or "ISet" or "IEnumerable" or "IReadOnlyCollection" or
                "IReadOnlyList" or "List" or "HashSet"))
        {
            return null;
        }

        return generic.TypeArgumentList.Arguments.Count == 1
            ? TypeName(generic.TypeArgumentList.Arguments[0])
            : null;
    }
}

internal sealed record RuntimeStoreRegistration(string ServiceType, string ImplementationType, string Source);

internal sealed record RuntimeStoreUsage(string Store, string Role, IReadOnlySet<string> EntryMethods);

internal sealed record RuntimeServiceCall(string Service, string Method);

internal static class RuntimeGrantCompositionAnalyzer
{
    private static readonly HashSet<string> RegistrationMethods =
    [
        "AddScoped",
        "AddSingleton",
        "AddTransient",
        "TryAddScoped",
        "TryAddSingleton",
        "TryAddTransient"
    ];

    private static readonly HashSet<string> StoreNames = new(StringComparer.Ordinal)
    {
        "EvaHandoffStore"
    };

    internal static IReadOnlyList<RuntimeStoreUsage> DiscoverStoreUsages(string root)
    {
        var documents = Documents(root);
        var registrations = documents
            .SelectMany(document => ParseRegistrations(document.Source, document.Path))
            .ToArray();
        var byService = registrations
            .GroupBy(registration => registration.ServiceType, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var declarations = documents
            .SelectMany(document => document.Root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Select(type => (Name: type.Identifier.ValueText, Document: document)))
            .GroupBy(value => value.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(value => value.Document).ToArray(), StringComparer.Ordinal);

        var result = new Dictionary<(string Store, string Role), HashSet<string>>();
        foreach (var role in new[] { "Web", "Worker" })
        {
            var queue = new Queue<(string Service, string Method)>();
            var visited = new HashSet<(string Service, string Method)>();
            foreach (var document in documents.Where(document => IsHostDocument(document.Path, role)))
            {
                foreach (var call in ServiceCalls(document.Root, byService, type: null))
                {
                    queue.Enqueue((call.Service, call.Method));
                }
            }

            while (queue.Count > 0)
            {
                var use = queue.Dequeue();
                if (!visited.Add(use) || !byService.TryGetValue(use.Service, out var serviceRegistrations))
                {
                    continue;
                }

                foreach (var registration in serviceRegistrations)
                {
                    if (IsStore(registration.ImplementationType))
                    {
                        AddUse(result, registration.ImplementationType, role, use.Method);
                    }

                    if (!declarations.TryGetValue(registration.ImplementationType, out var implementationDocuments))
                    {
                        continue;
                    }

                    var reachableMethods = ReachableMethods(
                        implementationDocuments.Select(document => document.Root),
                        [use.Method],
                        registration.ImplementationType);
                    foreach (var implementationDocument in implementationDocuments)
                    {
                        foreach (var call in ServiceCalls(
                                     implementationDocument.Root,
                                     byService,
                                     registration.ImplementationType,
                                     reachableMethods))
                        {
                            queue.Enqueue((call.Service, call.Method));
                        }
                    }
                }
            }
        }

        return result
            .Select(value => new RuntimeStoreUsage(
                value.Key.Store,
                value.Key.Role,
                value.Value))
            .OrderBy(value => value.Store, StringComparer.Ordinal)
            .ThenBy(value => value.Role, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyList<RuntimeStoreRegistration> DiscoverFixtureStores(string source, string sourcePath)
    {
        return ParseRegistrations(source, sourcePath)
            .Where(registration => IsStore(registration.ImplementationType))
            .GroupBy(registration => registration.ImplementationType, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    internal static IReadOnlySet<string> ReachableMethods(
        IEnumerable<SyntaxNode> roots,
        IEnumerable<string> entryMethods,
        string typeName)
    {
        var methods = roots
            .SelectMany(root => root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(method => method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() is
                { Identifier.ValueText: var owner } && owner == typeName)
            .GroupBy(method => method.Identifier.ValueText, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(entryMethods);
        while (queue.Count > 0)
        {
            var methodName = queue.Dequeue();
            if (!reachable.Add(methodName))
            {
                continue;
            }

            if (!methods.TryGetValue(methodName, out var declarationsForMethod))
            {
                continue;
            }

            foreach (var declaration in declarationsForMethod)
            {
                foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is MemberAccessExpressionSyntax access &&
                        methods.ContainsKey(access.Name.Identifier.ValueText))
                    {
                        queue.Enqueue(access.Name.Identifier.ValueText);
                    }
                }
            }
        }

        return reachable;
    }

    internal static IReadOnlySet<string> ReachableMethods(
        IEnumerable<string> sources,
        IEnumerable<string> entryMethods,
        string typeName) =>
        ReachableMethods(
            sources.Select(source => CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()),
            entryMethods,
            typeName);

    internal static IReadOnlySet<string> AllMethods(string source, string typeName) =>
        CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(method => method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() is
                { Identifier.ValueText: var owner } && owner == typeName)
            .Select(method => method.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

    private static List<RuntimeServiceCall> ServiceCalls(
        CompilationUnitSyntax root,
        Dictionary<string, RuntimeStoreRegistration[]> byService,
        string? type,
        IReadOnlySet<string>? methods = null)
    {
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(candidate => type is null || candidate.Identifier.ValueText == type)
            .ToArray();
        var result = new List<RuntimeServiceCall>();
        foreach (var declaration in types)
        {
            var methodDeclarations = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(method => methods is null || methods.Contains(method.Identifier.ValueText))
                .ToArray();
            var variables = ServiceVariables(declaration, methodDeclarations);
            foreach (var invocation in methodDeclarations.SelectMany(method => method.DescendantNodesAndSelf()
                         .OfType<InvocationExpressionSyntax>()))
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax access)
                {
                    continue;
                }

                var service = ReceiverService(access.Expression, variables);
                var method = access.Name.Identifier.ValueText;
                if (service is not null && byService.ContainsKey(service))
                {
                    result.Add(new(service, method));
                }
            }
        }

        return result;
    }

    private static Dictionary<string, string> ServiceVariables(
        TypeDeclarationSyntax type,
        IReadOnlyList<MethodDeclarationSyntax> methods)
    {
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        if (type.ParameterList is not null)
        {
            foreach (var parameter in type.ParameterList.Parameters)
            {
                AddTypedVariable(variables, parameter.Identifier.ValueText, parameter.Type);
            }
        }

        foreach (var constructor in type.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                AddTypedVariable(variables, parameter.Identifier.ValueText, parameter.Type);
            }
        }

        foreach (var field in type.DescendantNodes().OfType<FieldDeclarationSyntax>()
                     .Where(field => field.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() == type))
        {
            var fieldType = TypeName(field.Declaration.Type);
            foreach (var variable in field.Declaration.Variables)
            {
                variables[variable.Identifier.ValueText] = fieldType;
            }
        }

        foreach (var method in methods)
        {
            foreach (var parameter in method.ParameterList.Parameters)
            {
                AddTypedVariable(variables, parameter.Identifier.ValueText, parameter.Type);
            }

            foreach (var declaration in method.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
            {
                var declaredType = TypeName(declaration.Declaration.Type);
                foreach (var variable in declaration.Declaration.Variables)
                {
                    var resolvedType = string.Equals(declaredType, "var", StringComparison.Ordinal)
                        ? ServiceTypeFromExpression(variable.Initializer?.Value, variables)
                        : declaredType;
                    if (resolvedType is not null)
                    {
                        variables[variable.Identifier.ValueText] = resolvedType;
                    }
                }
            }
        }

        return variables;
    }

    private static void AddTypedVariable(
        Dictionary<string, string> variables,
        string name,
        TypeSyntax? type)
    {
        if (type is not null)
        {
            variables[name] = TypeName(type);
        }
    }

    private static string? ReceiverService(
        ExpressionSyntax expression,
        Dictionary<string, string> variables)
    {
        expression = Unwrap(expression);
        if (expression is IdentifierNameSyntax identifier &&
            variables.TryGetValue(identifier.Identifier.ValueText, out var variableType))
        {
            return variableType;
        }

        if (expression is MemberAccessExpressionSyntax member)
        {
            return ReceiverService(member.Name, variables);
        }

        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax access &&
            access.Name is GenericNameSyntax generic &&
            generic.Identifier.ValueText is "GetRequiredService" or "GetService" &&
            generic.TypeArgumentList.Arguments.Count == 1)
        {
            return TypeName(generic.TypeArgumentList.Arguments[0]);
        }

        return null;
    }

    private static string? ReceiverService(
        SimpleNameSyntax name,
        Dictionary<string, string> variables) =>
        variables.TryGetValue(name.Identifier.ValueText, out var variableType) ? variableType : null;

    private static string? ServiceTypeFromExpression(
        ExpressionSyntax? expression,
        Dictionary<string, string> variables)
    {
        if (expression is null)
        {
            return null;
        }

        expression = Unwrap(expression);
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax access &&
            access.Name is GenericNameSyntax generic &&
            generic.Identifier.ValueText is "GetRequiredService" or "GetService" &&
            generic.TypeArgumentList.Arguments.Count == 1)
        {
            return TypeName(generic.TypeArgumentList.Arguments[0]);
        }

        if (expression is IdentifierNameSyntax identifier &&
            variables.TryGetValue(identifier.Identifier.ValueText, out var type))
        {
            return type;
        }

        if (expression is MemberAccessExpressionSyntax member)
        {
            return ReceiverService(member.Name, variables);
        }

        return null;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression is AwaitExpressionSyntax awaited ? Unwrap(awaited.Expression) : expression;
    }

    private static List<RuntimeStoreRegistration> ParseRegistrations(string source, string sourcePath)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var registrations = new List<RuntimeStoreRegistration>();
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access ||
                access.Name is not GenericNameSyntax generic ||
                !RegistrationMethods.Contains(generic.Identifier.ValueText))
            {
                continue;
            }

            var typeArguments = generic.TypeArgumentList.Arguments
                .Select(TypeName)
                .ToArray();
            if (typeArguments.Length == 0)
            {
                continue;
            }

            var service = typeArguments[0];
            var implementation = typeArguments.Length > 1
                ? typeArguments[1]
                : invocation.ArgumentList.Arguments
                    .SelectMany(argument => argument.Expression.DescendantNodesAndSelf())
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Select(creation => TypeName(creation.Type))
                    .FirstOrDefault()
                    ?? invocation.ArgumentList.Arguments
                        .SelectMany(argument => argument.Expression.DescendantNodesAndSelf())
                        .OfType<GenericNameSyntax>()
                        .Where(name => name.Identifier.ValueText == "GetRequiredService")
                        .Select(name => name.TypeArgumentList.Arguments.FirstOrDefault())
                        .Where(type => type is not null)
                        .Select(type => TypeName(type!))
                        .FirstOrDefault()
                    ?? service;

            registrations.Add(new(service, implementation, sourcePath));
        }

        return registrations;
    }

    private static List<RuntimeSourceDocument> Documents(string root)
    {
        var documents = new List<RuntimeSourceDocument>();
        foreach (var project in new[] { "Pegasus.Core", "Pegasus.Infrastructure", "Pegasus.Web", "Pegasus.Worker" })
        {
            var directory = Path.Combine(root, "src", project);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) &&
                                        !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase)))
            {
                var source = File.ReadAllText(path);
                documents.Add(new(path, source, CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot()));
            }
        }

        return documents;
    }

    private static bool IsHostDocument(string path, string role) =>
        path.Contains($"\\src\\Pegasus.{role}\\", StringComparison.OrdinalIgnoreCase);

    private static bool IsStore(string typeName) =>
        typeName.StartsWith("Ef", StringComparison.Ordinal) ||
        typeName.EndsWith("Store", StringComparison.Ordinal) ||
        StoreNames.Contains(typeName);

    private static void AddUse(
        Dictionary<(string Store, string Role), HashSet<string>> result,
        string store,
        string role,
        string method)
    {
        if (!result.TryGetValue((store, role), out var methods))
        {
            methods = new HashSet<string>(StringComparer.Ordinal);
            result[(store, role)] = methods;
        }

        methods.Add(method);
    }

    private sealed record RuntimeSourceDocument(string Path, string Source, CompilationUnitSyntax Root);

    private static string TypeName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => TypeName(qualified.Right),
        NullableTypeSyntax nullable => TypeName(nullable.ElementType),
        _ => type.ToString().Split('.').Last()
    };
}
