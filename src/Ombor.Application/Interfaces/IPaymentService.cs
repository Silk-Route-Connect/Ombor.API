﻿using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Payment;

namespace Ombor.Application.Interfaces;

public interface IPaymentService
{
    Task<PagedList<PaymentDto>> GetAsync(GetPaymentsRequest request);
    Task<TransactionPaymentDto[]> GetTransactionPaymentsAsync(GetTransactionPaymentsRequest request);
    Task<PaymentDto> CreateAsync(CreatePaymentRequest request);
    Task<PaymentDto?> CreateAsync(CreateTransactionPaymentRequest request);
}
