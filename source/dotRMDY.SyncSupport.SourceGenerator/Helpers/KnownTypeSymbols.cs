using Microsoft.CodeAnalysis;

namespace dotRMDY.SyncSupport.SourceGenerator.Helpers
{
	internal sealed class KnownTypeSymbols
	{
		private const string OPERATION_HANDLER_UNBOUND_BASETYPE_FULLNAME = "dotRMDY.SyncSupport.Handlers.IOperationHandler`1";
		private const string OPERATION_HANDLER_SERVICE_BASETYPE_FULLNAME = "dotRMDY.SyncSupport.Services.Implementations.OperationHandlerService";

		public KnownTypeSymbols(Compilation compilation)
		{
			Compilation = compilation;
		}

		public Compilation Compilation { get; }

		public INamedTypeSymbol? OperationHandlerUnboundBaseTypeSymbol => GetOrResolveType(OPERATION_HANDLER_UNBOUND_BASETYPE_FULLNAME, ref _operationHandlerUnboundBaseType);
		private Option<INamedTypeSymbol?> _operationHandlerUnboundBaseType;

		public INamedTypeSymbol? OperationHandlerServiceBaseTypeSymbol => GetOrResolveType(OPERATION_HANDLER_SERVICE_BASETYPE_FULLNAME, ref _operationHandlerServiceBaseType);
		private Option<INamedTypeSymbol?> _operationHandlerServiceBaseType;

		private INamedTypeSymbol? GetOrResolveType(string fullyQualifiedName, ref Option<INamedTypeSymbol?> field)
		{
			if (field.HasValue)
			{
				return field.Value;
			}

			var type = Compilation.GetTypeByMetadataName(fullyQualifiedName);
			field = new Option<INamedTypeSymbol?>(type);
			return type;
		}

		private readonly struct Option<T>
		{
			public readonly bool HasValue;
			public readonly T Value;

			public Option(T value)
			{
				HasValue = true;
				Value = value;
			}
		}
	}
}