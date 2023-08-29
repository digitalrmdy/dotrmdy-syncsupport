using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;

[AzurePipelines(
	suffix: "PR",
	AzurePipelinesImage.UbuntuLatest,
	AutoGenerate = false,
	FetchDepth = 0,
	TriggerDisabled = true,
	InvokedTargets = new[] { nameof(Pack) },
	CacheKeyFiles = new string[0],
	CachePaths = new string[0])]
[AzurePipelines(
	suffix: "Publish",
	AzurePipelinesImage.UbuntuLatest,
	AutoGenerate = false,
	FetchDepth = 0,
	TriggerBatch = true,
	TriggerTagsInclude = new[] { "'*.*.*'" },
	InvokedTargets = new[] { nameof(Publish) },
	CacheKeyFiles = new string[0],
	CachePaths = new string[0])]
partial class Build
{
	[CI] readonly AzurePipelines AzurePipelines;

	Target PublishTestAndCoverageResultsToAzurePipelines => _ => _
		.TriggeredBy(RunTests)
		.OnlyWhenStatic(() => Host is AzurePipelines)
		.Executes(() =>
		{
			AzurePipelines?.PublishTestResults(
				"Unit test results",
				AzurePipelinesTestResultsType.VSTest,
				Solution
					.GetAllProjects("*.UnitTests")
					.SelectMany(x => Globbing.GlobFiles(x.Directory.ToString(), "**/*.trx")),
				true,
				configuration: Configuration);

			AzurePipelines?.PublishCodeCoverage(
				AzurePipelinesCodeCoverageToolType.Cobertura,
				CoverageResultsReportDirectory / "Cobertura.xml",
				CoverageResultsReportDirectory);
		});
}