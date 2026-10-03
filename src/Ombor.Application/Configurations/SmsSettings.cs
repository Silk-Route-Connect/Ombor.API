using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

public sealed class SmsSettings
{
    public const string SectionName = nameof(SmsSettings);

    [Required(ErrorMessage = "SMS Token is required.")]
    public required string Token { get; init; }

    [Required(ErrorMessage = "ApiUrl is required.")]
    public required string ApiUrl { get; set; }

    [Required(ErrorMessage = "From Number is required.")]
    public required string FromNumber { get; init; }

    /// <summary>
    /// In the Development environment SMS are written to the log instead of sent; set this to <c>true</c> to send
    /// real SMS from a local run. Ignored in every other environment.
    /// </summary>
    public bool SendInDevelopment { get; init; }
}
