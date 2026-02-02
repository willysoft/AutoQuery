using AutoQuery.Abstractions;

namespace AutoQuery.Tests;

public class PageTokenTests
{
    [Theory]
    [InlineData(123)]
    [InlineData("test-value")]
    [InlineData(456L)]
    public void Encode_ShouldEncodeValue(object value)
    {
        // Act
        var token = PageToken.Encode(value);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void Encode_ShouldThrowArgumentNullException_WhenValueIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PageToken.Encode(null!));
    }

    [Theory]
    [InlineData(123)]
    [InlineData(456L)]
    public void Decode_ShouldDecodeEncodedValue(object value)
    {
        // Arrange
        var token = PageToken.Encode(value);

        // Act
        var decoded = PageToken.Decode<long>(token);

        // Assert
        Assert.Equal(Convert.ToInt64(value), decoded);
    }

    [Fact]
    public void Decode_ShouldDecodeStringValue()
    {
        // Arrange
        var value = "test-value";
        var token = PageToken.Encode(value);

        // Act
        var decoded = PageToken.Decode<string>(token);

        // Assert
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Decode_ShouldThrowArgumentException_WhenTokenIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PageToken.Decode<int>(""));
    }

    [Fact]
    public void Decode_ShouldThrowArgumentException_WhenTokenIsInvalid()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PageToken.Decode<int>("invalid-token"));
    }

    [Fact]
    public void Encode_Decode_ShouldBeReversible()
    {
        // Arrange
        var originalValue = 42;

        // Act
        var token = PageToken.Encode(originalValue);
        var decodedValue = PageToken.Decode<int>(token);

        // Assert
        Assert.Equal(originalValue, decodedValue);
    }

    [Fact]
    public void Decode_ShouldHandleGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var token = PageToken.Encode(guid);

        // Act
        var decoded = PageToken.Decode<Guid>(token);

        // Assert
        Assert.Equal(guid, decoded);
    }

    [Fact]
    public void Decode_ShouldHandleDateTime()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var token = PageToken.Encode(dateTime);

        // Act
        var decoded = PageToken.Decode<DateTime>(token);

        // Assert
        Assert.Equal(dateTime, decoded);
    }

    [Fact]
    public void Decode_ShouldHandleLong()
    {
        // Arrange
        var longValue = 9876543210L;
        var token = PageToken.Encode(longValue);

        // Act
        var decoded = PageToken.Decode<long>(token);

        // Assert
        Assert.Equal(longValue, decoded);
    }
}
