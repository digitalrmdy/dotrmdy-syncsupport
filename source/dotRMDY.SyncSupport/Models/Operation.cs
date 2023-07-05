using System;
using dotRMDY.DataStorage.Abstractions.Models;

namespace dotRMDY.SyncSupport.Models
{
	public abstract class Operation : IRepositoryBaseEntity
	{
		protected Operation() => Id = Guid.NewGuid().ToString();

		public string Id { get; set; }

		public DateTimeOffset CreationTimestamp { get; set; }

		public bool LastSyncFailed { get; set; }
	}
}