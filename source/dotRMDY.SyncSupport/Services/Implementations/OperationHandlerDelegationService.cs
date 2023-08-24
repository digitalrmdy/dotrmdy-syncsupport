using System;
using System.Threading.Tasks;
using dotRMDY.Components.Services;
using dotRMDY.SyncSupport.Handlers;
using dotRMDY.SyncSupport.Models;

namespace dotRMDY.SyncSupport.Services.Implementations
{
	public abstract class OperationHandlerDelegationServiceBase : IOperationHandlerDelegationService
	{
		private readonly IResolver _resolver;

		protected OperationHandlerDelegationServiceBase(IResolver resolver)
		{
			_resolver = resolver;
		}

		public virtual Task<CallResult> HandleOperation(Operation operation)
		{
			throw new NotSupportedException($"Operation type '{operation.GetType().FullName}' is not supported.");
		}

		protected async Task<CallResult> HandleOperation<TOperation>(TOperation operation) where TOperation : Operation
		{
			var resolvedHandler = _resolver.Resolve<IOperationHandler<TOperation>>();
			if (resolvedHandler is null)
			{
				throw new InvalidOperationException($"Could not resolve handler for operation of type {operation.GetType().FullName}");
			}

			return await resolvedHandler.HandleOperation(operation).ConfigureAwait(false);
		}
	}
}