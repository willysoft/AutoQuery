using System.Text;
using System.Text.Json;

namespace AutoQuery;

/// <summary>
/// Utility class for encoding and decoding page tokens.
/// </summary>
public static class PageToken
{
    /// <summary>
    /// Encodes a cursor value into an opaque page token.
    /// </summary>
    /// <param name="cursorValue">The cursor value to encode.</param>
    /// <returns>The encoded page token.</returns>
    public static string Encode(object cursorValue)
    {
        if (cursorValue == null)
            throw new ArgumentNullException(nameof(cursorValue));

        var valueString = Convert.ToString(cursorValue, System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(valueString))
            throw new ArgumentException("Cursor value cannot be empty", nameof(cursorValue));

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(valueString));
    }

    /// <summary>
    /// Encodes multiple cursor values into an opaque page token.
    /// </summary>
    /// <param name="cursorValues">Dictionary of field names and their values.</param>
    /// <returns>The encoded page token.</returns>
    public static string EncodeComposite(Dictionary<string, object?> cursorValues)
    {
        if (cursorValues == null || cursorValues.Count == 0)
            throw new ArgumentException("Cursor values cannot be null or empty", nameof(cursorValues));

        var json = JsonSerializer.Serialize(cursorValues);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Decodes a page token into the original cursor value.
    /// </summary>
    /// <typeparam name="T">The type of the cursor value.</typeparam>
    /// <param name="pageToken">The page token to decode.</param>
    /// <returns>The decoded cursor value.</returns>
    public static T Decode<T>(string pageToken)
    {
        if (string.IsNullOrWhiteSpace(pageToken))
            throw new ArgumentException("Page token cannot be null or empty", nameof(pageToken));

        try
        {
            var valueString = Encoding.UTF8.GetString(Convert.FromBase64String(pageToken));
            var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (underlyingType == typeof(Guid))
                return (T)(object)Guid.Parse(valueString);
            
            if (underlyingType == typeof(DateTime))
                return (T)(object)DateTime.Parse(valueString, System.Globalization.CultureInfo.InvariantCulture);
            
            if (underlyingType == typeof(DateTimeOffset))
                return (T)(object)DateTimeOffset.Parse(valueString, System.Globalization.CultureInfo.InvariantCulture);

            return (T)Convert.ChangeType(valueString, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid page token", nameof(pageToken), ex);
        }
    }

    /// <summary>
    /// Decodes a composite page token into a dictionary of cursor values.
    /// </summary>
    /// <param name="pageToken">The page token to decode.</param>
    /// <returns>Dictionary of field names and their values.</returns>
    public static Dictionary<string, JsonElement> DecodeComposite(string pageToken)
    {
        if (string.IsNullOrWhiteSpace(pageToken))
            throw new ArgumentException("Page token cannot be null or empty", nameof(pageToken));

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(pageToken));
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) 
                ?? throw new InvalidOperationException("Failed to deserialize page token");
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid page token", nameof(pageToken), ex);
        }
    }
}
