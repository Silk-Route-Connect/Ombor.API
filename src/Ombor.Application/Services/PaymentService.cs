using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Payments;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class PaymentService(
    IApplicationDbContext context,
    IPaymentMapper mapper,
    IRequestValidator validator,
    IPaymentAllocationService paymentAllocationService,
    IFileService fileService,
    IOptions<FileSettings> fileSettings) : IPaymentService
{
    private readonly FileSettings _fileSettings = fileSettings.Value;

    public async Task<PaymentDto[]> GetAsync(GetPaymentsRequest request)
    {
        var query = GetQuery(request);
        var payments = await query.ToArrayAsync();

        return [.. payments.Select(mapper.ToDto)];
    }

    public async Task<PaymentDto> GetByIdAsync(GetPaymentByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var payment = await GetOrThrowAsync(request.Id);

        return mapper.ToDto(payment);
    }

    public async Task<CreatePaymentResponse> CreateAsync(CreatePaymentRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = mapper.ToEntity(request);
        await using var dbTransaction = await context.Database.BeginTransactionAsync();

        try
        {
            if (entity.NeedsAutoAllocation())
            {
                await paymentAllocationService.ApplyPayment(entity);
            }

            await AddAttachmentsAsync(request.Attachments, entity);

            context.Payments.Add(entity);
            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        return mapper.ToCreateResponse(entity);
    }

    public Task<UpdatePaymentResponse> UpdateAsync(UpdatePaymentRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(DeletePaymentRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var payment = await GetOrThrowAsync(request.Id);

        context.Payments.Remove(payment);
        await context.SaveChangesAsync();
    }

    private async Task<Payment> GetOrThrowAsync(int paymentId)
        => await context.Payments.FirstOrDefaultAsync(x => x.Id == paymentId)
        ?? throw new EntityNotFoundException<Payment>(paymentId);

    private IQueryable<Payment> GetQuery(GetPaymentsRequest request)
    {
        var query = context.Payments
            .Include(x => x.Attachments)
            .Include(x => x.Allocations)
            .AsNoTracking();

        if (request.PartnerId.HasValue)
        {
            query = query.Where(x => x.PartnerId == request.PartnerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Notes != null && x.Notes.Contains(request.SearchTerm));
        }

        if (request.Type.HasValue)
        {
            var paymentType = Enum.Parse<Domain.Enums.PaymentType>(request.Type.Value.ToString());
            query = query.Where(x => x.Type == paymentType);
        }

        return query;
    }

    private async ValueTask AddAttachmentsAsync(IEnumerable<IFormFile>? attachments, Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (attachments?.Any() != true)
        {
            return;
        }

        var fileUrls = await fileService.UploadAsync(attachments, _fileSettings.PaymentAttachmentsSection);
        var paymentAttachments = fileUrls.Select(x => new PaymentAttachment
        {
            FileName = x.FileName,
            FileUrl = x.Url,
            Payment = null! // will be set by EF
        });

        foreach (var attachment in paymentAttachments)
        {
            payment.Attachments.Add(attachment);
        }
    }
}
