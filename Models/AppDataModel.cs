using System.Collections.Generic;

namespace WebhookMessenger.Models;

public class AppDataModel
{
    public List<WebhookItem> Webhooks { get; set; } = new();
    public List<TemplateItem> Templates { get; set; } = new();
}
