using System.Text.Json;
using Cake.Common;
using Cake.Core;
using Cake.Frosting;

namespace Build;

public class BuildContext : FrostingContext
{
    public new string Configuration { get; }
    public string SolutionPath { get; }
    public string ArtifactsPath { get; }
    public string VersionFilePath { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Configuration = context.Argument("configuration", "Release");
        SolutionPath = "app-common.slnx";
        ArtifactsPath = "artifacts";
        VersionFilePath = "version.json";
    }

    public string GetVersion()
    {
        var json = File.ReadAllText(VersionFilePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var version = JsonSerializer.Deserialize<VersionInfo>(json, options)!;
        var baseVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        return string.IsNullOrEmpty(version.Suffix) ? baseVersion : $"{baseVersion}-{version.Suffix}";
    }

    public void IncrementBuildVersion()
    {
        var json = File.ReadAllText(VersionFilePath);
        var readOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var version = JsonSerializer.Deserialize<VersionInfo>(json, readOptions)!;
        version.Build++;
        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(VersionFilePath, JsonSerializer.Serialize(version, writeOptions));
    }

    private class VersionInfo
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
        public string? Suffix { get; set; }
    }
}
