using ElloSaude.Infrastructure.Identity;
using FluentAssertions;
using Xunit;

namespace ElloSaude.UnitTests.Infrastructure;

public class HashServiceTests
{
    [Fact]
    public void HashPassword_ShouldReturnHashedString()
    {
        // Arrange
        var service = new HashService();
        var password = "SenhaForte123!";

        // Act
        var hash = service.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
        service.VerifyPassword(password, hash).Should().BeTrue();
    }
}
