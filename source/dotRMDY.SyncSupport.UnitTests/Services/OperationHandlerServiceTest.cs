using System;
using System.Net;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using dotRMDY.SyncSupport.Services;
using dotRMDY.SyncSupport.Services.Implementations;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace dotRMDY.SyncSupport.UnitTests.Services
{
	public class OperationHandlerServiceTest : SutSupportingTest<OperationHandlerService>
	{
		private IOperationService _operationService = null!;
		private IOperationHandlerDelegationService _operationHandlerDelegationService = null!;

		protected override void SetupCustomSutDependencies(SutBuilder builder)
		{
			base.SetupCustomSutDependencies(builder);

			_operationService = builder.AddFakedDependency<IOperationService>();
			_operationHandlerDelegationService = builder.AddFakedDependency<IOperationHandlerDelegationService>();
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

			A.CallTo(() => _operationHandlerDelegationService.HandleOperation(A<Operation>._)).MustNotHaveHappened();

			result.Successful().Should().BeTrue();
		}

		[Fact]
		public async Task HandlePendingOperations_OneOperation()
		{
			// Arrange
			var operation = new OperationStub { CreationTimestamp = 17.May(2023).At(21, 40) };
			A.CallTo(() => _operationService.GetAllOperations())
				.Returns(new Operation[] { operation });

			var callResult = CallResult.CreateSuccess(HttpStatusCode.OK);
			A.CallTo(() => _operationHandlerDelegationService.HandleOperation(operation))
				.Returns(callResult);

			// Act
			var result = await Sut.HandlePendingOperations();

			// Assert
			A.CallTo(() => _operationService.GetAllOperations()).MustHaveHappenedOnceExactly();

			A.CallTo(() => _operationHandlerDelegationService.HandleOperation(operation)).MustHaveHappenedOnceExactly();

			result.Successful().Should().BeTrue();
		}

		[Fact]
		public async Task HandlePendingOperations_OneOperation_Throws()
		{
			// Arrange
			var operation = new OperationStub { CreationTimestamp = 17.May(2023).At(21, 40) };
			A.CallTo(() => _operationService.GetAllOperations())
				.Returns(new Operation[] { operation });

			A.CallTo(() => _operationHandlerDelegationService.HandleOperation(An<Operation>._))
				.Throws<Exception>();

			// Act
			var result = await Sut.HandlePendingOperations();

			// Assert
			A.CallTo(() => _operationService.GetAllOperations()).MustHaveHappenedOnceExactly();

			A.CallTo(() => _operationHandlerDelegationService.HandleOperation(operation)).MustHaveHappenedOnceExactly();

			result.Successful().Should().BeFalse();
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

		// ReSharper disable once MemberCanBePrivate.Global
		public class OperationStub : Operation
		{
		}
	}
}