using System.Text.Json;

namespace AutoQuery.Tests;

public class PageTokenTests
{
    [Fact]
    public void Encode_ShouldEncodeSimpleObject()
    {
        // Arrange
        var cursorData = new CursorData(LastId: 123);

        // Act
        var token = PageToken.Encode(cursorData);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void Encode_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        object? nullData = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PageToken.Encode(nullData!));
    }

    [Fact]
    public void Decode_ShouldDecodeEncodedToken()
    {
        // Arrange
        var originalData = new CursorData(LastId: 456);
        var token = PageToken.Encode(originalData);

        // Act
        var decoded = PageToken.Decode<CursorData>(token);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(originalData.LastId.ToString(), decoded.LastId.ToString());
    }

    [Fact]
    public void Decode_WithNullToken_ShouldReturnDefault()
    {
        // Act
        var result = PageToken.Decode<CursorData>(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Decode_WithEmptyToken_ShouldReturnDefault()
    {
        // Act
        var result = PageToken.Decode<CursorData>("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Decode_WithInvalidToken_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PageToken.Decode<CursorData>(invalidToken));
    }

    [Fact]
    public void Encode_Decode_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalData = new CursorData(
            LastId: 789, 
            LastSortValues: new Dictionary<string, object?> 
            { 
                { "Name", "Test" }, 
                { "CreatedAt", "2024-01-01" } 
            });

        // Act
        var token = PageToken.Encode(originalData);
        var decoded = PageToken.Decode<CursorData>(token);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(originalData.LastId.ToString(), decoded.LastId.ToString());
        Assert.NotNull(decoded.LastSortValues);
        Assert.Equal(2, decoded.LastSortValues.Count);
    }

    [Fact]
    public void Encode_ShouldProduceBase64String()
    {
        // Arrange
        var cursorData = new CursorData(LastId: 100);

        // Act
        var token = PageToken.Encode(cursorData);

        // Assert - Should be valid Base64
        var isBase64 = TryFromBase64String(token, out _);
        Assert.True(isBase64);
    }

    [Fact]
    public void Encode_DifferentData_ShouldProduceDifferentTokens()
    {
        // Arrange
        var data1 = new CursorData(LastId: 100);
        var data2 = new CursorData(LastId: 200);

        // Act
        var token1 = PageToken.Encode(data1);
        var token2 = PageToken.Encode(data2);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Encode_SameData_ShouldProduceSameToken()
    {
        // Arrange
        var data1 = new CursorData(LastId: 100);
        var data2 = new CursorData(LastId: 100);

        // Act
        var token1 = PageToken.Encode(data1);
        var token2 = PageToken.Encode(data2);

        // Assert
        Assert.Equal(token1, token2);
    }

    private static bool TryFromBase64String(string s, out byte[]? result)
    {
        try
        {
            result = Convert.FromBase64String(s);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
