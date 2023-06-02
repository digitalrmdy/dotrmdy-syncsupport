using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Shared.Models;
using JetBrains.Annotations;

namespace dotRMDY.SyncSupport.Shared.Services
{
	[PublicAPI]
	public interface IOperationService
	{
		Task AddOperation<TOperation>(Action<TOperation> enrichOperationAction) where TOperation : Operation, new();
		Task<IList<Operation>> GetAllOperations();
		Task UpdateOperation(Operation operation);
		Task DeleteOperation(Operation operation);
	}
}