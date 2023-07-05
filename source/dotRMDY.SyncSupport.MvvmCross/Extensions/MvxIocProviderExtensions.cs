using dotRMDY.SyncSupport.Services;
using dotRMDY.SyncSupport.Services.Implementations;
using JetBrains.Annotations;
using MvvmCross.IoC;

namespace dotRMDY.SyncSupport.MvvmCross.Extensions
{
	[PublicAPI]
	public static class MvxIocProviderExtensions
	{
		public static IMvxIoCProvider RegisterSyncSupportServices(this IMvxIoCProvider iocProvider)
		{
			iocProvider.LazyConstructAndRegisterSingleton<IOperationService, OperationService>();
			iocProvider.LazyConstructAndRegisterSingleton<IOperationHandlerService, OperationHandlerService>();

			return iocProvider;
		}
	}
}