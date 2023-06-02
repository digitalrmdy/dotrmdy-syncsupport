using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotRMDY.Components.Shared.Helpers;
using dotRMDY.Components.Shared.Services;
using dotRMDY.DataStorage.Abstractions.Repositories;
using dotRMDY.SyncSupport.Shared.Messages;
using dotRMDY.SyncSupport.Shared.Models;
using Microsoft.Extensions.Logging;

namespace dotRMDY.SyncSupport.Shared.Services.Implementations
{
	public class OperationService : IOperationService, INeedAsyncInitialization
	{
		private readonly ILogger<OperationService> _logger;
		private readonly IRepository<Operation> _operationRepository;
		private readonly IMessenger _messenger;
		private readonly ITimeKeeper _timekeeper;

		private readonly Queue<Operation> _inMemoryOperationQueue;
		private readonly SemaphoreSlim _initSemaphoreSlim = new(1, 1);

		private bool _initialized;

		public OperationService(
			ILogger<OperationService> logger,
			IRepository<Operation> operationRepository,
			IMessenger messenger,
			ITimeKeeper timekeeper)
		{
			_logger = logger;
			_operationRepository = operationRepository;
			_messenger = messenger;
			_timekeeper = timekeeper;

			_inMemoryOperationQueue = new Queue<Operation>();
		}

		public virtual Task AddOperation<TOperation>(Action<TOperation> enrichOperationAction)
			where TOperation : Operation, new()
		{
			var operation = new TOperation { CreationTimestamp = _timekeeper.NowOffset };

			enrichOperationAction(operation);

			return AddOperation(operation);
		}

		public virtual async Task<IList<Operation>> GetAllOperations()
		{
			var operationsEnumerable = await _operationRepository.GetAll();
			return operationsEnumerable.OrderBy(x => x.CreationTimestamp).ToList();
		}

		public virtual Task UpdateOperation(Operation operation)
		{
			return _operationRepository.UpsertItem(operation);
		}

		public virtual Task DeleteOperation(Operation operation)
		{
			return _operationRepository.DeleteItem(operation.Id);
		}

		public async Task Initialize()
		{
			try
			{
				if (_initialized)
				{
					return;
				}

				await _initSemaphoreSlim.WaitAsync();

				if (_initialized)
				{
					return;
				}

				_initialized = true;

				await HandleInMemoryQueue();
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error initializing OperationService");
			}
			finally
			{
				_initSemaphoreSlim.Release();
			}
		}

		protected virtual async Task AddOperation(Operation operation)
		{
			if (_initialized)
			{
				_inMemoryOperationQueue.Enqueue(operation);
				return;
			}

			await _operationRepository.UpsertItem(operation);

			_messenger.Send(new OperationAddedMessage());
		}

		protected virtual async Task HandleInMemoryQueue()
		{
			while (_inMemoryOperationQueue.TryDequeue(out var operation))
			{
				await AddOperation(operation);
			}
		}
	}
}