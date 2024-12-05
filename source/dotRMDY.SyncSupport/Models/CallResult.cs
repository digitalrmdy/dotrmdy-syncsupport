using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace dotRMDY.SyncSupport.Models
{
	/// <summary>
	///     Call result to be used with services, webservices
	/// </summary>
	public class CallResult
	{
		private static readonly CallResult NoConnectionCallResult = new(CallResultStatus.NoConnection);
		private static readonly CallResult NotAuthenticatedCallResult = new(CallResultStatus.NotAuthenticated);
		private static readonly CallResult TimeOutCallResult = new(CallResultStatus.TimeOut);

		protected CallResult(CallResultStatus status, HttpStatusCode? httpStatusCode = null)
		{
			Status = status;
			StatusCode = httpStatusCode;
		}

		protected CallResult(CallResultError error, HttpStatusCode? httpStatusCode = null)
		{
			Status = CallResultStatus.Error;
			StatusCode = httpStatusCode;
			Error = error;
		}

		protected CallResult(CallResult callResult)
		{
			Status = callResult.Status;
			StatusCode = callResult.StatusCode;
			Error = callResult.Error;
		}

		/// <summary>
		///     Gets the status.
		/// </summary>
		/// <value>The status.</value>
		public CallResultStatus Status { get; }

		/// <summary>
		///		Gets the status code.
		/// </summary>
		/// <value>The status code.</value>
		public HttpStatusCode? StatusCode { get; }

		/// <summary>
		///     Gets the error.
		/// </summary>
		/// <value>The error.</value>
		protected CallResultError? Error { get; }

		public static CallResult CreateSuccess(HttpStatusCode statusCode)
		{
			return new CallResult(CallResultStatus.Success, statusCode);
		}

		public static CallResult CreateError(CallResultError error, HttpStatusCode? statusCode = null)
		{
			return new CallResult(error, statusCode);
		}

		public static CallResult CreateNoConnection()
		{
			return NoConnectionCallResult;
		}

		public static CallResult CreateNotAuthenticated()
		{
			return NotAuthenticatedCallResult;
		}

		public static CallResult CreateTimeOutError()
		{
			return TimeOutCallResult;
		}

		public static CallResult Combine(ICollection<CallResult> callResults)
		{
			var callResultErrors = callResults.Select(cr => cr.Error).OfType<CallResultError>().ToArray();
			if (callResultErrors.Any())
			{
				return new CallResult(new CombinedCallResultError(callResultErrors));
			}

			var status = CallResultStatus.Success;
			var callResultStatusArray = callResults.Select(cr => cr.Status).Distinct().ToArray();
			if (callResultStatusArray.Any(s => s == CallResultStatus.NotAuthenticated))
			{
				status = CallResultStatus.NotAuthenticated;
			}
			else if (callResultStatusArray.Any(s => s == CallResultStatus.NoConnection))
			{
				status = CallResultStatus.NoConnection;
			}

			return new CallResult(status);
		}

		/// <summary>
		///     Set a fallback error message.
		/// </summary>
		/// <param name="fallbackMessage">Fallback message to set</param>
		public void EnsureErrorMessage(string fallbackMessage)
		{
			Error?.SetFallbackMessage(fallbackMessage);
		}

		public bool Successful()
		{
			return Status == CallResultStatus.Success;
		}

		public bool NotSuccessful()
		{
			return Status != CallResultStatus.Success;
		}

		public bool Errored()
		{
			return Status == CallResultStatus.Error;
		}

		public bool NoConnection()
		{
			return Status == CallResultStatus.NoConnection;
		}

		public bool NotAuthenticated()
		{
			return Status == CallResultStatus.NotAuthenticated;
		}

		public bool Authenticated()
		{
			return Status != CallResultStatus.NotAuthenticated;
		}

		public bool TimeOut()
		{
			return Status == CallResultStatus.TimeOut;
		}
	}

	/// <summary>
	///     Call result to be used with services, webservices; provides Data
	/// </summary>
	public class CallResult<TData> : CallResult
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="T:BelgianRail.Edrive.Components.MvvmCross.Core.Shared.Helpers.CallResult`1" /> class.
		/// </summary>
		/// <param name="status">Status.</param>
		/// <param name="httpStatusCode">Status code.</param>
		/// <param name="data">Data.</param>
		protected CallResult(CallResultStatus status, HttpStatusCode? httpStatusCode = null, TData? data = default) : base(status, httpStatusCode)
		{
			Data = data;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="T:BelgianRail.Edrive.Components.MvvmCross.Core.Shared.Helpers.CallResult`1" /> class.
		/// </summary>
		/// <param name="error">Error.</param>
		/// <param name="httpStatusCode">Status code.</param>
		/// <param name="data">Data.</param>
		protected CallResult(CallResultError error, HttpStatusCode? httpStatusCode = null, TData? data = default) : base(error, httpStatusCode)
		{
			Data = data;
		}

		/// <summary>
		///     Gets the data.
		/// </summary>
		/// <value>The data.</value>
		public TData? Data { get; }

		/// <summary>
		///     Map callresult data to another type
		/// </summary>
		/// <param name="mappedData">Mapped data</param>
		/// <typeparam name="TMappedType">Destination type</typeparam>
		/// <returns>A callresult with mapped data</returns>
		public CallResult<TMappedType> Map<TMappedType>(TMappedType? mappedData = null) where TMappedType : class
		{
			return Status switch
			{
				CallResultStatus.Success => StatusCode != null && mappedData != null
					? CreateSuccess(StatusCode.Value, mappedData)
					: throw new NullReferenceException(nameof(mappedData)),
				CallResultStatus.Error => CreateError<TMappedType>(Error!),
				CallResultStatus.NoConnection => CreateNoConnection<TMappedType>(),
				CallResultStatus.NotAuthenticated => CreateNotAuthenticated<TMappedType>(),
				CallResultStatus.TimeOut => CreateTimeOutError<TMappedType>(),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public static CallResult<T> CreateSuccess<T>(HttpStatusCode statusCode, T data)
		{
			return new CallResult<T>(CallResultStatus.Success, statusCode, data);
		}

		public static CallResult<T> CreateError<T>(CallResultError error, HttpStatusCode? statusCode = null)
		{
			return new CallResult<T>(error, statusCode);
		}

		public static CallResult<T> CreateNoConnection<T>()
		{
			return new CallResult<T>(CallResultStatus.NoConnection);
		}

		public static CallResult<T> CreateNotAuthenticated<T>()
		{
			return new CallResult<T>(CallResultStatus.NotAuthenticated);
		}

		public static CallResult<T> CreateTimeOutError<T>()
		{
			return new CallResult<T>(CallResultStatus.TimeOut);
		}
	}

	/// <summary>
	/// Represents a call result with data and error data.
	/// </summary>
	/// <typeparam name="TData">The type of the data.</typeparam>
	/// <typeparam name="TError">The type of the error data.</typeparam>
	public class CallResult<TData, TError> : CallResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CallResult{TData, TError}"/> class.
		/// </summary>
		/// <param name="status">The status of the call result.</param>
		/// <param name="httpStatusCode">The HTTP status code.</param>
		/// <param name="data">The data.</param>
		/// <param name="errorData">The error data.</param>
		protected CallResult(CallResultStatus status, HttpStatusCode? httpStatusCode = null, TData? data = default, TError? errorData = default)
			: base(status, httpStatusCode)
		{
			Data = data;
			ErrorData = errorData;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CallResult{TData, TError}"/> class.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <param name="httpStatusCode">The HTTP status code.</param>
		/// <param name="data">The data.</param>
		/// <param name="errorData">The error data.</param>
		protected CallResult(CallResultError error, HttpStatusCode? httpStatusCode = null, TData? data = default, TError? errorData = default)
			: base(error, httpStatusCode)
		{
			Data = data;
			ErrorData = errorData;
		}

		/// <summary>
		/// Gets the data.
		/// </summary>
		public TData? Data { get; }

		/// <summary>
		/// Gets the error data.
		/// </summary>
		public TError? ErrorData { get; }

		/// <summary>
		/// Maps the call result data to another type.
		/// </summary>
		/// <typeparam name="TMappedType">The type of the mapped data.</typeparam>
		/// <typeparam name="TErrorMapped">The type of the mapped error data.</typeparam>
		/// <param name="mappedData">The mapped data.</param>
		/// <param name="mappedErrorData">The mapped error data.</param>
		/// <returns>A call result with the mapped data.</returns>
		/// <exception cref="NullReferenceException">Thrown when the mapped data is null.</exception>
		public CallResult<TMappedType, TErrorMapped> Map<TMappedType, TErrorMapped>(TMappedType? mappedData = null, TErrorMapped? mappedErrorData = null)
			where TMappedType : class
			where TErrorMapped : class
		{
			return Status switch
			{
				CallResultStatus.Success => StatusCode != null && mappedData != null
					? CreateSuccess<TMappedType, TErrorMapped>(StatusCode.Value, mappedData)
					: throw new NullReferenceException(nameof(mappedData)),
				CallResultStatus.Error => CreateError<TMappedType, TErrorMapped>(Error!, mappedErrorData),
				CallResultStatus.NoConnection => CreateNoConnection<TMappedType, TErrorMapped>(),
				CallResultStatus.NotAuthenticated => CreateNotAuthenticated<TMappedType, TErrorMapped>(),
				CallResultStatus.TimeOut => CreateTimeOutError<TMappedType, TErrorMapped>(),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		/// <summary>
		/// Creates a successful call result.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <param name="statusCode">The HTTP status code.</param>
		/// <param name="data">The data.</param>
		/// <param name="errorData">The error data.</param>
		/// <returns>A successful call result.</returns>
		public static CallResult<T, TE> CreateSuccess<T, TE>(HttpStatusCode statusCode, T data, TE? errorData = default)
		{
			return new CallResult<T, TE>(CallResultStatus.Success, statusCode, data, errorData);
		}

		/// <summary>
		/// Creates an error call result.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <param name="error">The error.</param>
		/// <param name="errorData">The error data.</param>
		/// <param name="statusCode">The HTTP status code.</param>
		/// <returns>An error call result.</returns>
		public static CallResult<T, TE> CreateError<T, TE>(CallResultError error, TE? errorData = default, HttpStatusCode? statusCode = null)
		{
			return new CallResult<T, TE>(error, statusCode, default, errorData);
		}

		/// <summary>
		/// Creates a call result indicating no connection.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <returns>A call result indicating no connection.</returns>
		public static CallResult<T, TE> CreateNoConnection<T, TE>()
		{
			return new CallResult<T, TE>(CallResultStatus.NoConnection);
		}

		/// <summary>
		/// Creates a call result indicating not authenticated.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <returns>A call result indicating not authenticated.</returns>
		public static CallResult<T, TE> CreateNotAuthenticated<T, TE>()
		{
			return new CallResult<T, TE>(CallResultStatus.NotAuthenticated);
		}

		/// <summary>
		/// Creates a call result indicating a timeout error.
		/// </summary>
		/// <typeparam name="T">The type of the data.</typeparam>
		/// <typeparam name="TE">The type of the error data.</typeparam>
		/// <returns>A call result indicating a timeout error.</returns>
		public static CallResult<T, TE> CreateTimeOutError<T, TE>()
		{
			return new CallResult<T, TE>(CallResultStatus.TimeOut);
		}
	}
}