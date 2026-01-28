namespace AutoQuery;

/// <summary>
/// Represents cursor data for cursor-based pagination.
/// </summary>
/// <param name="LastId">The ID of the last item in the current page.</param>
public record CursorData(object? LastId);
