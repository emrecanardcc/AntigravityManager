using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AntigravityManager.Models;
using AntigravityManager.Services;
using AntigravityManager.Views;
using Microsoft.Win32;

namespace AntigravityManager;

public partial class MainWindow : Window
{
    private readonly AccountService _accountService;

    public MainWindow()
    {
        InitializeComponent();

        _accountService = new AccountService();
        AccountsList.ItemsSource = _accountService.Accounts;

        if (!string.IsNullOrWhiteSpace(_accountService.Settings.DefaultProjectPath))
        {
            TxtProjectPath.Text = _accountService.Settings.DefaultProjectPath;
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var total = _accountService.Accounts.Count;
        var active = _accountService.Accounts.Count(a => a.IsActive);
        TxtSummary.Text = $"Toplam: {total} Hesap | 🔵 {active} Aktif";
    }

    private void TxtProjectPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_accountService != null)
        {
            _accountService.Settings.DefaultProjectPath = TxtProjectPath.Text.Trim();
            _accountService.Save();
        }
    }

    private void BtnBrowseProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Açılacak Proje Klasörünü Seçin",
            InitialDirectory = Directory.Exists(TxtProjectPath.Text) ? TxtProjectPath.Text : @"c:\Projeler"
        };

        if (dialog.ShowDialog() == true)
        {
            TxtProjectPath.Text = dialog.FolderName;
            _accountService.Settings.DefaultProjectPath = dialog.FolderName;
            _accountService.Save();
        }
    }

    private async void BtnSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AccountProfile account)
        {
            try
            {
                await _accountService.SwitchToAccountAsync(account, TxtProjectPath.Text.Trim(), closePrevious: true);
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geçiş yapılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnAddAccountWizard_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new AddAccountWizardDialog(_accountService)
        {
            Owner = this
        };

        if (wizard.ShowDialog() == true)
        {
            UpdateSummary();
            MessageBox.Show($"Yeni hesap başarıyla eklendi:\n\n{wizard.CreatedAccount?.Name} ({wizard.CreatedAccount?.Email})", 
                            "Tebrikler", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AccountProfile account)
        {
            var dialog = new EditAccountDialog(account)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                _accountService.Save();
                UpdateSummary();
            }
        }
    }

    private void BtnShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AccountProfile account)
        {
            var success = _accountService.CreateShortcut(account);
            if (success)
            {
                MessageBox.Show($"'{account.Name}' için masaüstüne doğrudan başlatma kısayolu oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Kısayol oluşturulamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AccountProfile account)
        {
            var result = MessageBox.Show(
                $"'{account.Name}' ({account.Email}) hesabını listeden silmek istediğinize emin misiniz?",
                "Hesap Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _accountService.DeleteAccount(account);
                UpdateSummary();
            }
        }
    }

    private async void BtnCloseAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Açık olan Antigravity oturumu kapatılacak. Onaylıyor musunuz?",
            "Oturumu Kapat",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _accountService.CloseAntigravityAsync();
            MessageBox.Show("Antigravity oturumu kapatıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
