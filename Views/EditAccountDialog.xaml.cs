using System.Windows;
using AntigravityManager.Models;

namespace AntigravityManager.Views;

public partial class EditAccountDialog : Window
{
    private readonly AccountProfile _account;

    public EditAccountDialog(AccountProfile account)
    {
        InitializeComponent();
        _account = account;

        TxtName.Text = account.Name;
        TxtEmail.Text = account.Email;

        TxtName.Focus();
        TxtName.SelectAll();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Lütfen hesap için geçerli bir ad girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _account.Name = TxtName.Text.Trim();
        _account.Email = TxtEmail.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
