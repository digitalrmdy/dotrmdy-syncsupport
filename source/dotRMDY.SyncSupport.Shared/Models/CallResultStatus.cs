namespace dotRMDY.SyncSupport.Shared.Models
{
	/// <summary>
	/// Call result status.
	/// </summary>
	public enum CallResultStatus
	{
		/// <summary>
		/// Success.
		/// </summary>
		Success,

		/// <summary>
		/// Error.
		/// </summary>
		Error,

		/// <summary>
		/// There is no connection.
		/// </summary>
		NoConnection,

		/// <summary>
		/// There is no valid authentication.
		/// </summary>
		NotAuthenticated,

		/// <summary>
		/// The request timed out
		/// </summary>
		TimeOut
	}
}