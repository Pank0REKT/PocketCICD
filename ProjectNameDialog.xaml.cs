using System.Windows;
using System.Windows.Input;

namespace PocketCICD;

public partial class ProjectNameDialog : Window
{
    public string? ProjectName { get; private set; }

    public ProjectNameDialog(string? currentName = null)
    {
        InitializeComponent();
        TxtName.Text = currentName ?? string.Empty;
        TxtName.Focus();
        TxtName.SelectAll();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e) => Confirm();
    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TxtName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  Confirm();
        if (e.Key == Key.Escape) DialogResult = false;
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            TxtName.BorderBrush = System.Windows.Media.Brushes.Red;
            return;
        }
        ProjectName  = TxtName.Text.Trim();
        DialogResult = true;
    }
}