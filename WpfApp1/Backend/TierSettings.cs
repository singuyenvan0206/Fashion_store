using System;
using System.IO;
using System.Text.Json;

namespace WpfApp1
{
    public class TierSettings
    {
        // Regular Tier
        public int RegularMinPoints { get; set; } = 0;
        public decimal RegularDiscountPercent { get; set; } = 0;
        public string RegularBenefits { get; set; } = "Không có ưu đãi đặc biệt";
        public string RegularDescription { get; set; } = "Hạng thành viên cơ bản";

        // Silver Tier
        public int SilverMinPoints { get; set; } = 500;
        public decimal SilverDiscountPercent { get; set; } = 3;
        public string SilverBenefits { get; set; } = "Ưu tiên hỗ trợ khách hàng";
        public string SilverDescription { get; set; } = "Hạng thành viên bạc với ưu đãi cơ bản";

        // Gold Tier
        public int GoldMinPoints { get; set; } = 1000;
        public decimal GoldDiscountPercent { get; set; } = 7;
        public string GoldBenefits { get; set; } = "Miễn phí giao hàng, ưu tiên đặt hàng";
        public string GoldDescription { get; set; } = "Hạng thành viên vàng với nhiều ưu đãi";

        // Platinum Tier
        public int PlatinumMinPoints { get; set; } = 2000;
        public decimal PlatinumDiscountPercent { get; set; } = 10;
        public string PlatinumBenefits { get; set; } = "Tư vấn cá nhân, quà tặng sinh nhật, sự kiện VIP";
        public string PlatinumDescription { get; set; } = "Hạng thành viên cao cấp nhất với đầy đủ ưu đãi";

        // Helper methods
        public decimal GetDiscountForTier(string tier)
        {
            return tier?.ToLower() switch
            {
                "silver" => SilverDiscountPercent,
                "gold" => GoldDiscountPercent,
                "platinum" => PlatinumDiscountPercent,
                _ => RegularDiscountPercent
            };
        }

        public string GetBenefitsForTier(string tier)
        {
            return tier?.ToLower() switch
            {
                "silver" => SilverBenefits,
                "gold" => GoldBenefits,
                "platinum" => PlatinumBenefits,
                _ => RegularBenefits
            };
        }

        public string GetDescriptionForTier(string tier)
        {
            return tier?.ToLower() switch
            {
                "silver" => SilverDescription,
                "gold" => GoldDescription,
                "platinum" => PlatinumDescription,
                _ => RegularDescription
            };
        }

        public string DetermineTierByPoints(int points)
        {
            if (points >= PlatinumMinPoints) return "Platinum";
            if (points >= GoldMinPoints) return "Gold";
            if (points >= SilverMinPoints) return "Silver";
            return "Regular";
        }
    }

    public static class TierSettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WpfApp1", "tier_settings.json");

        public static TierSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<TierSettings>(json) ?? new TierSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tier settings: {ex.Message}");
            }
            return new TierSettings();
        }

        public static bool Save(TierSettings settings)
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
                System.Diagnostics.Debug.WriteLine($"Error saving tier settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the current discount percentage for a tier
        /// </summary>
        public static decimal GetTierDiscount(string tier)
        {
            var settings = Load();
            return settings.GetDiscountForTier(tier);
        }

        /// <summary>
        /// Get the benefits description for a tier
        /// </summary>
        public static string GetTierBenefits(string tier)
        {
            var settings = Load();
            return settings.GetBenefitsForTier(tier);
        }

        /// <summary>
        /// Determine tier based on points using current settings
        /// </summary>
        public static string DetermineTierByPoints(int points)
        {
            var settings = Load();
            return settings.DetermineTierByPoints(points);
        }

        /// <summary>
        /// Update customer tier based on current point thresholds
        /// </summary>
        public static bool UpdateCustomerTierByPoints(int customerId, int points)
        {
            try
            {
                var newTier = DetermineTierByPoints(points);
                return DatabaseHelper.UpdateCustomerLoyalty(customerId, points, newTier);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating customer tier: {ex.Message}");
                return false;
            }
        }
    }
}
