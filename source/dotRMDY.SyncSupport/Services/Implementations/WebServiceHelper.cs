using System;
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
	public class WebServiceHelper : IWebServiceHelper
	{
		protected readonly ILogger<WebServiceHelper> Logger;
		protected readonly IPolicyProvider PolicyProvider;
		protected readonly IResilienceStrategyProvider ResilienceStrategyProvider;

		protected readonly AsyncPolicy<CallResult> VoidPolicy;
		protected readonly ResiliencePipeline<CallResult> VoidResilienceStrategy;

		public WebServiceHelper(ILogger<WebServiceHelper> logger, IPolicyProvider policyProvider)
		public WebServiceHelper(ILogger<WebServiceHelper> logger, IResilienceStrategyProvider resilienceStrategyProvider)
		{
			Logger = logger;
			PolicyProvider = policyProvider;
			ResilienceStrategyProvider = resilienceStrategyProvider;

			VoidPolicy = PolicyProvider.BuildVoidPolicy();
			VoidResilienceStrategy = ResilienceStrategyProvider.BuildVoidResilienceStrategy();
		}

		public async Task<CallResult<T>> ExecuteCall<T>(
			Func<Task<IApiResponse<T>>> call,
			[CallerMemberName] string? callerMethod = null)
			where T : class
		{
			callerMethod ??= "N/A";

			var preConditionsCheckCallResult = await CheckPreConditions<T>(callerMethod);
			if (preConditionsCheckCallResult != null)
			{
				return preConditionsCheckCallResult;
			}

			Logger.LogInformation("Executing request {CallerMethod}", callerMethod);

			try
			{
				return await ResilienceStrategyProvider.BuildResultResilienceStrategy<T>().ExecuteAsync(async () =>
				{
					var result = await call.Invoke().ConfigureAwait(false);

					if (result.Error != null)
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

				var handledCallResult = await HandleTimeout<T>(exception, callerMethod);
				return handledCallResult ?? CallResult<T>.CreateTimeOutError<T>();
			}
			catch (Exception exception)
			{
				Logger.LogWarning(exception, "Exception during web call | Type: {Type} | Method: {Method}", typeof(T).Name, callerMethod);

				var handledCallResult = await HandleException<T>(exception, callerMethod);
				return handledCallResult ?? CallResult<T>.CreateError<T>(new CallResultError(exception));
			}
		}

		public async Task<CallResult> ExecuteCall(
			Func<Task<IApiResponse>> call,
			[CallerMemberName] string? callerMethod = null)
		{
			callerMethod ??= "N/A";

			var preConditionsCheckCallResult = await CheckPreConditions(callerMethod);
			if (preConditionsCheckCallResult != null)
			{
				return preConditionsCheckCallResult;
			}

			Logger.LogInformation("Executing request {CallerMethod}", callerMethod);

			try
			{
				return await VoidResilienceStrategy.ExecuteAsync(async () =>
				{
					var result = await call.Invoke().ConfigureAwait(false);
					if (result.Error != null)
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

				var handledCallResult = await HandleTimeout(exception, callerMethod);
				return handledCallResult ?? CallResult.CreateTimeOutError();
			}
			catch (Exception exception)
			{
				Logger.LogWarning(exception, "Exception during web call | Method: {Method}", callerMethod);

				var handledCallResult = await HandleException(exception, callerMethod);
				return handledCallResult ?? CallResult.CreateError(new CallResultError(exception));
			}
		}

		protected virtual Task<CallResult<T>?> CheckPreConditions<T>(string callerMethod)
		{
			return Task.FromResult<CallResult<T>?>(null);
		}

		protected virtual Task<CallResult<T>?> HandleTimeout<T>(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult<CallResult<T>?>(null);
		}

		protected virtual Task<CallResult<T>?> HandleException<T>(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult(exception is ApiException apiException
				? CallResult<T>.CreateError<T>(new CallResultError(apiException), apiException.StatusCode)
				: null);
		}

		protected virtual Task<CallResult?> CheckPreConditions(string callerMethod)
		{
			return Task.FromResult<CallResult?>(null);
		}

		protected virtual Task<CallResult?> HandleTimeout(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult<CallResult?>(null);
		}

		protected virtual Task<CallResult?> HandleException(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult(exception is ApiException apiException
				? CallResult.CreateError(new CallResultError(apiException), apiException.StatusCode)
				: null);
		}
	}
}