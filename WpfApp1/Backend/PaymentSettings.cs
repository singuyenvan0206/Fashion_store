
using System.IO;
using System.Text.Json;

namespace WpfApp1
{
    public class PaymentSettings
    {
        public string BankAccount { get; set; } = "";
        public string BankCode { get; set; } = "";
        public string BankName { get; set; } = "";
        public string AccountHolder { get; set; } = "";
        public bool EnableQRCode { get; set; } = true;
    }

    public static class PaymentSettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfApp1", "payment_settings.json");

        public static PaymentSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<PaymentSettings>(json) ?? new PaymentSettings();
                }
            }
            catch
            {
                // Silent failure
            }
            return new PaymentSettings();
        }

        public static bool Save(PaymentSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

