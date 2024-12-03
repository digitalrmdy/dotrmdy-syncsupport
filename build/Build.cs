using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;

[ShutdownDotNetAfterServerBuild]
partial class Build : NukeBuild
{
	/// Support plugins are available for:
	///   - JetBrains ReSharper        https://nuke.build/resharper
	///   - JetBrains Rider            https://nuke.build/rider
	///   - Microsoft VisualStudio     https://nuke.build/visualstudio
	///   - Microsoft VSCode           https://nuke.build/vscode
	public static int Main() => Execute<Build>(x => x.Pack);

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Solution] readonly Solution Solution;

	[GitRepository] readonly GitRepository GitRepository;

	[GitVersion(NoFetch = true)] readonly GitVersion GitVersion;

	static AbsolutePath SourceDirectory => RootDirectory / "source";
	static AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

	static AbsolutePath ResultsDirectory => TemporaryDirectory / "results";
	static AbsolutePath CoverageResultsDirectory => ResultsDirectory / "coverage";
	static AbsolutePath CoverageResultsReportDirectory => CoverageResultsDirectory / "report";

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			SourceDirectory
				.GlobDirectories("**/bin", "**/obj", "**/TestResults")
				.ForEach(ap => ap.DeleteDirectory());

			ArtifactsDirectory.CreateOrCleanDirectory();
			ResultsDirectory.DeleteDirectory();
		});

	Target Restore => _ => _
		.Executes(() =>
		{
			DotNetRestore(s => s
				.SetProjectFile(Solution)
				.EnableDeterministic()
				.EnableContinuousIntegrationBuild());
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(s => s
				.EnableNoRestore()
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.SetDeterministic(IsServerBuild)
				.SetContinuousIntegrationBuild(IsServerBuild)
				.SetVersion(GitVersion.FullSemVer)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion));
		});

	Target RunTests => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			DotNetTest(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.EnableNoRestore()
				.EnableNoBuild()
				.SetLoggers("trx")
				.SetDataCollector("XPlat Code Coverage")
				.AddRunSetting("DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format", "cobertura"));

			ReportGenerator(s => s
				.SetReportTypes(ReportTypes.HtmlInline_AzurePipelines, ReportTypes.Cobertura)
				.SetReports(Solution
					.GetAllProjects("*.UnitTests")
					.SelectMany(x => Globbing.GlobFiles((string) (x.Directory / "TestResults"), "**/coverage.cobertura.xml")))
				.SetTargetDirectory(CoverageResultsReportDirectory)
				.SetVerbosity(ReportGeneratorVerbosity.Verbose));
		});

	Target Pack => _ => _
		.DependsOn(Clean, RunTests)
		.Produces(ArtifactsDirectory / "*.nupkg")
		.Executes(() =>
		{
			DotNetPack(s => s
				.EnableNoRestore()
				.EnableNoBuild()
				.SetProject(Solution)
				.SetConfiguration(Configuration)
				.SetVersion(GitVersion.NuGetVersion)
				.SetProperty("RepositoryBranch", GitRepository.Branch)
				.SetProperty("RepositoryCommit", GitRepository.Commit)
				.SetOutputDirectory(ArtifactsDirectory));
		});

	Target Publish => _ => _
		.DependsOn(PublishToNugetOrg);
}