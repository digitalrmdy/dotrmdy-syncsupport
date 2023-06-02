using Nuke.Common.CI.AzurePipelines;

[AzurePipelines(
    suffix: "PR",
    AzurePipelinesImage.UbuntuLatest,
    AutoGenerate = false,
    FetchDepth = 0,
    TriggerBatch = true,
    PullRequestsBranchesInclude = new[] { "main", "develop" },
    ImportVariableGroups = new[] { "dotRMDY-MyGet" },
    ImportSecrets = new[] { nameof(MyGetUsername), nameof(MyGetApiKey) },
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
    ImportVariableGroups = new[] { "dotRMDY-MyGet" },
    ImportSecrets = new[] { nameof(MyGetUsername), nameof(MyGetApiKey) },
    InvokedTargets = new[] { nameof(PublishToMyGet) },
    CacheKeyFiles = new string[0],
    CachePaths = new string[0])]
partial class Build
{
}