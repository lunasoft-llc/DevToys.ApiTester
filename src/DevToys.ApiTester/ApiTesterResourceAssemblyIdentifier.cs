using System.ComponentModel.Composition;
using DevToys.Api;

namespace DevToys.ApiTester;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(ApiTesterResourceAssemblyIdentifier))]
internal sealed class ApiTesterResourceAssemblyIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync() => ValueTask.FromResult(Array.Empty<FontDefinition>());
}
