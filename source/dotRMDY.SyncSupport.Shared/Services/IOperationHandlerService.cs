using System.Threading.Tasks;
using dotRMDY.SyncSupport.Shared.Models;
using JetBrains.Annotations;

namespace dotRMDY.SyncSupport.Shared.Services
{
	[PublicAPI]
	public interface IOperationHandlerService
	{
		/// <summary>
		/// Try handling of pending operations
		/// </summary>
		Task<CallResult> HandlePendingOperations();

		Task MarkOperationAsFailed(Operation operation);
	}
}