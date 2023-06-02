using System.Threading.Tasks;
using dotRMDY.SyncSupport.Shared.Models;

namespace dotRMDY.SyncSupport.Shared.Handlers
{
	public interface IOperationHandler<in TOperation> where TOperation : Operation
	{
		Task<CallResult> HandleOperation(TOperation operation);
	}
}