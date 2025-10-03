using Ombor.Domain.Enums;

namespace Ombor.Application.Models;

public sealed record OtpCode(
    string PhoneNumber,
    string Code,
    OtpPurpose Purpose,
    DateTime ExpiredAt);
