using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebhookMessenger.Models;

namespace WebhookMessenger.Services;

public class ImportExportService
{
    // Portable format (plaintext URLs so it can move between PCs)
    public sealed class PortableWebhook
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public sealed class PortableTemplate
    {
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
        public string EmbedsJson { get; set; } = "[]";
    }

    public sealed class PortableData
    {
        public int Version { get; set; } = 1;
        public string App { get; set; } = "Webhook-Messenger";
        public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;

        public List<PortableWebhook> Webhooks { get; set; } = new();
        public List<PortableTemplate> Templates { get; set; } = new();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public string ExportToJson(AppDataModel data)
    {
        var portable = new PortableData();

        // Decrypt for portable export
        foreach (var w in data.Webhooks)
        {
            var url = "";
            try
            {
                url = DpapiProtector.UnprotectString(w.UrlProtected);
            }
            catch
            {
                url = "";
            }

            portable.Webhooks.Add(new PortableWebhook
            {
                Name = (w.Name ?? "").Trim(),
                Url = (url ?? "").Trim()
            });
        }

        foreach (var t in data.Templates)
        {
            portable.Templates.Add(new PortableTemplate
            {
                Name = (t.Name ?? "").Trim(),
                Content = t.Content ?? "",
                EmbedsJson = string.IsNullOrWhiteSpace(t.EmbedsJson) ? "[]" : t.EmbedsJson
            });
        }

        return JsonSerializer.Serialize(portable, JsonOpts);
    }

    public PortableData ParsePortable(string json)
    {
        var portable = JsonSerializer.Deserialize<PortableData>(json, JsonOpts);
        if (portable is null) throw new Exception("Import file is empty or invalid.");

        if (portable.Version <= 0) portable.Version = 1;
        portable.Webhooks ??= new();
        portable.Templates ??= new();

        return portable;
    }

    /// <summary>
    /// Merge import into existing data by Name (case-insensitive). Overwrites matches.
    /// Import URLs are re-encrypted with DPAPI.
    /// </summary>
    public void MergeInto(AppDataModel existing, PortableData imported)
    {
        // Webhooks by name
        var existingHooks = existing.Webhooks
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Name.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var w in imported.Webhooks)
        {
            var name = (w.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var urlPlain = (w.Url ?? "").Trim();
            var urlProtected = string.IsNullOrWhiteSpace(urlPlain)
                ? ""
                : DpapiProtector.ProtectString(urlPlain);

            if (existingHooks.TryGetValue(name, out var existingItem))
            {
                existingItem.Name = name;
                if (!string.IsNullOrWhiteSpace(urlProtected))
                    existingItem.UrlProtected = urlProtected;
            }
            else
            {
                existing.Webhooks.Add(new WebhookItem
                {
                    Name = name,
                    UrlProtected = urlProtected
                });
            }
        }

        // Templates by name
        var existingTemplates = existing.Templates
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Name.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var t in imported.Templates)
        {
            var name = (t.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var content = t.Content ?? "";
            var embeds = string.IsNullOrWhiteSpace(t.EmbedsJson) ? "[]" : t.EmbedsJson;

            if (existingTemplates.TryGetValue(name, out var existingItem))
            {
                existingItem.Name = name;
                existingItem.Content = content;
                existingItem.EmbedsJson = embeds;
            }
            else
            {
                existing.Templates.Add(new TemplateItem
                {
                    Name = name,
                    Content = content,
                    EmbedsJson = embeds
                });
            }
        }
    }
}
