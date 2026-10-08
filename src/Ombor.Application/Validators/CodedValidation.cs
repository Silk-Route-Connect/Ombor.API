using FluentValidation;
using FluentValidation.Results;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Validators;

/// <summary>
/// Builds a 400 <see cref="ValidationException"/> whose field failure carries a domain <see cref="ErrorCodes">error
/// code</see> (and optional params). <c>ValidationExceptionHandler</c> serves the first such code as the
/// ProblemDetails <c>code</c> and the failure's params as <c>params</c>; plain rule failures fall back to
/// <see cref="ErrorCodes.ValidationFailed"/>. Validator rules can attach a code the same way with
/// <c>.WithErrorCode(ErrorCodes.X)</c>.
/// </summary>
internal static class CodedValidation
{
    public static ValidationException Failure(
        string propertyName,
        string message,
        string code,
        IReadOnlyDictionary<string, object?>? parameters = null) =>
        new([
            new ValidationFailure(propertyName, message)
            {
                ErrorCode = code,
                CustomState = parameters,
            },
        ]);
}
