using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace dotRMDY.SyncSupport.SourceGenerator.Models
{
	internal sealed class OperationHandlerIntermediateContext
	{
		public OperationHandlerIntermediateContext(ImmutableArray<INamedTypeSymbol> allTypeInterfaces)
		{
			AllTypeInterfaces = allTypeInterfaces;
		}

		public ImmutableArray<INamedTypeSymbol> AllTypeInterfaces { get; }
	}
}