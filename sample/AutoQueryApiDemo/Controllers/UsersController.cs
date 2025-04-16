using AutoQuery.Abstractions;
using AutoQuery.AspNetCore;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoQueryApiDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private List<User> users = new List<User>
    {
        new User(4, "Bob Brown", "bob.brown@example.com", new DateTime(1988, 12, 30)),
        new User(1, "John Doe", "john.doe@example.com", new DateTime(1990, 1, 1)),
        new User(3, "Alice Johnson", "alice.johnson@example.com", new DateTime(1992, 8, 23)),
        new User(5, "Charlie Davis", "charlie.davis@example.com", new DateTime(1995, 3, 10)),
        new User(2, "Jane Smith", "jane.smith@example.com", new DateTime(1985, 5, 15)),
    };

    private readonly IQueryProcessor _queryProcessor;

    public UsersController(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
    }

    [HttpGet]
    [EnableFieldProjection]
    public IActionResult Get(UserQueryOptions queryOptions)
    {
        var result = users.AsQueryable()
                          .ApplyQueryPaged(_queryProcessor, queryOptions);
        return Ok(result);
    }
}
