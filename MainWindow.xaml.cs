using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using WebhookMessenger.ViewModels;
using WebhookMessenger.Views;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WebhookMessenger;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();

        Root.DataContext = _vm;

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1100, 800));

        Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_initialized) return;
        _initialized = true;
        await _vm.InitializeAsync();
    }

    private async void ManageWebhooks_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new WebhookManagerDialog(_vm.Store, _vm.DataModel)
        {
            XamlRoot = Root.XamlRoot
        };
        await dlg.ShowAsync();
        await _vm.ReloadAsync();
    }

    private async void ManageTemplates_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new TemplateManagerDialog(_vm.Store, _vm.DataModel)
        {
            XamlRoot = Root.XamlRoot
        };
        await dlg.ShowAsync();
        await _vm.ReloadAsync();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ExportAsync();
            _vm.StatusText = "Exported ✅ (JSON contains plaintext webhook URLs)";
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"Export failed: {ex.Message}";
        }
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ImportAsync();
            _vm.StatusText = "Imported ✅";
        }
        catch (Exception ex)
        {
            _vm.StatusText = $"Import failed: {ex.Message}";
        }
    }

    private async Task ExportAsync()
    {
        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("JSON", new[] { ".json" });
        picker.SuggestedFileName = "webhook-messenger-export";

        StorageFile file = await picker.PickSaveFileAsync();
        if (file is null) return;

        // Convert to portable JSON (includes plaintext webhook URLs for portability)
        var json = _vm.ImportExport.ExportToJson(_vm.DataModel);

        await FileIO.WriteTextAsync(file, json);
    }

    private async Task ImportAsync()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".json");

        StorageFile file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var json = await FileIO.ReadTextAsync(file);

        var portable = _vm.ImportExport.ParsePortable(json);

        // Merge into current data
        _vm.ImportExport.MergeInto(_vm.DataModel, portable);

        // Save + refresh UI
        await _vm.Store.SaveAsync(_vm.DataModel);
        await _vm.ReloadAsync();
    }
}
