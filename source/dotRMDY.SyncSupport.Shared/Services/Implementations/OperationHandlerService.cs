using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.Components.Shared.Services;
using dotRMDY.SyncSupport.Shared.Handlers;
using dotRMDY.SyncSupport.Shared.Messages;
using dotRMDY.SyncSupport.Shared.Models;
using Microsoft.Extensions.Logging;

namespace dotRMDY.SyncSupport.Shared.Services.Implementations
{
	public abstract class OperationHandlerService : IOperationHandlerService, IDisposable
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
					return CallResult.CreateSuccess(HttpStatusCode.OK);
				}

				List<CallResult> operationCallResults = new(allPendingOperations.Count);
				foreach (var operation in allPendingOperations)
				{
					try
					{
						operationCallResults.Add(await HandleOperationRaw(operation));
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error while handling operation {OperationId}", operation.Id);

						await MarkOperationAsFailed(operation);
						operationCallResults.Add(CallResult.CreateError(new CallResultError(ex)));
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

		protected abstract Task<CallResult> HandleOperationRaw(Operation operation);

		protected virtual async Task<CallResult> HandleOperation<TOperation>(TOperation operation) where TOperation : Operation
		{
			var resolvedHandler = _resolver.Resolve<IOperationHandler<TOperation>>();
			if (resolvedHandler is null)
			{
				throw new InvalidOperationException($"Could not resolve handler for operation of type {operation.GetType().FullName}");
			}

			var handlerCallResult = await resolvedHandler.HandleOperation(operation).ConfigureAwait(false);
			return await ProcessOperationCallResult(handlerCallResult, operation).ConfigureAwait(false);
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