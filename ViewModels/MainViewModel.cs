using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WebhookMessenger.Models;
using WebhookMessenger.Services;

namespace WebhookMessenger.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DataStore _store = new();
    private readonly DiscordWebhookClient _discord = new();
    private readonly ImportExportService _importExport = new();

    private AppDataModel _data = new();

    public ObservableCollection<WebhookItem> Webhooks { get; } = new();
    public ObservableCollection<TemplateItem> Templates { get; } = new();

    [ObservableProperty] private WebhookItem? selectedWebhook;
    [ObservableProperty] private TemplateItem? selectedTemplate;

    [ObservableProperty] private string messageContent = "";
    [ObservableProperty] private string embedsJson = "[]";
    [ObservableProperty] private string username = "";
    [ObservableProperty] private string avatarUrl = "";
    [ObservableProperty] private bool disableMentions = true;

    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private bool isBusy;

    // Expose for dialogs / window code-behind
    public DataStore Store => _store;
    public AppDataModel DataModel => _data;
    public ImportExportService ImportExport => _importExport;

    public async Task InitializeAsync()
    {
        _data = await _store.LoadAsync();

        Webhooks.Clear();
        foreach (var w in _data.Webhooks) Webhooks.Add(w);

        Templates.Clear();
        foreach (var t in _data.Templates) Templates.Add(t);

        SelectedWebhook = Webhooks.FirstOrDefault();
        SelectedTemplate = null;

        StatusText = Webhooks.Count == 0
            ? "No webhooks saved yet. Use Manage Webhooks to add one."
            : "Ready.";
    }

    public async Task ReloadAsync() => await InitializeAsync();

    partial void OnSelectedTemplateChanged(TemplateItem? value)
    {
        if (value is null) return;

        MessageContent = value.Content ?? "";
        EmbedsJson = string.IsNullOrWhiteSpace(value.EmbedsJson) ? "[]" : value.EmbedsJson;
        StatusText = $"Loaded template: {value.Name}";
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (SelectedWebhook is null)
        {
            StatusText = "Select a webhook first.";
            return;
        }

        // Validate embeds JSON early (optional)
        try
        {
            var txt = (EmbedsJson ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(txt))
            {
                using var _ = System.Text.Json.JsonDocument.Parse(txt);
            }
        }
        catch
        {
            StatusText = "Embeds JSON is invalid.";
            return;
        }

        IsBusy = true;
        StatusText = "Sending...";

        try
        {
            var url = DpapiProtector.UnprotectString(SelectedWebhook.UrlProtected);

            if (string.IsNullOrWhiteSpace(url))
            {
                StatusText = "Selected webhook has no URL. Edit it in Manage Webhooks.";
                return;
            }

            await _discord.SendAsync(
                webhookUrl: url,
                content: MessageContent,
                username: Username,
                avatarUrl: AvatarUrl,
                embedsJson: EmbedsJson ?? "[]",
                disableMentions: DisableMentions
            );

            StatusText = "Sent ✅";
            MessageContent = "";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
