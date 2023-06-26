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
using Refit;

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
			Func<Task<IApiResponse<T>>> call,
			[CallerMemberName] string? callerMethod = null)
			where T : class
		{
			callerMethod ??= "N/A";
			Logger.LogInformation("Executing request {CallerMethod}", callerMethod);

			try
			{
				return await PolicyProvider.BuildResultPolicy<T>().ExecuteAsync(async () =>
				{
					var result = await call.Invoke().ConfigureAwait(false);

					if ( result.Error != null)
					{
						throw result.Error;
					}

					Logger.LogInformation("Request completed {CallerMethod} ", callerMethod);

					return result.Content == null
						? (CallResult<T>) CallResult.CreateError(new CallResultError(new NullReferenceException("Content is null")))
						: CallResult<T>.CreateSuccess(result.StatusCode, result.Content);
				});
			}
			catch (Exception ex) when (ex is OperationCanceledException or WebException or SocketException)
			{
				Logger.LogInformation("Request timed out | Type: {Type} | Method: {Method}",
					typeof(T).Name,
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
			if (exception is ApiException apiException)
			{
				callResult = CallResult<T>.CreateError<T>(new CallResultError(apiException), apiException.StatusCode);
				return true;
			}

			callResult = default;
			return false;
		}

		protected virtual async Task<CallResult> ExecuteCall(
			Func<Task<IApiResponse>> call,
			[CallerMemberName] string? callerMethod = null)
		{
			callerMethod ??= "N/A";
			Logger.LogInformation("Executing request {CallerMethod}", callerMethod);

			try
			{
				return await VoidPolicy.ExecuteAsync(async () =>
				{
					var result = await call.Invoke().ConfigureAwait(false);
					if ( result.Error != null)
					{
						throw result.Error;
					}

					Logger.LogInformation("Request completed {CallerMethod} ", callerMethod);
					return CallResult.CreateSuccess(result.StatusCode);
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
			if (exception is ApiException apiException)
			{
				callResult = CallResult.CreateError(new CallResultError(apiException), apiException.StatusCode);
				return true;
			}

			callResult = default;
			return false;
		}
	}
}