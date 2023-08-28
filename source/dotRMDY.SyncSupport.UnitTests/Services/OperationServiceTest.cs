using System.Collections.Generic;
using System.Threading.Tasks;
using dotRMDY.Components.Services;
using dotRMDY.DataStorage.Abstractions.Repositories;
using dotRMDY.SyncSupport.Messages;
using dotRMDY.SyncSupport.Models;
using dotRMDY.SyncSupport.Services.Implementations;
using dotRMDY.SyncSupport.UnitTests.TestHelpers.Models;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace dotRMDY.SyncSupport.UnitTests.Services
{
	public class OperationServiceTest : SutSupportingTest<OperationService>
	{
		private IRepository<Operation> _operationRepository = null!;
		private IMessenger _messenger = null!;
		private ITimeKeeper _timeKeeper = null!;

		protected override void SetupCustomSutDependencies(SutBuilder builder)
		{
			base.SetupCustomSutDependencies(builder);

			_operationRepository = builder.AddFakedDependency<IRepository<Operation>>();
			_messenger = builder.AddFakedDependency<IMessenger>();
			_timeKeeper = builder.AddFakedDependency<ITimeKeeper>();
		}

		[Fact]
		public async Task AddOperation()
		{
			// Arrange
			await Sut.Initialize();

			OperationStub? capturedOperation = null;
			var enrichmentAction = (OperationStub op) =>
			{
				op.HasBeenCalled = true;
				capturedOperation = op;
			};

			// Act
			await Sut.AddOperation(enrichmentAction);

			// Assert
			capturedOperation.Should().NotBeNull();
			capturedOperation!.HasBeenCalled.Should().BeTrue();

			A.CallTo(() => _operationRepository.UpsertItem(capturedOperation))
				.MustHaveHappenedOnceExactly();
			A.CallTo(() => _messenger.Send(An<OperationAddedMessage>._))
				.MustHaveHappenedOnceExactly();
		}

		[Fact]
		public async Task AddOperation_NotInitialized()
		{
			// Arrange
			A.CallTo(() => _timeKeeper.NowOffset).Returns(28.August(2023));

			OperationStub? capturedOperation = null;
			var enrichmentAction = (OperationStub op) =>
			{
				op.HasBeenCalled = true;
				capturedOperation = op;
			};

			// Act
			await Sut.AddOperation(enrichmentAction);

			// Assert
			capturedOperation.Should().NotBeNull();
			capturedOperation!.HasBeenCalled.Should().BeTrue();

			A.CallTo(() => _operationRepository.UpsertItem(An<Operation>._))
				.MustNotHaveHappened();
			A.CallTo(() => _messenger.Send(An<OperationAddedMessage>._))
				.MustNotHaveHappened();
		}

		[Fact]
		public async Task GetAllOperations()
		{
			// Arrange
			var operation1 = new OperationStub { CreationTimestamp = 28.August(2023) };
			var operation2 = new OperationStub { CreationTimestamp = 29.August(2023) };

			A.CallTo(() => _operationRepository.GetAll())
				.Returns(new Operation[] { operation2, operation1 });

			// Act
			var result = await Sut.GetAllOperations();

			// Assert
			result.Should().ContainInConsecutiveOrder(operation1, operation2);
		}

		[Fact]
		public async Task UpdateOperation()
		{
			// Arrange
			var operation = new OperationStub { CreationTimestamp = 28.August(2023) };

			// Act
			await Sut.UpdateOperation(operation);

			// Assert
			A.CallTo(() => _operationRepository.UpsertItem(operation))
				.MustHaveHappenedOnceExactly();
		}

		[Fact]
		public async Task DeleteOperation()
		{
			// Arrange
			var operation = new OperationStub {
				Id = "OperationId",
				CreationTimestamp = 28.August(2023)
			};

			// Act
			await Sut.DeleteOperation(operation);

			// Assert
			A.CallTo(() => _operationRepository.DeleteItem("OperationId"))
				.MustHaveHappenedOnceExactly();
		}

		[Fact]
		public async Task Initialize_DequeuesPendingOperations()
		{
			// Arrange
			var inMemoryQueue = Sut.GetField<OperationService, Queue<Operation>>("_inMemoryOperationQueue")!;
			inMemoryQueue.Enqueue(new OperationStub { CreationTimestamp = 28.August(2023) });

			// Act
			await Sut.Initialize();

			// Assert
			A.CallTo(() => _operationRepository.UpsertItem(An<Operation>._)).MustHaveHappenedOnceExactly();

			inMemoryQueue.Should().BeEmpty();
		}
	}
}