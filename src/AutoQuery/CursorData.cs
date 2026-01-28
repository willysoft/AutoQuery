namespace AutoQuery;

/// <summary>
/// Represents cursor data for cursor-based pagination.
/// </summary>
/// <param name="LastId">The ID of the last item in the current page (for forward pagination).</param>
/// <param name="FirstId">The ID of the first item in the current page (for backward pagination).</param>
public record CursorData(object? LastId, object? FirstId = null);
