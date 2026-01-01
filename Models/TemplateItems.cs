using System;

namespace WebhookMessenger.Models;

public class TemplateItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";
    public string EmbedsJson { get; set; } = "[]";
}
