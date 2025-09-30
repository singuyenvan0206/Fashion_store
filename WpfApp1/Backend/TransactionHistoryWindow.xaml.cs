using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class TransactionHistoryWindow : Window
    {
        private List<InvoiceSummaryItem> _invoices = new();

        public TransactionHistoryWindow()
        {
            InitializeComponent();
            LoadInvoices();
        }

        private void LoadInvoices()
        {
            try
            {
                _invoices = DatabaseHelper.QueryInvoices(null, null, null, "")
                    .Select(i => new InvoiceSummaryItem
                    {
                        InvoiceId = i.Id,
                        CreatedAt = i.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                        CustomerName = i.CustomerName,
                        Total = i.Total.ToString("N0")
                    })
                    .ToList();
                InvoicesGrid.ItemsSource = _invoices;
                if (_invoices.Count > 0)
                {
                    InvoicesGrid.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử giao dịch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is InvoiceSummaryItem inv)
            {
                LoadInvoiceItems(inv.InvoiceId);
            }
        }

        private void LoadInvoiceItems(int invoiceId)
        {
            try
            {
                var items = DatabaseHelper.GetInvoiceItemsDetailed(invoiceId)
                    .Select(x => new InvoiceItemDetail
                    {
                        ProductName = x.ProductName,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice.ToString("N0"),
                        LineTotal = x.LineTotal.ToString("N0")
                    }).ToList();
                InvoiceItemsGrid.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class InvoiceSummaryItem
    {
        public int InvoiceId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;
    }

    public class InvoiceItemDetail
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string UnitPrice { get; set; } = string.Empty;
        public string LineTotal { get; set; } = string.Empty;
    }
}
