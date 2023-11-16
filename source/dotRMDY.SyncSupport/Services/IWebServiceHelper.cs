using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;
using Refit;

namespace dotRMDY.SyncSupport.Services
{
	[PublicAPI]
	public interface IWebServiceHelper
	{
		Task<CallResult<T>> ExecuteCall<T>(
			Func<CancellationToken, Task<IApiResponse<T>>> call,
			CancellationToken cancellationToken = default,
			[CallerMemberName] string? callerMethod = null)
			where T : class;

		Task<CallResult> ExecuteCall(
			Func<CancellationToken, Task<IApiResponse>> call,
			CancellationToken cancellationToken = default,
			[CallerMemberName] string? callerMethod = null);
	}
}