using Nuke.Common.CI.GitHubActions;

[GitHubActions(
	"pr",
	GitHubActionsImage.UbuntuLatest,
	On = [GitHubActionsTrigger.PullRequest],
	InvokedTargets = [nameof(Pack)],
	AutoGenerate = false,
	FetchDepth = 0,
	CacheKeyFiles = [])]
[GitHubActions(
	"publish",
	GitHubActionsImage.UbuntuLatest,
	InvokedTargets = [nameof(Publish)],
	AutoGenerate = false,
	FetchDepth = 0,
	CacheKeyFiles = [],
	OnPushTags = ["'*.*.*'"],
	ImportSecrets = [nameof(NugetApiKey)])]
partial class Build;