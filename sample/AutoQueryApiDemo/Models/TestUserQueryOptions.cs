using AutoQuery.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AutoQueryApiDemo.Models;

public class TestUserQueryOptions : IQueryPagedOptions
{
    [FromQuery(Name = "filter[email]")]
    public string? FilterEmail { get; set; }
    [FromQuery(Name = "fields")]
    public string? Fields { get; set; }
    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }
    [FromQuery(Name = "page")]
    public int? Page { get; set; }
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }
    [FromQuery(Name = "pageToken")]
    public string? PageToken { get; set; }
}
