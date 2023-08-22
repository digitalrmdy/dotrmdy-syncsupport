using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Polly;
using Refit;

namespace dotRMDY.SyncSupport.Services.Implementations
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
			catch (Exception exception) when (exception is OperationCanceledException or WebException or SocketException)
			{
				Logger.LogInformation("Request timed out | Type: {Type} | Method: {Method}", typeof(T).Name, callerMethod);

				return HandleTimeout<T>(exception, callerMethod, out var result)
					? result
					: CallResult<T>.CreateTimeOutError<T>();
			}
			catch (Exception exception)
			{
				Logger.LogWarning(exception, "Exception during web call | Type: {Type} | Method: {Method}", typeof(T).Name, callerMethod);

				return HandleException<T>(exception, callerMethod, out var result)
					? result
					: CallResult<T>.CreateError<T>(new CallResultError(exception));
			}
		}

		protected virtual bool HandleTimeout<T>(
			Exception exception,
			string? callerMethod,
			[NotNullWhen(true)] out CallResult<T>? callResult)
		{
			callResult = default;
			return false;
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
			catch (Exception exception) when (exception is OperationCanceledException or WebException or SocketException)
			{
				Logger.LogInformation("Request timed out | Method: {Method}", callerMethod);

				return HandleTimeout(exception, callerMethod, out var result)
					? result
					: CallResult.CreateTimeOutError();
			}
			catch (Exception exception)
			{
				Logger.LogWarning(exception, "Exception during web call | Method: {Method}", callerMethod);

				return HandleException(exception, callerMethod, out var result)
					? result
					: CallResult.CreateError(new CallResultError(exception));
			}
		}

		protected virtual bool HandleTimeout(
			Exception exception,
			string? callerMethod,
			[NotNullWhen(true)] out CallResult? callResult)
		{
			callResult = default;
			return false;
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