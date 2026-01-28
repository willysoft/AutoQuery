using AutoQuery;
using AutoQuery.Abstractions;
using AutoQuery.Extensions;
using AutoQueryApiDemo.Models;

namespace AutoQueryApiDemo.Configurations;

/// <summary>
/// Configuration for Product queries demonstrating custom cursor key using SKU property.
/// </summary>
public class ProductQueryConfiguration : IFilterQueryConfiguration<ProductQueryOptions, Product>
{
    public void Configure(FilterQueryBuilder<ProductQueryOptions, Product> builder)
    {
        // Configure SKU as the cursor key instead of Id for cursor-based pagination
        builder.Property(q => q.FilterSKU, d => d.SKU)
            .HasEqual()
            .HasCursorKey();  // SKU will be used as the cursor key
            
        builder.Property(q => q.FilterName, d => d.Name)
            .HasStringContains();
    }
}
