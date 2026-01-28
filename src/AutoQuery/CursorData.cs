namespace AutoQuery;

/// <summary>
/// Represents cursor data for cursor-based pagination.
/// </summary>
/// <param name="LastId">The ID of the last item in the current page.</param>
/// <param name="LastSortValues">The sort values of the last item for cursor positioning.</param>
public record CursorData(object? LastId, Dictionary<string, object?>? LastSortValues = null);
