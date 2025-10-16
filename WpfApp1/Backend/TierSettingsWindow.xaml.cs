using System;
using System.Windows;

namespace WpfApp1
{
    public partial class TierSettingsWindow : Window
    {
        public TierSettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                var settings = TierSettingsManager.Load();
                
                // Regular
                RegularMinPointsTextBox.Text = settings.RegularMinPoints.ToString();
                RegularDiscountTextBox.Text = settings.RegularDiscountPercent.ToString();
                RegularBenefitsTextBox.Text = settings.RegularBenefits;
                RegularDescriptionTextBox.Text = settings.RegularDescription;
                
                // Silver
                SilverMinPointsTextBox.Text = settings.SilverMinPoints.ToString();
                SilverDiscountTextBox.Text = settings.SilverDiscountPercent.ToString();
                SilverBenefitsTextBox.Text = settings.SilverBenefits;
                SilverDescriptionTextBox.Text = settings.SilverDescription;
                
                // Gold
                GoldMinPointsTextBox.Text = settings.GoldMinPoints.ToString();
                GoldDiscountTextBox.Text = settings.GoldDiscountPercent.ToString();
                GoldBenefitsTextBox.Text = settings.GoldBenefits;
                GoldDescriptionTextBox.Text = settings.GoldDescription;
                
                // Platinum
                PlatinumMinPointsTextBox.Text = settings.PlatinumMinPoints.ToString();
                PlatinumDiscountTextBox.Text = settings.PlatinumDiscountPercent.ToString();
                PlatinumBenefitsTextBox.Text = settings.PlatinumBenefits;
                PlatinumDescriptionTextBox.Text = settings.PlatinumDescription;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new TierSettings
                {
                    // Regular
                    RegularMinPoints = int.TryParse(RegularMinPointsTextBox.Text, out int regMin) ? regMin : 0,
                    RegularDiscountPercent = decimal.TryParse(RegularDiscountTextBox.Text, out decimal regDisc) ? regDisc : 0,
                    RegularBenefits = RegularBenefitsTextBox.Text.Trim(),
                    RegularDescription = RegularDescriptionTextBox.Text.Trim(),
                    
                    // Silver
                    SilverMinPoints = int.TryParse(SilverMinPointsTextBox.Text, out int silMin) ? silMin : 500,
                    SilverDiscountPercent = decimal.TryParse(SilverDiscountTextBox.Text, out decimal silDisc) ? silDisc : 3,
                    SilverBenefits = SilverBenefitsTextBox.Text.Trim(),
                    SilverDescription = SilverDescriptionTextBox.Text.Trim(),
                    
                    // Gold
                    GoldMinPoints = int.TryParse(GoldMinPointsTextBox.Text, out int goldMin) ? goldMin : 1000,
                    GoldDiscountPercent = decimal.TryParse(GoldDiscountTextBox.Text, out decimal goldDisc) ? goldDisc : 7,
                    GoldBenefits = GoldBenefitsTextBox.Text.Trim(),
                    GoldDescription = GoldDescriptionTextBox.Text.Trim(),
                    
                    // Platinum
                    PlatinumMinPoints = int.TryParse(PlatinumMinPointsTextBox.Text, out int platMin) ? platMin : 2000,
                    PlatinumDiscountPercent = decimal.TryParse(PlatinumDiscountTextBox.Text, out decimal platDisc) ? platDisc : 10,
                    PlatinumBenefits = PlatinumBenefitsTextBox.Text.Trim(),
                    PlatinumDescription = PlatinumDescriptionTextBox.Text.Trim()
                };

                // Validate settings
                if (!ValidateSettings(settings))
                {
                    return;
                }

                if (TierSettingsManager.Save(settings))
                {
                    string message = "Cài đặt hạng thành viên đã được lưu thành công!";
                    
                    // Check if auto-update is enabled
                    if (AutoUpdateTiersCheckBox?.IsChecked == true)
                    {
                        // Update all customer tiers based on new thresholds
                        int updatedCustomers = TierSettingsManager.UpdateAllCustomerTiers();
                        
                        if (updatedCustomers > 0)
                        {
                            message += $"\n\n🔄 Đã tự động cập nhật hạng cho {updatedCustomers} khách hàng theo ngưỡng điểm mới.";
                        }
                        else if (updatedCustomers == 0)
                        {
                            message += "\n\n✅ Không có khách hàng nào cần cập nhật hạng.";
                        }
                        else
                        {
                            message += "\n\n⚠️ Cảnh báo: Có lỗi khi cập nhật hạng khách hàng. Vui lòng kiểm tra lại.";
                        }
                    }
                    else
                    {
                        message += "\n\n📝 Lưu ý: Hạng khách hàng chưa được cập nhật tự động. Bạn có thể cập nhật thủ công nếu cần.";
                    }
                    
                    MessageBox.Show(message, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Không thể lưu cài đặt. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateSettings(TierSettings settings)
        {
            // Validate discount percentages
            if (settings.RegularDiscountPercent < 0 || settings.RegularDiscountPercent > 100 ||
                settings.SilverDiscountPercent < 0 || settings.SilverDiscountPercent > 100 ||
                settings.GoldDiscountPercent < 0 || settings.GoldDiscountPercent > 100 ||
                settings.PlatinumDiscountPercent < 0 || settings.PlatinumDiscountPercent > 100)
            {
                MessageBox.Show("Phần trăm giảm giá phải từ 0 đến 100.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate point progression
            if (settings.SilverMinPoints <= settings.RegularMinPoints ||
                settings.GoldMinPoints <= settings.SilverMinPoints ||
                settings.PlatinumMinPoints <= settings.GoldMinPoints)
            {
                MessageBox.Show("Điểm tối thiểu của các hạng phải tăng dần: Regular < Silver < Gold < Platinum.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate discount progression
            if (settings.SilverDiscountPercent < settings.RegularDiscountPercent ||
                settings.GoldDiscountPercent < settings.SilverDiscountPercent ||
                settings.PlatinumDiscountPercent < settings.GoldDiscountPercent)
            {
                MessageBox.Show("Phần trăm giảm giá của các hạng phải tăng dần: Regular ≤ Silver ≤ Gold ≤ Platinum.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn khôi phục cài đặt mặc định?\nTất cả thay đổi hiện tại sẽ bị mất.", 
                                       "Xác nhận khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var defaultSettings = new TierSettings();
                if (TierSettingsManager.Save(defaultSettings))
                {
                    LoadCurrentSettings();
                    MessageBox.Show("Đã khôi phục cài đặt mặc định.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể khôi phục cài đặt mặc định.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
