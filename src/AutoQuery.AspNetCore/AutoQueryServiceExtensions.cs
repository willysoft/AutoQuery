#nullable disable

using AutoQuery;
using AutoQuery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods related to query services.
/// </summary>
public static class AutoQueryServiceExtensions
{
    /// <summary>
    /// Adds query builder services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to apply configurations from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAutoQuery(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        services.AddSingleton<IQueryProcessor, QueryProcessor>(sp =>
        {
            var queryProcessor = new QueryProcessor();
            queryProcessor.ApplyConfigurationsFromAssembly(assembly);
            return queryProcessor;
        });

        return services;
    }
}
