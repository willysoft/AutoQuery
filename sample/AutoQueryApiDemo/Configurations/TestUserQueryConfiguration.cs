using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;

namespace AutoQueryApiDemo.Configurations;

public class TestUserQueryConfiguration : IFilterQueryConfiguration<TestUserQueryOptions, User>
{
    public void Configure(FilterQueryBuilder<TestUserQueryOptions, User> builder)
    {
        builder.Property(q => q.FilterEmail, d => d.Email)
            .HasEqual();
    }
}
