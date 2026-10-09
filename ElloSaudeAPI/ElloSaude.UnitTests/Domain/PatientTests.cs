using ElloSaude.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ElloSaude.UnitTests.Domain;

public class PatientTests
{
    [Fact]
    public void Patient_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var name = "Maria Silva";
        var email = "maria@email.com";
        var cpf = "123.456.789-00";
        var birthDate = new DateTime(1990, 1, 1);
        var tenantId = "tenant-123";

        // Act
        var patient = new Patient(name, email, cpf, birthDate, tenantId);

        // Assert
        patient.Id.Should().NotBeEmpty();
        patient.Name.Should().Be(name);
        patient.Email.Should().Be(email);
        patient.Cpf.Should().Be(cpf);
        patient.BirthDate.Should().Be(birthDate);
        patient.TenantId.Should().Be(tenantId);
        patient.Records.Should().NotBeNull();
    }
}
