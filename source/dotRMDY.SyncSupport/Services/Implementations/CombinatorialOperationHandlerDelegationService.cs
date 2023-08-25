using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;

namespace dotRMDY.SyncSupport.Services.Implementations
{
	public class CombinatorialOperationHandlerDelegationService : IOperationHandlerDelegationService
	{
		private readonly ICollection<IOperationHandlerDelegationService> _operationHandlerDelegationServices;

		public CombinatorialOperationHandlerDelegationService(ICollection<IOperationHandlerDelegationService> operationHandlerDelegationServices)
		{
			_operationHandlerDelegationServices = operationHandlerDelegationServices;
		}

		public Task<CallResult> HandleOperation(Operation operation)
		{
			foreach (var handlerDelegationService in _operationHandlerDelegationServices)
			{
				try
				{
					return handlerDelegationService.HandleOperation(operation);
				}
				catch (NotSupportedException)
				{
					// NOP
				}
			}

			throw new NotSupportedException($"Operation type '{operation.GetType().FullName}' is not supported.");
		}
	}
}