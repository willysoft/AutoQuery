using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;

namespace AutoQueryApiDemo.Configurations;

public class UserCursorQueryConfiguration : IFilterQueryConfiguration<UserCursorQueryOptions, User>
{
    public void Configure(FilterQueryBuilder<UserCursorQueryOptions, User> builder)
    {
        // Configure cursor key for cursor-based pagination
        builder.HasCursorKey(d => d.Id);
        
        // Configure filter properties
        builder.Property(q => q.FilterIds, d => d.Id)
            .HasCollectionContains();
        builder.Property(q => q.FilterName, d => d.Name)
            .HasEqual();
    }
}
