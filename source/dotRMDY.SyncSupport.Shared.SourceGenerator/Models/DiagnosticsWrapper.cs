using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace dotRMDY.SyncSupport.Shared.SourceGenerator.Models
{
	internal abstract class DiagnosticsWrapper
	{
		public List<Diagnostic> Diagnostics { get; } = new();
	}
}