using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;

namespace dotRMDY.SyncSupport.Handlers
{
	public interface IOperationHandler<in TOperation> where TOperation : Operation
	{
		Task<CallResult> HandleOperation(TOperation operation);
	}
}