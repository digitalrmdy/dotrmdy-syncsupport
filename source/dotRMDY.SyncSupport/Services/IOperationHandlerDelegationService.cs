using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;

namespace dotRMDY.SyncSupport.Services
{
	[PublicAPI]
	public interface IOperationHandlerDelegationService
	{
		Task<CallResult> HandleOperation(Operation operation);
	}
}