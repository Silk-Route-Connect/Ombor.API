using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentValidation;
using Ombor.Contracts.Serialization;
using Ombor.Domain.Exceptions;

namespace Ombor.API.Extensions;

/// <summary>
/// Sentry <c>BeforeSend</c>: drops client errors (4xx are not server faults) and makes sure no credential ever
/// leaves the process — request bodies of credential routes are dropped entirely, and secret-named fields are
/// masked in every other captured body.
/// </summary>
internal static class SentryEventFilter
{
    private const string Filtered = "[Filtered]";

    private static readonly HashSet<string> SecretFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "confirmPassword",
        "newPassword",
        "currentPassword",
        "refreshToken",
    };

    // Every body on these routes carries a password, a one-time code or a token.
    private static readonly string[] CredentialRoutes = ["/api/auth/", "/api/settings/password"];

    public static SentryEvent? BeforeSend(SentryEvent @event, SentryHint _)
    {
        if (IsClientError(@event.Exception))
        {
            return null;
        }

        var request = @event.Request;

        if (IsCredentialRoute(request.Url))
        {
            request.Data = null;
        }
        else
        {
            request.Data = Scrub(request.Data);
        }

        return @event;
    }

    private static bool IsClientError(Exception? exception) =>
        exception is ValidationException
            or EntityNotFoundException
            or UnauthorizedAccessException
            or TooManyRequestsException
            or ConflictException
            or InvalidOrderStateTransitionException
            or InvalidFileException
            or InvalidEnumValueException;

    private static bool IsCredentialRoute(string? url) =>
        url is not null && CredentialRoutes.Any(route => url.Contains(route, StringComparison.OrdinalIgnoreCase));

    private static object? Scrub(object? data) => data switch
    {
        string body => ScrubJson(body),
        IDictionary form => ScrubForm(form),
        _ => data,
    };

    private static string ScrubJson(string body)
    {
        JsonNode? root;

        try
        {
            root = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            // Not JSON (or truncated): it cannot be scrubbed field by field, so keep it only if no secret name appears.
            return SecretFields.Any(field => body.Contains(field, StringComparison.OrdinalIgnoreCase)) ? Filtered : body;
        }

        MaskSecrets(root);

        return root?.ToJsonString() ?? body;
    }

    private static void MaskSecrets(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(property => property.Key).ToList())
                {
                    if (SecretFields.Contains(key))
                    {
                        obj[key] = Filtered;
                    }
                    else
                    {
                        MaskSecrets(obj[key]);
                    }
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    MaskSecrets(item);
                }

                break;
        }
    }

    private static Dictionary<string, object?> ScrubForm(IDictionary form)
    {
        var scrubbed = new Dictionary<string, object?>();

        foreach (DictionaryEntry entry in form)
        {
            var key = entry.Key.ToString() ?? string.Empty;
            scrubbed[key] = SecretFields.Contains(key) ? Filtered : entry.Value;
        }

        return scrubbed;
    }
}
