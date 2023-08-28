using dotRMDY.SyncSupport.Models;

namespace dotRMDY.SyncSupport.UnitTests.TestHelpers.Models
{
	public class OperationStub : Operation
	{
		public bool HasBeenCalled { get; set; }
	}
}