using System.Text;

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

        var bytes = Encoding.UTF8.GetBytes(valueString);
        return Convert.ToBase64String(bytes);
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
            var bytes = Convert.FromBase64String(pageToken);
            var valueString = Encoding.UTF8.GetString(bytes);
            
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Handle common cursor key types explicitly
            if (underlyingType == typeof(Guid))
            {
                return (T)(object)Guid.Parse(valueString);
            }
            if (underlyingType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(valueString, System.Globalization.CultureInfo.InvariantCulture);
            }
            if (underlyingType == typeof(DateTimeOffset))
            {
                return (T)(object)DateTimeOffset.Parse(valueString, System.Globalization.CultureInfo.InvariantCulture);
            }

            return (T)Convert.ChangeType(valueString, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid page token", nameof(pageToken), ex);
        }
    }
}
