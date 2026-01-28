using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoQueryApiDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersCursorController : ControllerBase
{
    private readonly IQueryProcessor _queryProcessor;

    public UsersCursorController(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
    }

    /// <summary>
    /// Demonstrates cursor-based pagination using ApplyQueryCursorPagedResult.
    /// This will ALWAYS return cursor tokens, even on the first request (no pageToken needed).
    /// </summary>
    [HttpGet]
    public CursorPagedResult<User> Get([FromQuery] UserCursorQueryOptions options)
    {
        // Sample data (in a real app, this would come from a database)
        var users = new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com", DateOfBirth = new DateTime(1990, 1, 1) },
            new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com", DateOfBirth = new DateTime(1985, 5, 15) },
            new User { Id = 3, Name = "Alice Johnson", Email = "alice.johnson@example.com", DateOfBirth = new DateTime(1992, 8, 23) },
            new User { Id = 4, Name = "Bob Brown", Email = "bob.brown@example.com", DateOfBirth = new DateTime(1988, 12, 30) },
            new User { Id = 5, Name = "Charlie Wilson", Email = "charlie.wilson@example.com", DateOfBirth = new DateTime(1995, 3, 10) }
        };

        var query = users.AsQueryable();
        
        // Use the new ApplyQueryCursorPagedResult method - returns CursorPagedResult
        return query.ApplyQueryCursorPagedResult(_queryProcessor, options);
    }
}

/// <summary>
/// Query options for cursor-based user pagination.
/// Implements IQueryCursorPagedOptions (not IQueryPagedOptions).
/// </summary>
public class UserCursorQueryOptions : IQueryCursorPagedOptions
{
    public int? FilterId { get; set; }
    public string? FilterName { get; set; }
    public string? Fields { get; set; }
    public string? Sort { get; set; }
    public int? PageSize { get; set; }
    public string? PageToken { get; set; }
}
