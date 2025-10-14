using System;
using System.IO;
using System.Text.Json;

namespace WpfApp1
{
    public class PaymentSettings
    {
        public string PaymentMethod { get; set; } = "tpbank";
        public string MoMoPhone { get; set; } = "0123456789";
        public string BankAccount { get; set; } = "000003137";
        public string BankName { get; set; } = "Ngân Hàng Tiên Phong";
        public string BankBIN { get; set; } = "970423";
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading payment settings: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving payment settings: {ex.Message}");
                return false;
            }
        }
    }
}

