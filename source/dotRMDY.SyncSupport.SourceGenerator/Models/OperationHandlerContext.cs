namespace dotRMDY.SyncSupport.SourceGenerator.Models
{
	internal sealed class OperationHandlerContext
	{
		public OperationHandlerContext(string fullyQualifiedTypeName, string typeName)
		{
			FullyQualifiedTypeName = fullyQualifiedTypeName;
			TypeName = typeName;
		}

		public string FullyQualifiedTypeName { get; }
		public string TypeName { get; }

		public void Deconstruct(out string fullyQualifiedTypeName, out string typeName)
		{
			fullyQualifiedTypeName = FullyQualifiedTypeName;
			typeName = TypeName;
		}
	}
}