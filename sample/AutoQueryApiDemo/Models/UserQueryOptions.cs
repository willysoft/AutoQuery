using AutoQuery.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AutoQueryApiDemo.Models;

public class UserQueryOptions : IQueryPagedOptions
{
    [FromQuery(Name = "filter[ids]")]
    public int[]? FilterIds { get; set; }
    [FromQuery(Name = "filter[name]")]
    public string? FilterName { get; set; }
    [FromQuery(Name = "fields")]
    public string? Fields { get; set; }
    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }
    [FromQuery(Name = "page")]
    public int? Page { get; set; }
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }
}