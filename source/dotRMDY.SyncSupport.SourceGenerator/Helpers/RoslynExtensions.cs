using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace dotRMDY.SyncSupport.SourceGenerator.Helpers
{
	internal static class RoslynExtensions
	{
		/// <summary>
		/// Returns the kind keyword corresponding to the specified declaration syntax node.
		/// </summary>
		public static string? GetTypeKindKeyword(this TypeDeclarationSyntax typeDeclaration)
		{
			switch (typeDeclaration.Kind())
			{
				case SyntaxKind.ClassDeclaration:
					return "class";
				default:
					Debug.Fail("unexpected syntax kind");
					return null;
			}
		}
	}
}