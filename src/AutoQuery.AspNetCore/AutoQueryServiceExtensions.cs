#nullable disable

using AutoQuery;
using AutoQuery.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 提供查詢服務相關的擴展方法。
/// </summary>
public static class AutoQueryServiceExtensions
{
    /// <summary>
    /// 向服務集合中添加查詢建構器服務。
    /// </summary>
    /// <param name="services">服務集合。</param>
    /// <returns>更新後的服務集合。</returns>
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
