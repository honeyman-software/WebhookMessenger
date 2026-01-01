using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebhookMessenger.Services;

public class DiscordWebhookClient
{
    private readonly HttpClient _http = new();

    public async Task SendAsync(
        string webhookUrl,
        string content,
        string? username,
        string? avatarUrl,
        string embedsJson,
        bool disableMentions)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new Exception("Webhook URL is empty.");

        if (!webhookUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Webhook URL must start with https://");

        // Parse embeds JSON: allow [] or an object, or omit if blank
        object? embedsObj = null;
        var trimmedEmbeds = (embedsJson ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(trimmedEmbeds))
        {
            using var doc = JsonDocument.Parse(trimmedEmbeds);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                // OK: [] or [ {...} ]
                embedsObj = root.Clone();
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Convert single object -> array (Discord expects array)
                embedsObj = JsonDocument.Parse($"[{root.GetRawText()}]").RootElement.Clone();
            }
            else
            {
                throw new Exception("Embeds JSON must be an array [] or an object {}.");
            }
        }

        var trimmedContent = (content ?? "").Trim();

        // Discord requires either content or at least one embed
        var embedsIsEmptyArray = embedsObj is JsonElement je && je.ValueKind == JsonValueKind.Array && je.GetArrayLength() == 0;
        if (string.IsNullOrWhiteSpace(trimmedContent) && (embedsObj is null || embedsIsEmptyArray))
            throw new Exception("Discord requires message content or a non-empty embeds array.");

        object payload = new
        {
            content = string.IsNullOrWhiteSpace(trimmedContent) ? null : trimmedContent,
            username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            avatar_url = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
            embeds = embedsObj,
            allowed_mentions = disableMentions ? new { parse = Array.Empty<string>() } : null
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var res = await _http.PostAsync(webhookUrl, new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Discord error {(int)res.StatusCode}: {body}");
    }
}
