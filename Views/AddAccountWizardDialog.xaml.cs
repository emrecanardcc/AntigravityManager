using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AntigravityManager.Models;
using AntigravityManager.Services;

namespace AntigravityManager.Views;

public partial class AddAccountWizardDialog : Window
{
    private readonly AccountService _accountService;
    private CancellationTokenSource? _cts;
    public AccountProfile? CreatedAccount { get; private set; }

    public AddAccountWizardDialog(AccountService accountService)
    {
        InitializeComponent();
        _accountService = accountService;
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        BtnStart.IsEnabled = false;
        BtnCancel.Content = "Vazgeç";

        PnlInstructions.Visibility = Visibility.Collapsed;
        PnlWaiting.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();

        try
        {
            // 1. Temiz oturum oluştur ve Antigravity'yi başlat
            var (newId, tempDir) = await _accountService.StartNewAccountSessionAsync();

            // 2. Yeni token yazılana kadar dinle
            var account = await _accountService.WaitForNewAccountLoginAsync(newId, tempDir, _cts.Token);
            if (account != null)
            {
                CreatedAccount = account;
                PnlWaiting.Visibility = Visibility.Collapsed;
                PnlSuccess.Visibility = Visibility.Visible;
                TxtSuccessDetails.Text = $"{account.Name} ({account.Email})";

                await Task.Delay(1800);
                DialogResult = true;
                Close();
            }
        }
        catch (OperationCanceledException)
        {
            // İptal edildi
        }
        catch (Exception ex)
        {
            MessageBox.Show($"İşlem sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            _accountService.RestoreActiveCredential();
            DialogResult = false;
            Close();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _accountService.RestoreActiveCredential();
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts?.Cancel();
        base.OnClosed(e);
    }
}
