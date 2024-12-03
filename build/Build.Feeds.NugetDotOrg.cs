using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
	[Secret, Parameter] string NugetApiKey;
	[Parameter] string NugetSource = "https://api.nuget.org/v3/index.json";

	Target PublishToNugetOrg => _ => _
		.DependsOn(Pack)
		.Requires(() => NugetSource)
		.Requires(() => NugetApiKey)
		.Executes(() =>
		{
			IEnumerable<AbsolutePath> artifactPackages = ArtifactsDirectory.GlobFiles("*.nupkg");
			if (artifactPackages.IsNullOrEmpty())
			{
				Log.Warning("No packages found to push to NuGet.org");
				return;
			}

			artifactPackages.ForEach(x =>
			{
				Log.Information("Pushing {Path} to NuGet.org", x);
				DotNetNuGetPush(s => s
					.SetSource(NugetSource)
					.SetApiKey(NugetApiKey)
					.SetTargetPath(x));
			});
		});
}