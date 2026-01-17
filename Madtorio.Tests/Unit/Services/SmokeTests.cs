namespace Madtorio.Tests.Unit.Services;

/// <summary>
/// Basic smoke tests to verify the test infrastructure is working
/// </summary>
public class SmokeTests
{
    [Fact]
    public void SmokeTest_Pass()
    {
        // Arrange
        var expected = 42;

        // Act
        var actual = 42;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SmokeTest_StringComparison()
    {
        // Arrange
        var expected = "Madtorio";

        // Act
        var actual = "Madtorio";

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 5, 10)]
    [InlineData(-1, 1, 0)]
    public void SmokeTest_Addition(int a, int b, int expected)
    {
        // Act
        var actual = a + b;

        // Assert
        Assert.Equal(expected, actual);
    }
}
