using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using WebhookMessenger.Models;
using WebhookMessenger.Services;

namespace WebhookMessenger.Views;

public sealed partial class WebhookManagerDialog : ContentDialog
{
    private readonly DataStore _store;
    private readonly AppDataModel _data;

    public ObservableCollection<WebhookItem> Items { get; } = new();

    public WebhookManagerDialog(DataStore store, AppDataModel data)
    {
        InitializeComponent();
        _store = store;
        _data = data;

        foreach (var w in _data.Webhooks) Items.Add(w);
        HookList.ItemsSource = Items;

        HookList.SelectionChanged += (_, __) =>
        {
            if (HookList.SelectedItem is WebhookItem w)
            {
                NameBox.Text = w.Name;
                UrlBox.Password = ""; // hidden by default
                StatusText.Text = "Editing selected webhook. URL stays hidden unless you re-enter it.";
            }
        };
    }

    private void AddNew_Click(object sender, RoutedEventArgs e)
    {
        var w = new WebhookItem { Name = "New Webhook", UrlProtected = "" };
        _data.Webhooks.Add(w);
        Items.Add(w);
        HookList.SelectedItem = w;
        StatusText.Text = "Added new webhook. Enter name + URL, then Save.";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (HookList.SelectedItem is not WebhookItem w) return;

        w.Name = NameBox.Text?.Trim() ?? "";

        var urlPlain = UrlBox.Password?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(urlPlain))
            w.UrlProtected = DpapiProtector.ProtectString(urlPlain);

        await _store.SaveAsync(_data);
        StatusText.Text = "Saved ✅";

        // refresh list labels
        HookList.ItemsSource = null;
        HookList.ItemsSource = Items;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (HookList.SelectedItem is not WebhookItem w) return;

        _data.Webhooks.Remove(w);
        Items.Remove(w);

        await _store.SaveAsync(_data);
        StatusText.Text = "Deleted ✅";

        NameBox.Text = "";
        UrlBox.Password = "";
    }
}
