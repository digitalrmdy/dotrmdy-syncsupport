using System.Linq;

namespace dotRMDY.SyncSupport.Models
{
	public class CombinedCallResultError : CallResultError
	{
		public CombinedCallResultError(params CallResultError[] callResultErrors)
			: base(string.Join("|", callResultErrors.Where(cre => cre != null).Select(cre => cre.Code)),
				string.Join("|", callResultErrors.Where(cre => cre != null).Select(cre => cre.Message)),
				string.Join("|", callResultErrors.Where(cre => cre != null).Select(cre => cre.TechnicalMessage)))
		{
		}
	}
}