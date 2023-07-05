using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;

namespace dotRMDY.SyncSupport.SourceGenerator.Models
{
	internal sealed class OperationHandlerServiceContext
	{
		public OperationHandlerServiceContext(ISymbol typeSymbol, List<string> typeDeclarations)
		{
			Namespace = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null;
			FullTypeName = typeSymbol.ToDisplayString();
			TypeDeclarations = typeDeclarations.AsReadOnly();
		}

		public string? Namespace { get; }
		public string FullTypeName { get; }
		public ReadOnlyCollection<string> TypeDeclarations { get; }

		public void Deconstruct(out string? @namespace, out string fullTypeName, out ReadOnlyCollection<string> typeDeclarations)
		{
			@namespace = Namespace;
			fullTypeName = FullTypeName;
			typeDeclarations = TypeDeclarations;
		}
	}
}