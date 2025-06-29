using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal interface ITransactionMapper
{
    TransactionRecord ToEntity(CreateTransactionRequest request);
    TransactionRecord ToEntity(UpdateTransactionRequest request);
    TransactionDto ToDto(TransactionRecord transaction, IEnumerable<Payment> payments);
    CreateTransactionResponse ToCreateResponse(TransactionRecord transaction);
    UpdateTransactionResponse ToUpdateResponse(TransactionRecord transaction);
}

internal sealed class TransactionMapper(
    IDateTimeProvider dateTimeProvider,
    IPaymentMapper paymentMapper) : ITransactionMapper
{
    /// <summary>
    /// Converts a <see cref="CreateTransactionRequest"/> into a <see cref="TransactionRecord"/> entity.
    /// </summary>
    /// <remarks>
    /// This method maps the properties of the <paramref name="request"/> to a new <see cref="TransactionRecord"/> instance.
    /// The <see cref="TransactionRecord.Partner"/> property is set to a non-null
    /// placeholder value and will be populated by Entity Framework during persistence.
    /// <see cref="TransactionRecord.TotalPaid"/> is not populated in mapping and will be set to default 0, 
    /// the payment amount should be applied during payment creation.
    /// </remarks>
    /// <param name="request">The transaction request containing details such as transaction lines, partner information, and notes. Cannot be
    /// <see langword="null"/>.</param>
    /// <returns>A <see cref="TransactionRecord"/> entity initialized with the data from the provided <paramref name="request"/>.</returns>
    public TransactionRecord ToEntity(CreateTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var totalDue = CalculateTotal(request.Lines);

        return new()
        {
            Notes = request.Notes,
            TransactionNumber = "",
            TotalDue = totalDue,
            TotalPaid = 0,
            DateUtc = dateTimeProvider.UtcNow,
            Type = Enum.Parse<Domain.Enums.TransactionType>(request.Type.ToString()),
            Status = Domain.Enums.TransactionStatus.Open,
            PartnerId = request.PartnerId,
            Partner = null!, // will be set by EF
            Lines = GetLines(request.Lines)
        };
    }

    public TransactionRecord ToEntity(UpdateTransactionRequest request)
    {
        var totalDue = CalculateTotal(request.Lines);
        var status = totalDue > request.TotalPaid
            ? Domain.Enums.TransactionStatus.Open
            : Domain.Enums.TransactionStatus.Closed;

        return new()
        {
            Id = request.Id,
            Notes = request.Notes,
            TotalDue = totalDue,
            TotalPaid = request.TotalPaid,
            Type = Enum.Parse<Domain.Enums.TransactionType>(request.Type.ToString()),
            Status = status,
            PartnerId = request.PartnerId,
            Partner = null!, // will be set by EF
            Lines = GetLines(request.Lines)
        };
    }

    public TransactionDto ToDto(TransactionRecord transaction, IEnumerable<Payment> payments) =>
        new(Id: transaction.Id,
            PartnerId: transaction.PartnerId,
            PartnerName: transaction.Partner.Name,
            Notes: transaction.Notes,
            TransactionNumber: transaction.TransactionNumber,
            TotalDue: transaction.TotalDue,
            TotalPaid: transaction.TotalPaid,
            Date: transaction.DateUtc,
            Type: Enum.Parse<Contracts.Enums.TransactionType>(transaction.Type.ToString()),
            Status: Enum.Parse<Contracts.Enums.TransactionStatus>(transaction.Status.ToString()),
            Lines: GetLines(transaction),
            Payments: paymentMapper.ToTransactionPayments(payments),
            Refunds: []);

    public CreateTransactionResponse ToCreateResponse(TransactionRecord transaction) =>
        new(Id: transaction.Id,
            PartnerId: transaction.Id,
            PartnerName: transaction.Partner.Name,
            Notes: transaction.Notes,
            TransactionNumber: transaction.TransactionNumber,
            TotalDue: transaction.TotalDue,
            TotalPaid: transaction.TotalPaid,
            Type: Enum.Parse<Contracts.Enums.TransactionType>(transaction.Type.ToString()),
            Status: Enum.Parse<Contracts.Enums.TransactionStatus>(transaction.Status.ToString()),
            Lines: GetLines(transaction.Lines));

    public UpdateTransactionResponse ToUpdateResponse(TransactionRecord transaction) =>
        new(Id: transaction.Id,
            PartnerId: transaction.Id,
            PartnerName: transaction.Partner.Name,
            Notes: transaction.Notes,
            TransactionNumber: transaction.TransactionNumber,
            TotalDue: transaction.TotalDue,
            TotalPaid: transaction.TotalPaid,
            Type: Enum.Parse<Contracts.Enums.TransactionType>(transaction.Type.ToString()),
            Status: Enum.Parse<Contracts.Enums.TransactionStatus>(transaction.Status.ToString()),
            Lines: GetLines(transaction.Lines));

    private static decimal CalculateTotal(IEnumerable<CreateTransactionLine> lines) =>
        lines.Sum(line => (line.UnitPrice * line.Quantity) - line.Discount);

    private static decimal CalculateTotal(IEnumerable<UpdateTransactionLine> lines) =>
        lines.Sum(line => (line.UnitPrice * line.Quantity) - line.Discount);

    private static TransactionLine[] GetLines(IEnumerable<CreateTransactionLine> lines) =>
        lines.Select(line => new TransactionLine
        {
            ProductId = line.ProductId,
            UnitPrice = line.UnitPrice,
            Quantity = line.Quantity,
            Discount = line.Discount,
            Product = null!, // will be set by EF
            Transaction = null! // will be set by EF
        })
        .ToArray();

    private static TransactionLine[] GetLines(IEnumerable<UpdateTransactionLine> lines) =>
        lines.Select(line => new TransactionLine
        {
            Id = line.Id,
            ProductId = line.ProductId,
            UnitPrice = line.UnitPrice,
            Quantity = line.Quantity,
            Discount = line.Discount,
            Product = null!, // will be set by EF
            Transaction = null! // will be set by EF
        })
        .ToArray();

    private static TransactionLineDto[] GetLines(IEnumerable<TransactionLine> lines) =>
        lines.Select(line => new TransactionLineDto(
            Id: line.Id,
            ProductId: line.ProductId,
            ProductName: line.Product.Name,
            Total: line.LineTotal,
            UnitPrice: line.UnitPrice,
            Quantity: line.Quantity,
            Discount: line.Discount))
        .ToArray();

    private static TransactionLineDto[] GetLines(TransactionRecord transaction) =>
        transaction.Lines
        .Select(x => new TransactionLineDto(
            Id: x.Id,
            ProductId: x.ProductId,
            ProductName: x.Product.Name,
            Total: x.LineTotal,
            UnitPrice: x.UnitPrice,
            Quantity: x.Quantity,
            Discount: x.Discount))
        .ToArray();
}
