using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotRMDY.Components.Helpers;
using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;

namespace dotRMDY.SyncSupport.Services
{
	[PublicAPI]
	public interface IOperationService : INeedAsyncInitialization
	{
		Task AddOperation<TOperation>(Action<TOperation> enrichOperationAction) where TOperation : Operation, new();
		Task<IList<Operation>> GetAllOperations();
		Task UpdateOperation(Operation operation);
		Task DeleteOperation(Operation operation);
	}
}