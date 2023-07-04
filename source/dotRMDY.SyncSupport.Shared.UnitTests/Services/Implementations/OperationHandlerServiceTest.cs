using System;
using System.Net;
using System.Threading.Tasks;
using dotRMDY.Components.Shared.Services;
using dotRMDY.SyncSupport.Shared.Handlers;
using dotRMDY.SyncSupport.Shared.Models;
using dotRMDY.SyncSupport.Shared.Services;
using dotRMDY.SyncSupport.Shared.Services.Implementations;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace dotRMDY.SyncSupport.Shared.UnitTests.Services.Implementations
{
	public class OperationHandlerServiceTest : SutSupportingTest<OperationHandlerServiceTest.TestOperationHandlerService>
	{
		private IOperationService _operationService = null!;
		private IResolver _resolver = null!;

		protected override void SetupCustomSutDependencies(SutBuilder builder)
		{
			base.SetupCustomSutDependencies(builder);

			_operationService = builder.AddFakedDependency<IOperationService>();
			_resolver = builder.AddFakedDependency<IResolver>();
		}

		[Fact]
		public async Task HandlePendingOperations_NoOperations()
		{
			// Arrange
			A.CallTo(() => _operationService.GetAllOperations())
				.Returns(Array.Empty<Operation>());

			// Act
			var result = await Sut.HandlePendingOperations();

			// Assert
			A.CallTo(() => _operationService.GetAllOperations()).MustHaveHappenedOnceExactly();

			result.Successful().Should().BeTrue();
		}

		[Fact]
		public async Task HandlePendingOperations_OneOperation()
		{
			// Arrange
			var operation = new OperationStub { CreationTimestamp = 17.May(2023).At(21, 40) };
			A.CallTo(() => _operationService.GetAllOperations())
				.Returns(new Operation[] { operation });

			var operationStubHandler = new OperationStubHandler();
			/*A.CallTo(() => operationStubHandler.HandleOperation(An<OperationStub>._))
				.Returns(CallResult.CreateSuccess(HttpStatusCode.OK));*/
			var operationStubHandlerType = typeof(IOperationHandler<OperationStub>);
			A.CallTo(() => _resolver.Resolve(operationStubHandlerType))
				.Invokes(call =>
				{
					var requestedType = call.Arguments.Get<Type>(0);
					requestedType.Should().Be(operationStubHandlerType);
				})
				.Returns(operationStubHandler);

			// Act
			var result = await Sut.HandlePendingOperations();

			// Assert
			A.CallTo(() => _operationService.GetAllOperations()).MustHaveHappenedOnceExactly();

			result.Successful().Should().BeTrue();
		}

		[Fact]
		public async Task MarkAsFailed()
		{
			// Arrange
			var operation = new OperationStub { CreationTimestamp = 17.May(2023).At(21, 40) };

			// Act
			await Sut.MarkOperationAsFailed(operation);

			// Assert
			operation.LastSyncFailed.Should().BeTrue();

			A.CallTo(() => _operationService.UpdateOperation(operation)).MustHaveHappened();
		}

		public class TestOperationHandlerService : OperationHandlerService
		{
			public TestOperationHandlerService(
				ILogger<TestOperationHandlerService> logger,
				IOperationService operationService,
				IMessenger messenger,
				IResolver resolver)
				: base(logger, operationService, messenger, resolver)
			{
			}

			protected override Task<CallResult> HandleOperationRaw(Operation operation)
			{
				return operation switch
				{
					OperationStub operationStub => HandleOperation(operationStub),
					_ => base.HandleOperationRaw(operation)
				};
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public class OperationStub : Operation
		{
		}

		public class OperationStubHandler : IOperationHandler<OperationStub>
		{
			public Task<CallResult> HandleOperation<TOperation>(TOperation operation) where TOperation : Operation
			{
				return operation is not OperationStub operationStub
					? throw new ArgumentException($"Operation must be of type {nameof(OperationStub)}", nameof(operation))
					: HandleOperation(operationStub);
			}

			public Task<CallResult> HandleOperation(OperationStub operation)
			{
				return Task.FromResult(CallResult.CreateSuccess(HttpStatusCode.OK));
			}
		}
	}
}