using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Shared.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Polly;

namespace dotRMDY.SyncSupport.Shared.Services.Implementations
{
	[PublicAPI]
	public abstract class WebServiceBase
	{
		protected readonly ILogger<WebServiceBase> Logger;
		protected readonly IPolicyProvider PolicyProvider;

		protected readonly AsyncPolicy<CallResult> VoidPolicy;

		protected WebServiceBase(
			ILogger<WebServiceBase> logger,
			IPolicyProvider policyProvider)
		{
			Logger = logger;
			PolicyProvider = policyProvider;

			VoidPolicy = PolicyProvider.BuildVoidPolicy();
		}

		protected virtual async Task<CallResult<T>> ExecuteCall<T>(
			Func<Task<T>> call,
			[CallerMemberName] string? callerMethod = null)
			where T : class
		{
			Logger.LogInformation("Executing request {CallerMethod}", callerMethod ?? string.Empty);

			try
			{
				return await PolicyProvider.BuildResultPolicy<T>().ExecuteAsync(async () =>
				{
					var result = await call.Invoke().ConfigureAwait(false);
					Logger.LogInformation("Request completed {CallerMethod}", callerMethod ?? string.Empty);
					return CallResult<T>.CreateSuccess(result);
				});
			}
			catch (Exception ex) when (ex is OperationCanceledException or WebException or SocketException)
			{
				Logger.LogInformation("Request timed out | Type: {Type} | Method: {Method}", typeof(T).Name,
					callerMethod);

				return CallResult<T>.CreateTimeOutError<T>();
			}
			catch (Exception ex)
			{
				if (HandleException<T>(ex, callerMethod, out var result))
				{
					return result;
				}

				Logger.LogWarning(ex, "Exception during web call | Type: {Type} | Method: {Method}",
					typeof(T).Name,
					callerMethod);

				return CallResult<T>.CreateError<T>(new CallResultError(ex));
			}
		}

		protected virtual bool HandleException<T>(
			Exception exception,
			string? callerMethod,
			[NotNullWhen(true)] out CallResult<T>? callResult)
		{
			callResult = default;
			return false;
		}

		protected virtual async Task<CallResult> ExecuteCall(
			Func<Task> call,
			[CallerMemberName] string? callerMethod = null)
		{
			Logger.LogInformation("Executing request {CallerMethod}", callerMethod ?? string.Empty);

			try
			{
				return await VoidPolicy.ExecuteAsync(async () =>
				{
					await call.Invoke().ConfigureAwait(false);
					Logger.LogInformation("Request completed {CallerMethod}", callerMethod ?? string.Empty);
					return CallResult.CreateSuccess();
				});
			}
			catch (Exception ex) when (ex is OperationCanceledException or WebException or SocketException)
			{
				Logger.LogInformation("Request timed out | Method: {Method}", callerMethod);

				return CallResult.CreateTimeOutError();
			}
			catch (Exception ex)
			{
				if (HandleException(ex, callerMethod, out var result))
				{
					return result;
				}

				Logger.LogWarning(ex, "Exception during web call | Method: {Method}",
					callerMethod);

				return CallResult.CreateError(new CallResultError(ex));
			}
		}

		protected virtual bool HandleException(
			Exception exception,
			string? callerMethod,
			[NotNullWhen(true)] out CallResult? callResult)
		{
			callResult = default;
			return false;
		}
	}
}