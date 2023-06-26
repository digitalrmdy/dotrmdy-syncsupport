using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace dotRMDY.SyncSupport.Shared.Models
{
	/// <summary>
	///     Call result to be used with services, webservices
	/// </summary>
	public class CallResult
	{
		private static readonly CallResult _noConnectionCallResult = new(CallResultStatus.NoConnection);
		private static readonly CallResult _notAuthenticatedCallResult = new(CallResultStatus.NotAuthenticated);
		private static readonly CallResult _timeOutCallResult = new(CallResultStatus.TimeOut);

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
		public CallResultError? Error { get; }

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
			return _noConnectionCallResult;
		}

		public static CallResult CreateNotAuthenticated()
		{
			return _notAuthenticatedCallResult;
		}

		public static CallResult CreateTimeOutError()
		{
			return _timeOutCallResult;
		}

		public static CallResult Combine(ICollection<CallResult> callResults)
		{
			var callResultErrors = callResults.Select(cr => cr.Error).OfType<CallResultError>().ToArray();
			if (callResultErrors.Any())
			{
				return new CallResult(new CombinedCallResultError(callResultErrors));
			}

			var status = CallResultStatus.Success;
			var stati = callResults.Select(cr => cr.Status).Distinct().ToArray();
			if (stati.Any(s => s == CallResultStatus.NotAuthenticated))
			{
				status = CallResultStatus.NotAuthenticated;
			}
			else if (stati.Any(s => s == CallResultStatus.NoConnection))
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
			return new CallResult<T>(CallResultStatus.Success,  statusCode, data);
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
}