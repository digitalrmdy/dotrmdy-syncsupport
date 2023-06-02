using dotRMDY.SyncSupport.Shared.Models;
using JetBrains.Annotations;
using Polly;

namespace dotRMDY.SyncSupport.Shared.Services
{
	[PublicAPI]
	public interface IPolicyProvider
	{
		AsyncPolicy<CallResult> BuildVoidPolicy();
		AsyncPolicy<CallResult<TResult>> BuildResultPolicy<TResult>() where TResult : class;
	}
}