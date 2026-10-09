using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Patients.Commands;
using ElloSaude.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ElloSaude.UnitTests.Application;

public class CreatePatientHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldAddPatientAndReturnId()
    {
        // Arrange
        var uowMock = new Mock<IUnitOfWork>();
        var tenantMock = new Mock<ITenantService>();
        var repoMock = new Mock<IRepository<Patient>>();

        tenantMock.Setup(t => t.GetTenantId()).Returns("tenant-test");
        uowMock.Setup(u => u.Patients).Returns(repoMock.Object);

        var handler = new CreatePatientHandler(uowMock.Object, tenantMock.Object);
        var command = new CreatePatientCommand("João Santos", "joao@email.com", "999.888.777-66", new DateTime(1995, 10, 10));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        repoMock.Verify(r => r.AddAsync(It.Is<Patient>(p => p.Name == "João Santos" && p.TenantId == "tenant-test"), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
