using System;

namespace dotRMDY.SyncSupport.Shared.Models
{
	/// <summary>
	/// Call result error.
	/// </summary>
	public class CallResultError
	{
		public CallResultError(string technicalMessage, string? code = null, string? message = null)
		{
			Code = code;
			Message = message;
			TechnicalMessage = technicalMessage;
		}

		public CallResultError(Exception exc) : this(exc.Message, exc.GetType().Name)
		{
		}

		/// <summary>
		/// Gets the code.
		/// </summary>
		/// <value>The code.</value>
		public string? Code { get; }

		/// <summary>
		/// Gets the message.
		/// </summary>
		/// <value>The message.</value>
		public string? Message { get; private set; }

		/// <summary>
		/// Gets the technical message.
		/// </summary>
		/// <value>The technical message.</value>
		public string TechnicalMessage { get; }

		/// <summary>
		/// Set a fallback error message. This method does nothing when a non-empty message or a previous non-empty fallback has been set.
		/// </summary>
		/// <param name="fallbackMessage">Fallback message</param>
		public void SetFallbackMessage(string fallbackMessage)
		{
			if (string.IsNullOrWhiteSpace(Message))
			{
				Message = fallbackMessage;
			}
		}
	}
}