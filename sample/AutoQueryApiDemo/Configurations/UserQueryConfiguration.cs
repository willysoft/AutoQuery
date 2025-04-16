using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;

namespace AutoQueryApiDemo.Configurations;

public class UserQueryConfiguration : IFilterQueryConfiguration<UserQueryOptions, User>
{
    public void Configure(FilterQueryBuilder<UserQueryOptions, User> builder)
    {
        builder.Property(q => q.FilterIds, d => d.Id)
            .HasCollectionContains();
        builder.Property(q => q.FilterName, d => d.Name)
            .HasEqual();
    }
}