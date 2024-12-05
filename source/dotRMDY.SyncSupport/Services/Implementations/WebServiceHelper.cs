using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Refit;

namespace dotRMDY.SyncSupport.Services.Implementations
{
	/// <summary>
	/// Helper class for executing web service calls.
	/// </summary>
	[PublicAPI]
	public class WebServiceHelper : IWebServiceHelper
	{
		protected readonly ILogger<WebServiceHelper> Logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="WebServiceHelper"/> class.
		/// </summary>
		/// <param name="logger">The logger instance.</param>
		public WebServiceHelper(ILogger<WebServiceHelper> logger)
		{
			Logger = logger;
		}

		/// <summary>
		/// Executes a web service call and returns the result.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <param name="call">The function representing the API call.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T}"/> as the result.</returns>
		public async Task<CallResult<T>> ExecuteCall<T>(
			Func<CancellationToken, Task<IApiResponse<T>>> call,
			CancellationToken cancellationToken = default,
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
				var result = await call.Invoke(cancellationToken).ConfigureAwait(false);

				if (result.Error != null)
				{
					throw result.Error;
				}

				Logger.LogInformation("Request completed {CallerMethod} ", callerMethod);

				return result.Content == null
					? (CallResult<T>) CallResult.CreateError(new CallResultError(new NullReferenceException("Content is null")))
					: CallResult<T>.CreateSuccess(result.StatusCode, result.Content);
			}
			catch (Exception exception) when (ShouldHandleExceptionAsTimeout(exception))
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

		/// <summary>
		/// Executes a web service call and returns the result with error data.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <param name="call">The function representing the API call.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T, TE}"/> as the result.</returns>
		public async Task<CallResult<T, TE>> ExecuteCall<T, TE>(
			Func<CancellationToken, Task<IApiResponse<T>>> call,
			CancellationToken cancellationToken = default,
			[CallerMemberName] string? callerMethod = null)
			where T : class?
			where TE : class?
		{
			callerMethod ??= "N/A";

			var preConditionsCheckCallResult = await CheckPreConditions<T, TE>(callerMethod);
			if (preConditionsCheckCallResult != null)
			{
				return preConditionsCheckCallResult;
			}

			Logger.LogInformation("Executing request {CallerMethod}", callerMethod);

			IApiResponse<T>? result = null;
			try
			{
				result = await call.Invoke(cancellationToken).ConfigureAwait(false);

				if (result.Error != null)
				{
					throw result.Error;
				}

				Logger.LogInformation("Request completed {CallerMethod} ", callerMethod);

				return result.Content == null
					? (CallResult<T, TE>) CallResult.CreateError(new CallResultError(new NullReferenceException("Content is null")))
					: CallResult<T, TE>.CreateSuccess<T, TE>(result.StatusCode, result.Content);
			}
			catch (Exception exception) when (ShouldHandleExceptionAsTimeout(exception))
			{
				Logger.LogInformation("Request timed out | Type: {Type} | Method: {Method}", typeof(T).Name, callerMethod);

				var handledCallResult = await HandleTimeout<T, TE>(exception, callerMethod);
				return handledCallResult ?? CallResult<T, TE>.CreateTimeOutError<T, TE>();
			}
			catch (Exception exception)
			{
				Logger.LogWarning(exception, "Exception during web call | Type: {Type} | Method: {Method}", typeof(T).Name, callerMethod);

				if (string.IsNullOrWhiteSpace(result?.Error?.Content))
				{
					return (CallResult<T, TE>) CallResult.CreateError(new CallResultError(new NullReferenceException("Content is null")));
				}

				var callResultError = new CallResultError(exception);

				try
				{
					var content = JsonSerializer.Deserialize<TE>(result.Error.Content);
					return CallResult<T, TE>.CreateError<T, TE>(callResultError, content);
				}
				catch (Exception e)
				{
					Logger.LogWarning(e, "Error deserialization failed | Type: {Type} | Method: {Method}", typeof(TE).Name, callerMethod);
					return CallResult<T, TE>.CreateError<T, TE>(callResultError);
				}
			}
		}

