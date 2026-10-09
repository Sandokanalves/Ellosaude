using ElloSaude.Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;
using CustomValidationException = ElloSaude.Application.Common.Exceptions.ValidationException;

namespace ElloSaude.UnitTests.Application;

public record TestCommand(string Name) : IRequest<string>;

public class TestCommandValidator : AbstractValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Nome é obrigatório.");
    }
}

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand(""); // Nome vazio propositalmente

        var nextMock = new Mock<RequestHandlerDelegate<string>>();

        // Act
        Func<Task> act = async () => await behavior.Handle(command, nextMock.Object, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<CustomValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
        exception.Which.Errors["Name"].Should().Contain("Nome é obrigatório.");

        nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNextDelegate()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Nome Valido");

        var nextMock = new Mock<RequestHandlerDelegate<string>>();
        nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("Sucesso");

        // Act
        var result = await behavior.Handle(command, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be("Sucesso");
        nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);
    }
}
