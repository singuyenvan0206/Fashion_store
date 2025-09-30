using System;
using System.IO;
using MySql.Data.MySqlClient;

namespace WpfApp1
{
	public class SettingsConfig
	{
		public string Server { get; set; } = "localhost";
		public string Database { get; set; } = "accountsdb";
		public string UserId { get; set; } = "root";
		public string Password { get; set; } = "";
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
			var csb = new MySqlConnectionStringBuilder
			{
				Server = c.Server,
				Database = c.Database,
				UserID = c.UserId,
				Password = c.Password,
				CharacterSet = "utf8mb4",
				AllowUserVariables = true,
				DefaultCommandTimeout = 30
			};
			return csb.ConnectionString;
		}

		public static bool TestConnection(SettingsConfig cfg, out string message)
		{
			try
			{
				var csb = new MySqlConnectionStringBuilder
				{
					Server = cfg.Server,
					Database = cfg.Database,
					UserID = cfg.UserId,
					Password = cfg.Password
				};
				using var conn = new MySqlConnection(csb.ConnectionString);
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
