using System;
using System.Runtime.CompilerServices;
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
			Func<Task<IApiResponse<T>>> call,
			[CallerMemberName] string? callerMethod = null)
			where T : class;

		Task<CallResult> ExecuteCall(
			Func<Task<IApiResponse>> call,
			[CallerMemberName] string? callerMethod = null);
	}
}