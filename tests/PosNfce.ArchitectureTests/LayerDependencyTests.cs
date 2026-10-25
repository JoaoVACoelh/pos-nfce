using System.Xml.Linq;

namespace PosNfce.ArchitectureTests;

/// <summary>
/// Enforces the dependency rules from CLAUDE.md by reading the .csproj files.
/// The csproj is checked (not the compiled assembly) because the compiler drops references that aren't used yet,
/// and a forbidden reference should fail even before any code uses it.
/// </summary>
public class LayerDependencyTests
{
    private static readonly string[] Modules = ["Catalog", "Checkout", "Fiscal", "Reports"];

    public static TheoryData<string> AllModules() => new(Modules);

    [Fact]
    public void SharedKernel_depends_on_nothing()
    {
        var project = Project.Load("PosNfce.SharedKernel");

        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.PackageReferences);
    }

    [Theory]
    [MemberData(nameof(AllModules))]
    public void Domain_depends_only_on_the_shared_kernel(string module)
    {
        var project = Project.Load($"PosNfce.{module}.Domain");

        Assert.Equal(["PosNfce.SharedKernel"], project.ProjectReferences);
        Assert.Empty(project.PackageReferences);
    }

    [Theory]
    [MemberData(nameof(AllModules))]
    public void Application_depends_on_its_domain_and_other_modules_use_cases_only(string module)
    {
        var project = Project.Load($"PosNfce.{module}.Application");

        Assert.Contains($"PosNfce.{module}.Domain", project.ProjectReferences);
        Assert.All(project.ProjectReferences, reference => Assert.True(
            reference == $"PosNfce.{module}.Domain" || IsOtherModuleApplication(reference, module),
            $"PosNfce.{module}.Application must not reference {reference}"));
        Assert.Empty(project.PackageReferences);
    }

    [Theory]
    [MemberData(nameof(AllModules))]
    public void Adapters_never_reach_into_another_modules_domain_or_adapters(string module)
    {
        var project = Project.Load($"PosNfce.{module}.Adapters");

        Assert.All(project.ProjectReferences, reference => Assert.True(
            reference.StartsWith($"PosNfce.{module}.", StringComparison.Ordinal)
                || reference == "PosNfce.SharedKernel"
                || IsOtherModuleApplication(reference, module),
            $"PosNfce.{module}.Adapters must not reference {reference}"));
    }

    private static bool IsOtherModuleApplication(string reference, string module) =>
        Modules.Any(other => other != module && reference == $"PosNfce.{other}.Application");

    private sealed record Project(IReadOnlyList<string> ProjectReferences, IReadOnlyList<string> PackageReferences)
    {
        public static Project Load(string name)
        {
            // Module projects live in src/<Module>/; SharedKernel and Desktop sit directly in src/.
            var module = name.Split('.')[1];
            var folder = Modules.Contains(module) ? Path.Combine("src", module, name) : Path.Combine("src", name);
            var path = Path.Combine(RepositoryRoot(), folder, $"{name}.csproj");
            var xml = XDocument.Load(path);

            var projects = xml.Descendants("ProjectReference")
                .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value.Replace('\\', '/')))
                .ToList();
            var packages = xml.Descendants("PackageReference")
                .Select(e => e.Attribute("Include")!.Value)
                .ToList();

            return new Project(projects, packages);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PosNfce.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("PosNfce.slnx not found above the test output folder.");
        }
    }
}
