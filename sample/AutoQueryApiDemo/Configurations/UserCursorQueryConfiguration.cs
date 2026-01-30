using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;

namespace AutoQueryApiDemo.Configurations;

public class UserCursorQueryConfiguration : IFilterQueryConfiguration<UserCursorQueryOptions, User>
{
    public void Configure(FilterQueryBuilder<UserCursorQueryOptions, User> builder)
    {
        builder.HasCursorKey(d => d.Id);
        
        builder.Property(q => q.FilterIds, d => d.Id)
            .HasCollectionContains();
        builder.Property(q => q.FilterName, d => d.Name)
            .HasEqual();
    }
}
