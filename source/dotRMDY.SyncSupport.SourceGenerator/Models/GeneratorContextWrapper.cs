namespace dotRMDY.SyncSupport.SourceGenerator.Models
{
	internal static class GeneratorContextWrapper
	{
		public static GeneratorContextWrapper<TContext> CreateContinue<TContext>(TContext context)
		{
			return new GeneratorContextWrapper<TContext>
			{
				CanContinue = true,
				Context = context
			};
		}

		public static GeneratorContextWrapper<TContext> CreateStop<TContext>(TContext? context = default)
		{
			return new GeneratorContextWrapper<TContext>
			{
				CanContinue = false,
				Context = context
			};
		}

		public static GeneratorContextWrapper<TContext> Create<TContext>() where TContext : new()
		{
			return CreateContinue(new TContext());
		}
	}

	internal sealed class GeneratorContextWrapper<T> : DiagnosticsWrapper
	{
		public bool CanContinue { get; set; }
		public T? Context { get; set; }

		public GeneratorContextWrapper<TNewContext> Merge<TNewContext>(DiagnosticsWrapper other, TNewContext newContext, bool canContinue = true)
		{
			var mergedContext = new GeneratorContextWrapper<TNewContext>
			{
				CanContinue = canContinue,
				Context = newContext
			};
			mergedContext.Diagnostics.AddRange(Diagnostics);
			mergedContext.Diagnostics.AddRange(other.Diagnostics);
			return mergedContext;
		}
	}
}