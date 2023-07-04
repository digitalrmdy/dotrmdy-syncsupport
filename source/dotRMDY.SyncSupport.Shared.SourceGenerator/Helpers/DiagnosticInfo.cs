using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace dotRMDY.SyncSupport.Shared.SourceGenerator.Helpers
{
	/// <summary>
	/// Descriptor for diagnostic instances using structural equality comparison.
	/// Provides a work-around for https://github.com/dotnet/roslyn/issues/68291.
	/// </summary>
	public readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
	{
		public required DiagnosticDescriptor Descriptor { get; init; }
		public required object?[] MessageArgs { get; init; }
		public required Location? Location { get; init; }

		public Diagnostic CreateDiagnostic() => Diagnostic.Create(Descriptor, Location, MessageArgs);

		public override readonly bool Equals(object? obj) => obj is DiagnosticInfo info && Equals(info);

		public readonly bool Equals(DiagnosticInfo other)
		{
			return Descriptor.Equals(other.Descriptor) &&
			       MessageArgs.SequenceEqual(other.MessageArgs) &&
			       Location == other.Location;
		}

		public override readonly int GetHashCode()
		{
			var hashCode = Descriptor.GetHashCode();
			foreach (var messageArg in MessageArgs)
			{
				hashCode ^= messageArg?.GetHashCode() ?? 0;
			}

			hashCode ^= Location?.GetHashCode() ?? 0;
			return hashCode;
		}
	}
}