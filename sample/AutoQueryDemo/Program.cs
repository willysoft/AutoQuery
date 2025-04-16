using AutoQuery;
using AutoQuery.Extensions;
using QueryHelperDemo;
using System.Reflection;

var queryProcessor = new QueryProcessor();
queryProcessor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
var users = new List<User>
{
    new User(4, "Bob Brown", "bob.brown@example.com", new DateTime(1988, 12, 30)),
    new User(1, "John Doe", "john.doe@example.com", new DateTime(1990, 1, 1)),
    new User(3, "Alice Johnson", "alice.johnson@example.com", new DateTime(1992, 8, 23)),
    new User(5, "Charlie Davis", "charlie.davis@example.com", new DateTime(1995, 3, 10)),
    new User(2, "Jane Smith", "jane.smith@example.com", new DateTime(1985, 5, 15)),
};
var result = users.AsQueryable().ApplyQuery(queryProcessor, new UserQueryOptions()
{
    Fields = "Id,Name",
    Sort = "-Id",
    FilterIds = [3, 4],
}).ToArray();

Console.WriteLine("All Users:");