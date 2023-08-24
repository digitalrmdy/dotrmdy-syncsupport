using System;
using System.Net;
using System.Threading.Tasks;
using dotRMDY.Components.Services;
using dotRMDY.SyncSupport.Handlers;
using dotRMDY.SyncSupport.Models;
using dotRMDY.SyncSupport.Services.Implementations;
using dotRMDY.TestingTools;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace dotRMDY.SyncSupport.UnitTests.Services
{
	public class OperationHandlerDelegationServiceTest : SutSupportingTest<OperationHandlerDelegationServiceTest.TestOperationHandlerDelegationService>
	{
		private IResolver _resolver = null!;

		protected override void SetupCustomSutDependencies(SutBuilder builder)
		{
			base.SetupCustomSutDependencies(builder);

			_resolver = builder.AddFakedDependency<IResolver>(true);
		}

		[Fact]
		public async Task HandleOperation()
		{
			// Arrange
			var operationStubHandler = A.Fake<IOperationHandler<OperationStub>>();
			A.CallTo(() => operationStubHandler.HandleOperation(A<OperationStub>._))
				.Returns(CallResult.CreateSuccess(HttpStatusCode.OK));

			A.CallTo(() => _resolver.Resolve<IOperationHandler<OperationStub>>())
				.Returns(operationStubHandler);

			// Act
			var handlerCallResult = await Sut.HandleOperation(new OperationStub());

			// Assert
			handlerCallResult.Successful().Should().BeTrue();
		}

		[Fact]
		public async Task HandleOperation_NoHandlerResolution()
		{
			// Arrange
			A.CallTo(() => _resolver.Resolve<IOperationHandler<OperationStub>>())!
				.Returns(null);

			// Act
			var act = () => Sut.HandleOperation(new OperationStub());

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Could not resolve handler for operation of type dotRMDY.SyncSupport.UnitTests.Services.OperationHandlerDelegationServiceTest+OperationStub");
		}

		public class TestOperationHandlerDelegationService : OperationHandlerDelegationServiceBase
		{
			public TestOperationHandlerDelegationService(IResolver resolver) : base(resolver)
			{
			}

			public override Task<CallResult> HandleOperation(Operation operation)
			{
				return operation switch
				{
					OperationStub operationStub => HandleOperation(operationStub),
					_ => base.HandleOperation(operation)
				};
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public class OperationStub : Operation
		{
		}
	}
}