using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using System.Threading.Tasks;

namespace SMA.Pages;

public partial class SettingsPopup : Popup
{
    Manifest manifest;
    bool fullyLoaded = false;
    private Page _parentPage;
    public SettingsPopup(Page parent)
	{
        InitializeComponent();
        _parentPage = parent;

        manifest = Manifest.GetManifest(true);

        chkPeriodicChecking.IsChecked = manifest.PeriodicChecking;
        numPeriodicInterval.Value = manifest.PeriodicCheckingInterval;
        chkCheckAll.IsChecked = manifest.CheckAllAccounts;
        chkConfirmMarket.IsChecked = manifest.AutoConfirmMarketTransactions;
        chkConfirmTrades.IsChecked = manifest.AutoConfirmTrades;

        SetControlsEnabledState(chkPeriodicChecking.IsChecked);

        fullyLoaded = true;
    }

    private void Stepper_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        lblInterval.Text = $"Seconds between checking: {(int)e.NewValue}";
    }

    private void btnSave_Clicked(object sender, EventArgs e)
    {
        manifest.PeriodicChecking = chkPeriodicChecking.IsChecked;
        manifest.PeriodicCheckingInterval = (int)numPeriodicInterval.Value;
        manifest.CheckAllAccounts = chkCheckAll.IsChecked;
        manifest.AutoConfirmMarketTransactions = chkConfirmMarket.IsChecked;
        manifest.AutoConfirmTrades = chkConfirmTrades.IsChecked;
        manifest.Save();
        this.CloseAsync();
    }
    private void SetControlsEnabledState(bool enabled)
    {
        numPeriodicInterval.IsEnabled = chkCheckAll.IsEnabled = chkConfirmMarket.IsEnabled = chkConfirmTrades.IsEnabled = enabled;
    }

    private async Task ShowWarning(CheckBox affectedBox)
    {
        if (!fullyLoaded) return;

        var result = await _parentPage.DisplayAlertAsync("Warning!", "Warning: enabling this will severely reduce the security of your items! Use of this option is at your own risk. Would you like to continue?",  "Yes", "Cancel");
        if (result == true)
        {
            affectedBox.IsChecked = true;
        }
    }

    private void chkPeriodicChecking_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        SetControlsEnabledState(chkPeriodicChecking.IsChecked);
    }

    private void chkConfirmMarket_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (chkConfirmMarket.IsChecked)
            ShowWarning(chkConfirmMarket);
    }

    private void chkConfirmTrades_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (chkConfirmTrades.IsChecked)
            ShowWarning(chkConfirmTrades);
    }
}