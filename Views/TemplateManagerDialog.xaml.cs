using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using WebhookMessenger.Models;
using WebhookMessenger.Services;

namespace WebhookMessenger.Views;

public sealed partial class TemplateManagerDialog : ContentDialog
{
    private readonly DataStore _store;
    private readonly AppDataModel _data;

    public ObservableCollection<TemplateItem> Items { get; } = new();

    public TemplateManagerDialog(DataStore store, AppDataModel data)
    {
        InitializeComponent();
        _store = store;
        _data = data;

        foreach (var t in _data.Templates) Items.Add(t);
        TplList.ItemsSource = Items;

        TplList.SelectionChanged += (_, __) =>
        {
            if (TplList.SelectedItem is TemplateItem t)
            {
                TplName.Text = t.Name;
                TplContent.Text = t.Content ?? "";
                TplEmbeds.Text = string.IsNullOrWhiteSpace(t.EmbedsJson) ? "[]" : t.EmbedsJson;
                StatusText.Text = "Editing selected template.";
            }
        };
    }

    private void AddNew_Click(object sender, RoutedEventArgs e)
    {
        var t = new TemplateItem { Name = "New Template", Content = "", EmbedsJson = "[]" };
        _data.Templates.Add(t);
        Items.Add(t);
        TplList.SelectedItem = t;
        StatusText.Text = "Added new template. Edit it and click Save.";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TplList.SelectedItem is not TemplateItem t) return;

        t.Name = TplName.Text?.Trim() ?? "";
        t.Content = TplContent.Text ?? "";
        t.EmbedsJson = string.IsNullOrWhiteSpace(TplEmbeds.Text) ? "[]" : TplEmbeds.Text;

        await _store.SaveAsync(_data);
        StatusText.Text = "Saved ✅";

        TplList.ItemsSource = null;
        TplList.ItemsSource = Items;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (TplList.SelectedItem is not TemplateItem t) return;

        _data.Templates.Remove(t);
        Items.Remove(t);

        await _store.SaveAsync(_data);
        StatusText.Text = "Deleted ✅";

        TplName.Text = "";
        TplContent.Text = "";
        TplEmbeds.Text = "[]";
    }
}
