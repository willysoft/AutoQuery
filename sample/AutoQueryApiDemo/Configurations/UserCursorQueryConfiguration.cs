using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Controllers;
using AutoQueryApiDemo.Models;

namespace AutoQueryApiDemo.Configurations;

public class UserCursorQueryConfiguration : IFilterQueryConfiguration<UserCursorQueryOptions, User>
{
    public void Configure(FilterQueryBuilder<UserCursorQueryOptions, User> builder)
    {
        // Configure Id as the cursor key
        // Using FilterId property (even though it's for filtering by Id, we can use it for cursor key too)
        builder.Property(q => q.FilterId, d => d.Id)
            .HasCursorKey();
    }
}
