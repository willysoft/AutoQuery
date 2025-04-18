namespace AutoQuery.Abstractions;

/// <summary>
/// Query parameters.
/// </summary>
public interface IQueryOptions
{
    /// <summary>
    /// Selected fields.
    /// </summary>
    string? Fields { get; set; }

    /// <summary>
    /// Sorting fields.
    /// </summary>
    string? Sort { get; set; }
}

