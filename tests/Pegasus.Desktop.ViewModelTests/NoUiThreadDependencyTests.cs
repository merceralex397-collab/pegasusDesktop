using System.Reflection;
using Pegasus.Desktop.ViewModels;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class NoUiThreadDependencyTests
{
    [Fact]
    [Trait("Category", "ViewModel")]
    public void PublicViewModelsDoNotReferenceDispatcherOrXamlTypes()
    {
        var violations = typeof(MainPageViewModel).Assembly
            .GetTypes()
            .Where(type => type.IsPublic &&
                           type.Namespace?.StartsWith("Pegasus.Desktop.ViewModels", StringComparison.Ordinal) == true)
            .SelectMany(FindUiTypeReferences)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FindUiTypeReferences(Type viewModel)
    {
        foreach (var constructor in viewModel.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                if (IsForbidden(parameter.ParameterType))
                {
                    yield return $"{viewModel.FullName} constructor parameter {parameter.Name}: {parameter.ParameterType.FullName}";
                }
            }
        }

        foreach (var field in viewModel.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (IsForbidden(field.FieldType))
            {
                yield return $"{viewModel.FullName} field {field.Name}: {field.FieldType.FullName}";
            }
        }

        foreach (var property in viewModel.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (IsForbidden(property.PropertyType))
            {
                yield return $"{viewModel.FullName} property {property.Name}: {property.PropertyType.FullName}";
            }
        }
    }

    private static bool IsForbidden(Type type)
    {
        var fullName = type.FullName ?? string.Empty;
        return fullName == "Microsoft.UI.Dispatching.DispatcherQueue" ||
               fullName.StartsWith("Microsoft.UI.Xaml.", StringComparison.Ordinal) ||
               fullName == "Microsoft.UI.Xaml";
    }
}
