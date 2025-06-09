using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Template;

public sealed record GetTemplatesRequest(
    string? SearchTerm = null,
    TemplateType? Type = null);
