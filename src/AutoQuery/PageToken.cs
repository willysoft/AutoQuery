using System.Text;
using System.Text.Json;

namespace AutoQuery;

/// <summary>
/// Provides utilities for encoding and decoding page tokens used in cursor-based pagination.
/// </summary>
public static class PageToken
{
    /// <summary>
    /// Encodes cursor data into an opaque page token.
    /// </summary>
    /// <param name="cursorData">The cursor data to encode (e.g., last ID, last value).</param>
    /// <returns>An opaque Base64-encoded page token.</returns>
    public static string Encode(object cursorData)
    {
        if (cursorData == null)
            throw new ArgumentNullException(nameof(cursorData));

        var json = JsonSerializer.Serialize(cursorData);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes an opaque page token into cursor data.
    /// </summary>
    /// <typeparam name="T">The type of cursor data to decode.</typeparam>
    /// <param name="token">The page token to decode.</param>
    /// <returns>The decoded cursor data.</returns>
    /// <exception cref="ArgumentException">Thrown when the token is invalid or cannot be decoded.</exception>
    public static T? Decode<T>(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return default;

        try
        {
            var bytes = Convert.FromBase64String(token);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid page token.", nameof(token), ex);
        }
    }
}
