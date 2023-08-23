using dotRMDY.SyncSupport.MvvmCross.Extensions;
using dotRMDY.SyncSupport.MvvmCross.UnitTests.TestHelpers;
using dotRMDY.SyncSupport.Services;
using dotRMDY.SyncSupport.Services.Implementations;
using FakeItEasy;
using MvvmCross.IoC;
using Xunit;

namespace dotRMDY.SyncSupport.MvvmCross.UnitTests.Extensions
{
	public class MvxIocProviderExtensionsTest
	{
		[Fact]
		public void RegisterAllComponentsCore()
		{
			// Arrange
			var mvxIocProvider = A.Fake<IMvxIoCProvider>();

			// Act
			mvxIocProvider.RegisterSyncSupportServices();

			// Assert
			mvxIocProvider.VerifyLazySingletonRegistration<IOperationService, OperationService>()
				.MustHaveHappenedOnceExactly();
			mvxIocProvider.VerifyLazySingletonRegistration<IOperationHandlerService, OperationHandlerService>()
				.MustHaveHappenedOnceExactly();
			mvxIocProvider.VerifyLazySingletonRegistration<IWebServiceHelper, WebServiceHelper>()
				.MustHaveHappenedOnceExactly();
		}
	}
}