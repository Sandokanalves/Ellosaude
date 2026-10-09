using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ElloSaude.UnitTests.Domain;

public class PaymentRecordTests
{
    [Fact]
    public void RegisterPayment_ShouldAccumulatePartialPaymentsUsingOneMethod()
    {
        var record = CreatePaymentRecord(expectedAmount: 100);

        record.RegisterPayment(40, PaymentMethod.Pix, Guid.NewGuid());
        record.Status.Should().Be(PaymentStatus.ParcialmentePago);
        record.RegisterPayment(60, PaymentMethod.Pix, Guid.NewGuid());

        record.AmountPaid.Should().Be(100);
        record.Status.Should().Be(PaymentStatus.Pago);
    }

    [Fact]
    public void RegisterPayment_ShouldRejectCombiningDifferentMethods()
    {
        var record = CreatePaymentRecord(expectedAmount: 100);
        record.RegisterPayment(40, PaymentMethod.Pix, Guid.NewGuid());

        var act = () => record.RegisterPayment(60, PaymentMethod.Dinheiro, Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*combinar formas de pagamento*");
    }

    [Fact]
    public void MarkAsFreeReturn_ShouldNotEraseAnExistingPayment()
    {
        var record = CreatePaymentRecord(expectedAmount: 100);
        record.RegisterPayment(25, PaymentMethod.Pix, Guid.NewGuid());

        var act = () => record.MarkAsFreeReturn(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sem pagamentos*");
        record.AmountPaid.Should().Be(25);
    }

    [Fact]
    public void Cancel_ShouldRequireRefundWhenPaymentWasReceived()
    {
        var record = CreatePaymentRecord(expectedAmount: 100);
        record.RegisterPayment(100, PaymentMethod.Pix, Guid.NewGuid());

        var act = () => record.Cancel(Guid.NewGuid(), "Cancelamento");

        act.Should().Throw<InvalidOperationException>().WithMessage("*estorno*");
        record.Status.Should().Be(PaymentStatus.Pago);
    }

    private static PaymentRecord CreatePaymentRecord(decimal expectedAmount)
    {
        return new PaymentRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            expectedAmount,
            "tenant-a",
            Guid.NewGuid());
    }
}
