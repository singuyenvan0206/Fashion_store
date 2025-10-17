using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WpfApp1
{
	public class SettingsConfig
	{
		public string DatabasePath { get; set; } = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"WpfApp1", "main.db");
	}

	public static class SettingsManager
	{
		private static readonly string SettingsPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"WpfApp1", "settings.json");

		public static SettingsConfig Load()
		{
			try
			{
				if (File.Exists(SettingsPath))
				{
					var json = File.ReadAllText(SettingsPath);
					return System.Text.Json.JsonSerializer.Deserialize<SettingsConfig>(json) ?? new SettingsConfig();
				}
			}
			catch
			{
				// ignore and return defaults
			}
			return new SettingsConfig();
		}

		public static bool Save(SettingsConfig config, out string error)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
				var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(SettingsPath, json);
				error = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		public static string BuildConnectionString()
		{
			var c = Load();
			// Ensure directory exists
			Directory.CreateDirectory(Path.GetDirectoryName(c.DatabasePath)!);
			return $"Data Source={c.DatabasePath}";
		}

		public static bool TestConnection(SettingsConfig cfg, out string message)
		{
			try
			{
				// Ensure directory exists
				Directory.CreateDirectory(Path.GetDirectoryName(cfg.DatabasePath)!);
				var connectionString = $"Data Source={cfg.DatabasePath}";
				using var conn = new SqliteConnection(connectionString);
				conn.Open();
				message = "Connection successful.";
				return true;
			}
			catch (Exception ex)
			{
				message = $"Connection failed: {ex.Message}";
				return false;
			}
		}
	}
}
