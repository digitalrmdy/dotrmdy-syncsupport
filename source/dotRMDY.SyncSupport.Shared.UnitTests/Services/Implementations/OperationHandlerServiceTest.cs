using System;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Shared.Models;
using dotRMDY.SyncSupport.Shared.Services;
using dotRMDY.SyncSupport.Shared.Services.Implementations;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace dotRMDY.SyncSupport.Shared.UnitTests.Services.Implementations
{
	public class OperationHandlerServiceTest : SutSupportingTest<OperationHandlerService>
	{
		private IOperationService _operationService = null!;

		protected override void SetupCustomSutDependencies(SutBuilder builder)
		{
			base.SetupCustomSutDependencies(builder);

			_operationService = builder.AddFakedDependency<IOperationService>();
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

		private class OperationStub : Operation
		{
		}
	}
}