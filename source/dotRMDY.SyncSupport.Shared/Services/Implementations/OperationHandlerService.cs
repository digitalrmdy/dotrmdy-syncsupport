using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.Components.Shared.Services;
using dotRMDY.SyncSupport.Shared.Handlers;
using dotRMDY.SyncSupport.Shared.Messages;
using dotRMDY.SyncSupport.Shared.Models;
using Microsoft.Extensions.Logging;

namespace dotRMDY.SyncSupport.Shared.Services.Implementations
{
	public class OperationHandlerService : IOperationHandlerService, IDisposable
	{
		private readonly ILogger<OperationHandlerService> _logger;
		private readonly IOperationService _operationService;
		private readonly IMessenger _messenger;
		private readonly IResolver _resolver;

		private readonly SemaphoreSlim _operationHandlingSemaphoreSlim = new(1, 1);

		public OperationHandlerService(
			ILogger<OperationHandlerService> logger,
			IOperationService operationService,
			IMessenger messenger,
			IResolver resolver)
		{
			_logger = logger;
			_operationService = operationService;
			_messenger = messenger;
			_resolver = resolver;

			_messenger.Register<OperationAddedMessage>(this, OperationAddedMessageHandler);
		}

		public virtual async Task<CallResult> HandlePendingOperations()
		{
			try
			{
				await _operationHandlingSemaphoreSlim.WaitAsync();

				var allPendingOperations = await _operationService.GetAllOperations();
				if (allPendingOperations.Count == 0)
				{
					return CallResult.CreateSuccess();
				}

				List<CallResult> operationCallResults = new(allPendingOperations.Count);
				foreach (var operation in allPendingOperations)
				{
					try
					{
						operationCallResults.Add(await HandleOperation(operation));
					}
					catch (Exception e)
					{
						_logger.LogError(e, "Error while handling operation {OperationId}", operation.Id);
						await MarkOperationAsFailed(operation);
					}
				}

				return CallResult.Combine(operationCallResults);
			}
			finally
			{
				_operationHandlingSemaphoreSlim.Release();
			}
		}

		public virtual Task MarkOperationAsFailed(Operation operation)
		{
			operation.LastSyncFailed = true;
			return _operationService.UpdateOperation(operation);
		}

		public void Dispose()
		{
			_messenger.Unregister<OperationAddedMessage>(this);
		}

		protected virtual void OperationAddedMessageHandler(object arg1, OperationAddedMessage arg2)
		{
			Task.Run(HandlePendingOperations);
		}

		protected virtual async Task<CallResult> HandleOperation<T>(T operation) where T : Operation
		{
			var constructedHandlerType = typeof(IOperationHandler<>).MakeGenericType(operation.GetType());
			if (_resolver.Resolve(constructedHandlerType) is not IOperationHandler<T> resolvedHandler)
			{
				throw new InvalidOperationException($"Could not resolve handler for operation of type {operation.GetType().FullName}");
			}

			var handlerCallResult = await resolvedHandler.HandleOperation(operation);
			return await ProcessOperationCallResult(handlerCallResult, operation);
		}

		protected virtual async Task<CallResult> ProcessOperationCallResult(CallResult callResult, Operation operation)
		{
			switch (callResult.Status)
			{
				case CallResultStatus.Success:
					await _operationService.DeleteOperation(operation);
					break;
				case CallResultStatus.Error:
					await MarkOperationAsFailed(operation);
					break;
			}

			return callResult;
		}
	}
}