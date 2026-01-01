using System;
using System.IO;

namespace WebhookMessenger.Services;

public static class AppPaths
{
	public static string DataFolder =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Webhook-Messenger");

	public static string DataFile => Path.Combine(DataFolder, "data.json");

	public static void Ensure()
	{
		if (!Directory.Exists(DataFolder))
			Directory.CreateDirectory(DataFolder);
	}
}
