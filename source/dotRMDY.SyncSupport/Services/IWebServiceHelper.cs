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
		/// <summary>
		/// Executes a web service call and returns the result.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <param name="call">The function representing the API call.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T}"/> as the result.</returns>
		Task<CallResult<T>> ExecuteCall<T>(
			Func<CancellationToken, Task<IApiResponse<T>>> call,
			CancellationToken cancellationToken = default,
			[CallerMemberName] string? callerMethod = null)
			where T : class;

		/// <summary>
		/// Executes a web service call and returns the result.
		/// </summary>
		/// <param name="call">The function representing the API call.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult"/> as the result.</returns>
		Task<CallResult> ExecuteCall(
			Func<CancellationToken, Task<IApiResponse>> call,
			CancellationToken cancellationToken = default,
			[CallerMemberName] string? callerMethod = null);

		/// <summary>
		/// Executes a web service call and returns the result with error data.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <param name="call">The function representing the API call.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T, TE}"/> as the result.</returns>
		Task<CallResult<T, TE>> ExecuteCall<T, TE>(Func<CancellationToken, Task<IApiResponse<T>>> call,
			CancellationToken cancellationToken = default,
			[CallerMemberName] string? callerMethod = null)
			where T : class?
			where TE : class?;
	}
}