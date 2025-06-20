using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

internal interface IPaymentAllocationService
{
    Task ApplyPayment(Payment payment);
}
