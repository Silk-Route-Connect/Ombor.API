namespace Ombor.Contracts.Requests.User;

/// <summary>Sets the current user's interface language. Allowed values: ru, uz-Latn, uz-Cyrl.</summary>
/// <param name="Language">The language code.</param>
public sealed record SetLanguageRequest(string Language);