		/// <summary>
		/// Executes a web service call and returns the result.
		/// </summary>
		/// <param name="call">The function representing the API call.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult"/> as the result.</returns>
		public async Task<CallResult> ExecuteCall(
			Func<CancellationToken, Task<IApiResponse>> call,
			CancellationToken cancellationToken = default,
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
				var result = await call.Invoke(cancellationToken).ConfigureAwait(false);
				if (result.Error != null)
				{
					throw result.Error;
				}

				Logger.LogInformation("Request completed {CallerMethod} ", callerMethod);

				return CallResult.CreateSuccess(result.StatusCode);
			}
			catch (Exception exception) when (ShouldHandleExceptionAsTimeout(exception))
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

		/// <summary>
		/// Checks preconditions before executing a web service call.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T, TE}"/> as the result.</returns>
		protected virtual Task<CallResult<T, TE>?> CheckPreConditions<T, TE>(string callerMethod)
		{
			return Task.FromResult<CallResult<T, TE>?>(null);
		}

		/// <summary>
		/// Checks preconditions before executing a web service call.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T}"/> as the result.</returns>
		protected virtual Task<CallResult<T>?> CheckPreConditions<T>(string callerMethod)
		{
			return Task.FromResult<CallResult<T>?>(null);
		}

		/// <summary>
		/// Handles timeout exceptions during a web service call.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <param name="exception">The exception that occurred.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T, TE}"/> as the result.</returns>
		protected virtual Task<CallResult<T, TE>?> HandleTimeout<T, TE>(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult<CallResult<T, TE>?>(null);
		}

		/// <summary>
		/// Handles timeout exceptions during a web service call.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <param name="exception">The exception that occurred.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T}"/> as the result.</returns>
		protected virtual Task<CallResult<T>?> HandleTimeout<T>(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult<CallResult<T>?>(null);
		}

		/// <summary>
		/// Handles exceptions during a web service call.
		/// </summary>
		/// <typeparam name="T">The type of the expected result.</typeparam>
		/// <param name="exception">The exception that occurred.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult{T}"/> as the result.</returns>
		protected virtual Task<CallResult<T>?> HandleException<T>(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult(exception is ApiException apiException
				? CallResult<T>.CreateError<T>(new CallResultError(apiException), apiException.StatusCode)
				: null);
		}

		/// <summary>
		/// Checks preconditions before executing a web service call.
		/// </summary>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult"/> as the result.</returns>
		protected virtual Task<CallResult?> CheckPreConditions(string callerMethod)
		{
			return Task.FromResult<CallResult?>(null);
		}

		/// <summary>
		/// Handles timeout exceptions during a web service call.
		/// </summary>
		/// <param name="exception">The exception that occurred.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult"/> as the result.</returns>
		protected virtual Task<CallResult?> HandleTimeout(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult<CallResult?>(null);
		}

		/// <summary>
		/// Handles exceptions during a web service call.
		/// </summary>
		/// <param name="exception">The exception that occurred.</param>
		/// <param name="callerMethod">The name of the calling method.</param>
		/// <returns>A task representing the asynchronous operation, with a <see cref="CallResult"/> as the result.</returns>
		protected virtual Task<CallResult?> HandleException(
			Exception exception,
			string callerMethod)
		{
			return Task.FromResult(exception is ApiException apiException
				? CallResult.CreateError(new CallResultError(apiException), apiException.StatusCode)
				: null);
		}

		/// <summary>
		/// Determines whether the exception should be handled as a timeout.
		/// </summary>
		/// <param name="exception">The exception that occurred.</param>
		/// <returns><c>true</c> if the exception should be handled as a timeout; otherwise, <c>false</c>.</returns>
		protected virtual bool ShouldHandleExceptionAsTimeout(Exception exception)
		{
			return exception is OperationCanceledException or WebException { Status: WebExceptionStatus.Timeout } or SocketException;
		}
	}
}