using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WebhookMessenger.Models;

namespace WebhookMessenger.Services;

public class DataStore
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true
	};

	public async Task<AppDataModel> LoadAsync()
	{
		AppPaths.Ensure();

		if (!File.Exists(AppPaths.DataFile))
			return new AppDataModel();

		try
		{
			var json = await File.ReadAllTextAsync(AppPaths.DataFile);
			return JsonSerializer.Deserialize<AppDataModel>(json, Options) ?? new AppDataModel();
		}
		catch
		{
			// If corrupted, don’t crash.
			return new AppDataModel();
		}
	}

	public async Task SaveAsync(AppDataModel data)
	{
		AppPaths.Ensure();
		var json = JsonSerializer.Serialize(data, Options);
		await File.WriteAllTextAsync(AppPaths.DataFile, json);
	}
}
