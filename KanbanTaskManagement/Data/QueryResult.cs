using KanbanTaskManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KanbanTaskManagement.Data;

public record QueryStatus {
	public ActionResult? ErrorStatus { get; protected init; }
	public bool Ok { get; protected init; } = false;
}

public record ReadQueryResult<T> : QueryStatus {
	public T? Result { get; protected init; }

	public static implicit operator ReadQueryResult<T>(ActionResult errorStatus) => new() { ErrorStatus = errorStatus };
	public static implicit operator ReadQueryResult<T>(T? result) {
		if(result == null) {
			return new NotFoundResult();
		} else {
			return new() {
				Result = result,
				Ok = true
			};
		}
	}
}

public record UpdateQueryResult<T> : QueryStatus {
	public UpdateResult? Result { get; set; }

	public static implicit operator UpdateQueryResult<T>(ActionResult errorStatus) => new() { ErrorStatus = errorStatus };
	public static implicit operator UpdateQueryResult<T>(UpdateResult result) {
		if (!result.IsAcknowledged) {
			return new BadRequestResult();
		} else if(result.MatchedCount <= 0) {
			return new NotFoundResult();
		} else {
			return new() {
				Result = result,
				Ok = true
			};
		}
	}
}