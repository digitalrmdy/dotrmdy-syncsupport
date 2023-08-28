using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using dotRMDY.SyncSupport.Models;
using dotRMDY.SyncSupport.Services;
using dotRMDY.SyncSupport.Services.Implementations;
using dotRMDY.SyncSupport.UnitTests.TestHelpers.Models;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace dotRMDY.SyncSupport.UnitTests.Services
{
	public class CombinatorialOperationHandlerDelegationServiceTest : SutSupportingTest<CombinatorialOperationHandlerDelegationService>
	{
		private IOperationHandlerDelegationService _operationHandlerDelegationService1 = null!;
		private IOperationHandlerDelegationService _operationHandlerDelegationService2 = null!;

		protected override void SetupCustomSutDependencies(SutBuilder builder)
		{
			base.SetupCustomSutDependencies(builder);

			_operationHandlerDelegationService1 = A.Fake<IOperationHandlerDelegationService>();
			_operationHandlerDelegationService2 = A.Fake<IOperationHandlerDelegationService>();

			builder.AddDependency((IEnumerable<IOperationHandlerDelegationService>) new []
			{
				_operationHandlerDelegationService1,
				_operationHandlerDelegationService2
			});
		}

		[Fact]
		public async Task HandleOperation_HandledByFirstDelegationService()
		{
			// Arrange
			var operation = new OperationStub();

			var callResult = CallResult.CreateSuccess(HttpStatusCode.OK);
			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation))
				.Returns(callResult);

			// Act
			var result = await Sut.HandleOperation(operation);

			// Assert
			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _operationHandlerDelegationService2.HandleOperation(operation)).MustNotHaveHappened();

			result.Should().Be(callResult);
		}

		[Fact]
		public async Task HandleOperation_HandledBySecondDelegationService()
		{
			// Arrange
			var operation = new OperationStub();

			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation))
				.Throws<NotSupportedException>();

			var callResult = CallResult.CreateSuccess(HttpStatusCode.OK);
			A.CallTo(() => _operationHandlerDelegationService2.HandleOperation(operation))
				.Returns(callResult);

			// Act
			var result = await Sut.HandleOperation(operation);

			// Assert
			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _operationHandlerDelegationService2.HandleOperation(operation)).MustHaveHappenedOnceExactly();

			result.Should().Be(callResult);
		}

		[Fact]
		public async Task HandleOperation_UnhandledOperation()
		{
			// Arrange
			var operation = new UnhandledOperationStub();

			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation))
				.Throws<NotSupportedException>();

			A.CallTo(() => _operationHandlerDelegationService2.HandleOperation(operation))
				.Throws<NotSupportedException>();

			// Act
			var act = () => Sut.HandleOperation(operation);

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>()
				.WithMessage($"Operation type 'dotRMDY.SyncSupport.UnitTests.TestHelpers.Models.UnhandledOperationStub' is not supported.");
		}

		[Fact]
		public async Task HandleOperation_ExceptionDuringDelegation()
		{
			// Arrange
			var operation = new OperationStub();

			var exception = new Exception("TestException");
			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation))
				.Throws(exception);

			// Act
			var act = () => Sut.HandleOperation(operation);

			// Assert
			await act.Should().ThrowAsync<Exception>()
				.WithMessage("TestException");

			A.CallTo(() => _operationHandlerDelegationService1.HandleOperation(operation)).MustHaveHappenedOnceExactly();
			A.CallTo(() => _operationHandlerDelegationService2.HandleOperation(operation)).MustNotHaveHappened();
		}
	}
}