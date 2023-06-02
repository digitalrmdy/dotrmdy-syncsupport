using System;
using FakeItEasy;
using FakeItEasy.Configuration;
using MvvmCross.IoC;

namespace dotRMDY.SyncSupport.MvvmCross.UnitTests.TestHelpers
{
	public static class MvxIocProviderTestExtensions
	{
		public static IVoidArgumentValidationConfiguration VerifyLazySingletonRegistration<TInterface, TImplementation>(
			this IMvxIoCProvider iocProvider)
			where TInterface : class
			where TImplementation : class, TInterface
		{
			return A.CallTo(() => iocProvider.RegisterSingleton(A<Func<TInterface>>
				.That
				.Matches(func => func() is TImplementation)));
		}

		public static IVoidArgumentValidationConfiguration VerifySingletonRegistration<TInterface, TImplementation>(
			this IMvxIoCProvider iocProvider)
			where TInterface : class
			where TImplementation : class, TInterface
		{
			return A.CallTo(() => iocProvider.RegisterSingleton<TInterface>(A<TImplementation>._));
		}
	}
}