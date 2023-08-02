using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
	const string RMDY_AZURE_ARTIFACTS_FEED_NAME = "RMDY-AzureArtifacts";

	[Secret, Parameter]
	readonly string DotRmdyAzureArtifactsToken;

	Target UpdateDotRmdyAzureArtifactsFeedCredentials => _ => _
		.OnlyWhenStatic(() => IsServerBuild && Host is not AzurePipelines)
		.Requires(() => !string.IsNullOrEmpty(DotRmdyAzureArtifactsToken))
		.Executes(() =>
		{
			DotNet($"nuget update source {RMDY_AZURE_ARTIFACTS_FEED_NAME} --username az --password {DotRmdyAzureArtifactsToken} --store-password-in-clear-text");
		});

	Target PublishToDotRmdyAzureArtifacts => _ => _
		.DependsOn(Pack)
		.Executes(() =>
		{
			IEnumerable<AbsolutePath> artifactPackages = ArtifactsDirectory.GlobFiles("*.nupkg");

			DotNetNuGetPush(s => s
				.SetSource(RMDY_AZURE_ARTIFACTS_FEED_NAME)
				// ReSharper disable once UnencryptedSecretHighlighting
				.SetApiKey("az") // API key is ignored due to the usage of a PAT, but still has to be filled in.
				.EnableSkipDuplicate()
				.CombineWith(artifactPackages, (_, v) => _
					.SetTargetPath(v)));
		});
}