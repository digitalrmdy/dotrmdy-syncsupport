using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using dotRMDY.SyncSupport.Shared.SourceGenerator.Helpers;
using dotRMDY.SyncSupport.Shared.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotRMDY.SyncSupport.Shared.SourceGenerator;

[Generator]
public class HandleOperationIncrementalGenerator : IIncrementalGenerator
{
	private static readonly AssemblyName _assemblyName = typeof(HandleOperationIncrementalGenerator).Assembly.GetName();
	private static readonly string _generatedCodeAttribute = $@"[System.CodeDom.Compiler.GeneratedCode(""{_assemblyName.Name}"", ""{_assemblyName.Version}"")]";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var knownTypeSymbolsIncrementalValueProvider = context.CompilationProvider
			.Select(static (compilation, _) => new KnownTypeSymbols(compilation));

		var operationHandlerIncrementalValuesProvider = context.SyntaxProvider.CreateSyntaxProvider(
				IsSyntaxForGeneration,
				Step1FilterAndTransformForOperationHandlers)
			.Where(x => x.CanContinue)
			.Combine(knownTypeSymbolsIncrementalValueProvider)
			.Select(Step2FilterAndTransformForOperationHandlers)
			.Where(x => x.CanContinue);

		var operationHandlerServiceIncrementalValuesProvider = context.SyntaxProvider.CreateSyntaxProvider(
				IsSyntaxForGeneration,
				Step1FilterAndTransformForOperationHandlerServices)
			.Where(x => x.CanContinue)
			.Combine(knownTypeSymbolsIncrementalValueProvider)
			.Select(Step2FilterAndTransformForOperationHandlerServices)
			.Where(x => x.CanContinue)
			.Collect();

		var tadaam = operationHandlerServiceIncrementalValuesProvider.Combine(operationHandlerIncrementalValuesProvider.Collect());

