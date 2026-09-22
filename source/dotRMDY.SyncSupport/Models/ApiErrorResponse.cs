namespace dotRMDY.SyncSupport.Models;

public class ApiErrorResponse
{
	public int? StatusCode { get; set; }
	public string? Message { get; set; }
	public string? Value { get; set; }
}