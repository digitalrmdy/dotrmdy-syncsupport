using System;
using System.Text.Json;
using dotRMDY.SyncSupport.Models;
using Microsoft.Extensions.Logging;

namespace dotRMDY.SyncSupport.Extensions;

public static class CallResultErrorExtension
{
	public static T? TryGetErrorData<T>(this CallResultError? callResult, ILogger logger) where T : class
	{
		if (string.IsNullOrWhiteSpace(callResult?.ApiException?.Content))
		{
			return null;
		}
		try
		{
			var content = JsonSerializer.Deserialize<T>(callResult.ApiException.Content);
			return content;
		}
		catch (Exception e)
		{
			logger.LogWarning(e, "Error deserialization failed | Type: {Type} | CallResultErrorExtension.TryGetErrorData", typeof(T).Name);
			return null;
		}
	}
}