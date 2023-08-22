using System;
using dotRMDY.DataStorage.Abstractions.Models;

namespace dotRMDY.SyncSupport.Models
{
	public abstract class Operation : IRepositoryBaseEntity
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public DateTimeOffset CreationTimestamp { get; set; }

		public bool LastSyncFailed { get; set; }
	}
}