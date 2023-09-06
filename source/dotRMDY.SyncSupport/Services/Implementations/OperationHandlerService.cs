using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace dotRMDY.SyncSupport.Services.Implementations
{
	[PublicAPI]
	public class OperationHandlerService : IOperationHandlerService
	{
		private readonly ILogger<OperationHandlerService> _logger;
		private readonly IOperationService _operationService;
		private readonly IOperationHandlerDelegationService _operationHandlerDelegationService;

		private readonly SemaphoreSlim _operationHandlingSemaphoreSlim = new(1, 1);

		public OperationHandlerService(
			ILogger<OperationHandlerService> logger,
			IOperationService operationService,
			IOperationHandlerDelegationService operationHandlerDelegationService)
		{
			_logger = logger;
			_operationService = operationService;
			_operationHandlerDelegationService = operationHandlerDelegationService;
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
						var operationHandlerCallResult = await _operationHandlerDelegationService.HandleOperation(operation).ConfigureAwait(false);
						var processOperationCallResult = await ProcessOperationCallResult(operationHandlerCallResult, operation).ConfigureAwait(false);

						operationCallResults.Add(processOperationCallResult);
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