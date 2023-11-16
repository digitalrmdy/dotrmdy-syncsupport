using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;
using Polly;

namespace dotRMDY.SyncSupport.Services
{
	[PublicAPI]
	public interface IResilienceStrategyProvider
	{
		ResiliencePipeline<CallResult> BuildVoidResilienceStrategy();
		ResiliencePipeline<CallResult<TResult>> BuildResultResilienceStrategy<TResult>() where TResult : class;
	}
}