		context.RegisterSourceOutput(tadaam, GenerateSource);
	}

	private static bool IsSyntaxForGeneration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
	{
		if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax)
		{
			return false;
		}

		return classDeclarationSyntax.BaseList?.Types.Count > 0;
	}

	private static GeneratorContextWrapper<OperationHandlerIntermediateContext> Step1FilterAndTransformForOperationHandlers(
		GeneratorSyntaxContext syntaxContext, CancellationToken ct)
	{
		var semanticModel = syntaxContext.SemanticModel;
		var classDeclarationSyntax = (ClassDeclarationSyntax) syntaxContext.Node;

		var concreteClassSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, classDeclarationSyntax) as INamedTypeSymbol;

		if (concreteClassSymbol!.IsAbstract)
		{
			return GeneratorContextWrapper.CreateStop<OperationHandlerIntermediateContext>();
		}

		var allTypeInterfaces = concreteClassSymbol.AllInterfaces;
		if (allTypeInterfaces.Length == 0)
		{
			return GeneratorContextWrapper.CreateStop<OperationHandlerIntermediateContext>();
		}

		return GeneratorContextWrapper.CreateContinue(new OperationHandlerIntermediateContext(allTypeInterfaces));
	}

	private static GeneratorContextWrapper<OperationHandlerContext> Step2FilterAndTransformForOperationHandlers(
		(GeneratorContextWrapper<OperationHandlerIntermediateContext> Left, KnownTypeSymbols Right) valueTuple,
		CancellationToken ct)
	{
		var baseTypeSymbol = valueTuple.Right.OperationHandlerUnboundBaseTypeSymbol;
		var specializedImplementedInterfaces = valueTuple.Left.Context!.AllTypeInterfaces;

		var operationHandlerOfTTypeSymbol = specializedImplementedInterfaces.FirstOrDefault(implementedInterface =>
			SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, baseTypeSymbol!));
		if (operationHandlerOfTTypeSymbol == null)
		{
			return GeneratorContextWrapper.CreateStop<OperationHandlerContext>();
		}

		var operationTypeSymbol = operationHandlerOfTTypeSymbol.TypeArguments[0];
		var fullyQualifiedTypeName = operationTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		var typeName = operationTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		typeName = char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);

		return GeneratorContextWrapper.CreateContinue(new OperationHandlerContext(fullyQualifiedTypeName, typeName));
	}

	private static GeneratorContextWrapper<OperationHandlerServiceIntermediateContext> Step1FilterAndTransformForOperationHandlerServices(
		GeneratorSyntaxContext syntaxContext, CancellationToken ct)
	{
		var semanticModel = syntaxContext.SemanticModel;
		var classDeclarationSyntax = (ClassDeclarationSyntax) syntaxContext.Node;

		return (classDeclarationSyntax.BaseList?.Types.Count > 0)
			? GeneratorContextWrapper.CreateContinue(new OperationHandlerServiceIntermediateContext(classDeclarationSyntax, semanticModel))
			: GeneratorContextWrapper.CreateStop<OperationHandlerServiceIntermediateContext>();
	}

	private static GeneratorContextWrapper<OperationHandlerServiceContext> Step2FilterAndTransformForOperationHandlerServices(
		(GeneratorContextWrapper<OperationHandlerServiceIntermediateContext> Left, KnownTypeSymbols Right) valueTuple, CancellationToken ct)
	{
		var (operationHandlerServiceClassDeclaration, semanticModel) = valueTuple.Left.Context!;
		var baseTypeSymbol = valueTuple.Right.OperationHandlerServiceBaseTypeSymbol;

		var concreteClassTypeSymbol = semanticModel.GetDeclaredSymbol(operationHandlerServiceClassDeclaration, ct);
		Debug.Assert(concreteClassTypeSymbol != null);

		var currentBaseTypeSymbol = concreteClassTypeSymbol!.BaseType;
		while (currentBaseTypeSymbol != null && currentBaseTypeSymbol.SpecialType != SpecialType.System_Object)
		{
			if (SymbolEqualityComparer.Default.Equals(currentBaseTypeSymbol, baseTypeSymbol!))
			{
				if (TryGetNestedTypeDeclarations(operationHandlerServiceClassDeclaration, semanticModel, ct, out var typeDeclarations))
				{
					return GeneratorContextWrapper.CreateContinue(new OperationHandlerServiceContext(concreteClassTypeSymbol, typeDeclarations!));
				}

				return GeneratorContextWrapper.CreateStop<OperationHandlerServiceContext>();
			}

			currentBaseTypeSymbol = currentBaseTypeSymbol.BaseType;
		}

		return GeneratorContextWrapper.CreateStop<OperationHandlerServiceContext>();
	}

	private static void GenerateSource(
		SourceProductionContext productionContext,
		(ImmutableArray<GeneratorContextWrapper<OperationHandlerServiceContext>> Left, ImmutableArray<GeneratorContextWrapper<OperationHandlerContext>> Right) sourceInput)
	{
		var (@namespace, fullTypeName, typeDeclarations) = sourceInput.Left.First().Context!;
		var operationTypeInfo = sourceInput.Right;

		var sourceWriter = new SourceWriter();
		sourceWriter.WriteLine("// <auto-generated/>");
		sourceWriter.WriteLine();

		if (@namespace != null)
		{
			sourceWriter.WriteLine($"namespace {@namespace}");
			sourceWriter.WriteLine('{');
			sourceWriter.Indentation++;
		}

		sourceWriter.WriteLine(_generatedCodeAttribute);

		foreach (var typeDeclaration in typeDeclarations)
		{
			sourceWriter.WriteLine(typeDeclaration);
			sourceWriter.WriteLine('{');
			sourceWriter.Indentation++;
		}

		sourceWriter.WriteLine("protected override global::System.Threading.Tasks.Task<global::dotRMDY.SyncSupport.Shared.Models.CallResult> HandleOperationRaw(global::dotRMDY.SyncSupport.Shared.Models.Operation operation)");
		sourceWriter.WriteLine('{');
		sourceWriter.Indentation++;

		sourceWriter.WriteLine("switch (operation)");
		sourceWriter.WriteLine('{');
		sourceWriter.Indentation++;

		foreach (var variable in operationTypeInfo)
		{
			var (fullyQualifiedOperationTypeName, operationTypeName) = variable.Context!;
			sourceWriter.WriteLine($"case {fullyQualifiedOperationTypeName} {operationTypeName}:");
			sourceWriter.Indentation++;
			sourceWriter.WriteLine($"return HandleOperation({operationTypeName});");
			sourceWriter.Indentation--;
		}

		sourceWriter.WriteLine("default:");
		sourceWriter.Indentation++;
		sourceWriter.WriteLine("return base.HandleOperationRaw(operation);");
		sourceWriter.Indentation--;

		while (sourceWriter.Indentation > 0)
		{
			sourceWriter.Indentation--;
			sourceWriter.WriteLine('}');
		}

		productionContext.AddSource($"{fullTypeName}.g.cs", sourceWriter.ToSourceText());
	}

	private static bool TryGetNestedTypeDeclarations(
		ClassDeclarationSyntax contextClassSyntax,
		SemanticModel semanticModel,
		CancellationToken cancellationToken,
		out List<string>? typeDeclarations)
	{
		typeDeclarations = null;

		for (TypeDeclarationSyntax? currentType = contextClassSyntax; currentType != null; currentType = currentType.Parent as TypeDeclarationSyntax)
		{
			StringBuilder stringBuilder = new();
			var isPartialType = false;

			foreach (var modifier in currentType.Modifiers)
			{
				stringBuilder.Append(modifier.Text);
				stringBuilder.Append(' ');
				isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
			}

			if (!isPartialType)
			{
				typeDeclarations = null;
				return false;
			}

			stringBuilder.Append(currentType.GetTypeKindKeyword());
			stringBuilder.Append(' ');

			var typeSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, currentType, cancellationToken);
			Debug.Assert(typeSymbol != null);

			var typeName = typeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
			stringBuilder.Append(typeName);

			(typeDeclarations ??= new()).Add(stringBuilder.ToString());
		}

		Debug.Assert(typeDeclarations?.Count > 0);
		return true;
	}
}