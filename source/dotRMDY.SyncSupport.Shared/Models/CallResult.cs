using System;
using System.Collections.Generic;
using System.Linq;

namespace dotRMDY.SyncSupport.Shared.Models
{
	/// <summary>
	///     Call result to be used with services, webservices
	/// </summary>
	public class CallResult
	{
		private static readonly CallResult _noConnectionCallResult = new(CallResultStatus.NoConnection);
		private static readonly CallResult _notAuthenticatedCallResult = new(CallResultStatus.NotAuthenticated);
		private static readonly CallResult _successCallResult = new(CallResultStatus.Success);
		private static readonly CallResult _timeOutCallResult = new(CallResultStatus.TimeOut);

		protected CallResult(CallResultStatus status)
		{
			Status = status;
		}

		protected CallResult(CallResultError error)
		{
			Status = CallResultStatus.Error;
			Error = error;
		}

		protected CallResult(CallResult callResult)
		{
			Status = callResult.Status;
			Error = callResult.Error;
		}

		/// <summary>
		///     Gets the status.
		/// </summary>
		/// <value>The status.</value>
		public CallResultStatus Status { get; }

		/// <summary>
		///     Gets the error.
		/// </summary>
		/// <value>The error.</value>
		public CallResultError? Error { get; }

		public static CallResult CreateNoConnection()
		{
			return _noConnectionCallResult;
		}

		public static CallResult CreateNotAuthenticated()
		{
			return _notAuthenticatedCallResult;
		}

		public static CallResult CreateError(CallResultError error)
		{
			return new CallResult(error);
		}

		public static CallResult CreateSuccess()
		{
			return _successCallResult;
		}

		public static CallResult CreateTimeOutError()
		{
			return _timeOutCallResult;
		}

		public static CallResult Combine(IEnumerable<CallResult> callResults)
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

		public bool Errored()
		{
			return Status == CallResultStatus.Error;
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
		/// <param name="data">Data.</param>
		private CallResult(CallResultStatus status, TData? data = default) : base(status)
		{
			Data = data;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="T:BelgianRail.Edrive.Components.MvvmCross.Core.Shared.Helpers.CallResult`1" /> class.
		/// </summary>
		/// <param name="error">Error.</param>
		/// <param name="data">Data.</param>
		private CallResult(CallResultError error, TData? data = default) : base(error)
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
				CallResultStatus.Success => mappedData != null ? CreateSuccess(mappedData) : throw new NullReferenceException(nameof(mappedData)),
				CallResultStatus.Error => CreateError<TMappedType>(Error!),
				CallResultStatus.NoConnection => CreateNoConnection<TMappedType>(),
				CallResultStatus.NotAuthenticated => CreateNotAuthenticated<TMappedType>(),
				CallResultStatus.TimeOut => CreateTimeOutError<TMappedType>(),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public static CallResult<T> CreateNoConnection<T>()
		{
			return new CallResult<T>(CallResultStatus.NoConnection);
		}

		public static CallResult<T> CreateNotAuthenticated<T>()
		{
			return new CallResult<T>(CallResultStatus.NotAuthenticated);
		}

		public static CallResult<T> CreateError<T>(CallResultError error)
		{
			return new CallResult<T>(error);
		}

		public static CallResult<T> CreateSuccess<T>(T data)
		{
			return new CallResult<T>(CallResultStatus.Success, data);
		}

		public static CallResult<T> CreateTimeOutError<T>()
		{
			return new CallResult<T>(CallResultStatus.TimeOut);
		}
	}
}