using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotRMDY.SyncSupport.Shared.SourceGenerator.Models
{
	internal sealed class OperationHandlerServiceIntermediateContext
	{
		public OperationHandlerServiceIntermediateContext(ClassDeclarationSyntax operationHandlerServiceClassDeclaration, SemanticModel semanticModel)
		{
			OperationHandlerServiceClassDeclaration = operationHandlerServiceClassDeclaration;
			SemanticModel = semanticModel;
		}

		public ClassDeclarationSyntax OperationHandlerServiceClassDeclaration { get; }
		public SemanticModel SemanticModel { get; }

		public void Deconstruct(out ClassDeclarationSyntax classDeclarationSyntax, out SemanticModel semanticModel)
		{
			classDeclarationSyntax = OperationHandlerServiceClassDeclaration;
			semanticModel = SemanticModel;
		}
	}
}