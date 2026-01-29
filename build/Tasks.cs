using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace Build;

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Cleaning solution...");

        context.CleanDirectory(context.ArtifactsPath);

        context.DotNetClean(context.SolutionPath, new DotNetCleanSettings
        {
            Configuration = context.Configuration
        });
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Restoring NuGet packages...");

        context.DotNetRestore(context.SolutionPath);
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Building solution...");

        context.DotNetBuild(context.SolutionPath, new DotNetBuildSettings
        {
            Configuration = context.Configuration,
            NoRestore = true
        });
    }
}

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Running tests...");

        context.DotNetTest(context.SolutionPath, new DotNetTestSettings
        {
            Configuration = context.Configuration,
            NoRestore = true,
            NoBuild = true,
            Collectors = ["XPlat Code Coverage"]
        });
    }
}

[TaskName("Pack")]
[IsDependentOn(typeof(TestTask))]
public sealed class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Creating NuGet packages...");

        context.IncrementBuildVersion();
        var version = context.GetVersion();

        context.Log.Information($"Package version: {version}");

        context.EnsureDirectoryExists(context.ArtifactsPath);

        var projects = new[]
        {
            "src/AppCommon.Core/AppCommon.Core.csproj",
            "src/AppCommon.Aws/AppCommon.Aws.csproj",
            "src/AppCommon.Persistence/AppCommon.Persistence.csproj",
            "src/AppCommon.Api/AppCommon.Api.csproj"
        };

        foreach (var project in projects)
        {
            context.DotNetPack(project, new DotNetPackSettings
            {
                Configuration = context.Configuration,
                NoRestore = true,
                NoBuild = true,
                OutputDirectory = context.ArtifactsPath,
                ArgumentCustomization = args => args.Append($"/p:PackageVersion={version}")
            });
        }
    }
}

[TaskName("Push")]
[IsDependentOn(typeof(PackTask))]
public sealed class PushTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Pushing NuGet packages...");

        var nugetSource = context.Argument<string>("nuget-source");
        var nugetApiKey = context.Argument<string>("nuget-api-key");

        var packages = context.GetFiles($"{context.ArtifactsPath}/*.nupkg");

        foreach (var package in packages)
        {
            context.DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
            {
                Source = nugetSource,
                ApiKey = nugetApiKey
            });
        }
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(BuildTask))]
public sealed class DefaultTask : FrostingTask<BuildContext>
{
}
