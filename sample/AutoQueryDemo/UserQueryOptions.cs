using AutoQuery.Abstractions;

namespace QueryHelperDemo;

public class UserQueryOptions : IQueryPagedOptions
{
    public int[]? FilterIds { get; set; }
    public string? FilterName { get; set; }
    public string? Fields { get; set; }
    public string? Sort { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
