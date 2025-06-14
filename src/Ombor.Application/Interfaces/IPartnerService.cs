using FluentValidation;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Defunes CRUD operations for partner management.
/// </summary>
public interface IPartnerService
{
    /// <summary>
    /// Retrieves all partners, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">
    /// The filtering options. <see cref="GetPartnersRequest.SearchTerm"/> may be null or whitespace.
    /// </param>
    /// <returns>An array of <see cref="PartnerDto"/>Matching the filter.</returns>
    Task<PartnerDto[]> GetAsync(GetPartnersRequest request);

    /// <summary>
    /// Retrieves a single partner by its identifier.
    /// </summary>
    /// <param name="request">Contains the <see cref="GetPartnerByIdRequest.Id"/> of the partner to fetch.</param>
    /// <returns>The matching <see cref="PartnerDto"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{partner}">If no partner with the given ID exists.</exception>
    Task<PartnerDto> GetByIdAsync(GetPartnerByIdRequest request);

    /// <summary>
    /// Creates a new partner.
    /// </summary>
    /// <param name="request">The properties of the partner to create.</param>
    /// <returns>Details of the newly created partner.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    Task<CreatePartnerResponse> CreateAsync(CreatePartnerRequest request);

    /// <summary>
    /// Updates an existing partner.
    /// </summary>
    /// <param name="request">Contains the ID and new values for the partner.</param>
    /// <returns>Details of the updated partner.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{partner}">If no partner with the given ID exists.</exception>
    Task<UpdatePartnerResponse> UpdateAsync(UpdatePartnerRequest request);

    /// <summary>
    /// Deletes an existing partner by its ID.
    /// </summary>
    /// <param name="request">Contains the <see cref="DeletePartnerRequest.Id"/> to remove.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{partner}">If no partner with the given ID exists.</exception>
    Task DeleteAsync(DeletePartnerRequest request);
}
