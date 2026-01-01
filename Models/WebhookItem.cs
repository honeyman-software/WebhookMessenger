using System;

namespace WebhookMessenger.Models;

public class WebhookItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "";
    public string UrlProtected { get; set; } = "";
}
