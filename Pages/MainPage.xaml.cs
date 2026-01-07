using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Dispatching;
using SMA.Helpers;
using SteamAuth;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SMA.Pages;

public partial class MainPage : ContentPage
{
    private SteamGuardAccount currentAccount;
    private ObservableCollection<SteamGuardAccount> accounts = new();
    private Manifest manifest;
    private string? passKey;
    private long steamTime;
    private CancellationTokenSource? tokenSource;

    private TradePopupPage popupFrm = new TradePopupPage();
    private SettingsPopup popupSett;



    private static SemaphoreSlim confirmationsSemaphore = new SemaphoreSlim(1, 1);


    private IDispatcherTimer timerTradesPopup;
    public MainPage()
    {
        InitializeComponent();
        listAccounts.ItemsSource = accounts;
        LoadManifest();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (tokenSource == null || tokenSource.IsCancellationRequested)
            StartTimer();

    }

    private void OnItemFrameTapped(object sender, EventArgs e)
    {
        if (sender is VisualElement ve && ve.BindingContext is SteamGuardAccount acc)
        {
            listAccounts.SelectedItem = acc;
        }
    }

    private async void LoadManifest()
    {
        try
        {
            manifest = Manifest.GetManifest();
        }
        catch
        {
            manifest = Manifest.GenerateNewManifest();
        }

        if (manifest.Encrypted)
            passKey = await manifest.PromptForPassKey();

        LoadAccountsList();
    }

    private void LoadAccountsList()
    {
        currentAccount = null;
        accounts.Clear();

        var allAccounts = manifest.GetAllAccounts(passKey);

        if (allAccounts.Length > 0)
        {
            foreach (var acc in allAccounts)
                accounts.Add(acc);

            listAccounts.SelectedItem = accounts[0];
        }
    }

    private async void StartTimer()
    {
        tokenSource = new CancellationTokenSource();

        try
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                steamTime = await TimeAligner.GetSteamTimeAsync();

                if (currentAccount == null)
                {
                    await Task.Delay(500, tokenSource.Token);
                    continue;
                }

                long chunkStart = (steamTime / 30L) * 30L;
                long elapsed = steamTime - chunkStart;
                long secondsLeft = 30 - elapsed;

                if (secondsLeft <= 0 || secondsLeft > 30)
                    secondsLeft = 30;

                UpdateSteamCodeText(steamTime);

                double startProgress = secondsLeft / 30.0;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    pbTimeout.Progress = startProgress;

                    await pbTimeout.ProgressTo(
                        0.0,
                        (uint)(secondsLeft * 1000),
                        Easing.Linear);
                });
            }
        }
        catch (OperationCanceledException)
        {
            
        }
    }




    private void UpdateSteamCodeText(long steamTime)
    {
        if (currentAccount == null)
            return;

        var code = currentAccount.GenerateSteamGuardCodeForTime(steamTime);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            txtLoginToken.Text = code;
        });
    }


    private void OnAccountSelected(object sender, SelectionChangedEventArgs e)
    {
        currentAccount = e.CurrentSelection.FirstOrDefault() as SteamGuardAccount;

        if (currentAccount != null)
        {
            steamTime = TimeAligner.GetSteamTime();
            UpdateSteamCodeText(steamTime);
        }
    }


    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var filtered = manifest.GetAllAccounts(passKey)
            .Where(a => a.AccountName.Contains(e.NewTextValue ?? "", StringComparison.OrdinalIgnoreCase))
            .ToList();

        accounts.Clear();
        foreach (var acc in filtered)
            accounts.Add(acc);

        if (accounts.Count > 0)
            listAccounts.SelectedItem = accounts[0];
    }

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtLoginToken.Text))
            await Clipboard.SetTextAsync(txtLoginToken.Text);
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Menu", "Cancel", null,
            "Import Account", "Settings", "Login Again", "Remove From Manifest", "Deactivate Authenticator");

        switch (action)
        {
            case "Import Account":
                await ImportAccountAsync();
                break;
            case "Settings":
                await GoToSettings();
                break;
            case "Login Again":
                await PromptRefreshLogin(currentAccount);
                break;
            case "Remove From Manifest":
                if (manifest.Encrypted)
                {
                    await DisplayAlertAsync("Remove from manifest", "You cannot remove accounts from the manifest file while it is encrypted.", "OK");
                }
                else
                {
                    var res = await DisplayAlertAsync("Remove from manifest", "This will remove the selected account from the manifest file.\nUse this to move a maFile to another computer.\nThis will NOT delete your maFile.", "OK", "Cancel");
                    if (res == true)
                    {
                        manifest.RemoveAccount(currentAccount, false);
                        await DisplayAlertAsync("Removed from manifest", "Account removed from manifest.\nYou can now move its maFile to another computer and import it using the File menu.", "OK");
                        LoadAccountsList();
                    }
                }
                break;
            case "Deactivate Authenticator":
                await DeactivateAuthentikator();
                break;
        }
    }
    private async Task DeactivateAuthentikator()
    {
        if (currentAccount == null) return;

        // Check for a valid refresh token first
        if (currentAccount.Session.IsRefreshTokenExpired())
        {
            await DisplayAlertAsync("Deactivate Authenticator", "Your session has expired. Use the login again button under the selected account menu.", "OK");
            return;
        }

        // Check for a valid access token, refresh it if needed
        if (currentAccount.Session.IsAccessTokenExpired())
        {
            try
            {
                await currentAccount.Session.RefreshAccessToken();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Deactivate Authenticator Error", ex.Message,  "OK");
                return;
            }
        }

        string res = await DisplayActionSheetAsync(
            $"Deactivate Authenticator: {currentAccount.AccountName}",
            "Cancel",
            null,
            "Yes - Remove Steam Guard completely",
            "No - Switch back to Email authentication");
        int scheme = 0;
        if (res == "Yes - Remove Steam Guard completely")
        {
            scheme = 2;
        }
        else if (res == "No - Switch back to Email authentication")
        {
            scheme = 1;
        }
        else if (res == "Cancel")
        {
            scheme = 0;
        }

        if (scheme != 0)
        {
            string confCode = currentAccount.GenerateSteamGuardCode();
            InputPage confirmationDialog = new InputPage(String.Format("Removing Steam Guard from {0}. Enter this confirmation code: {1}", currentAccount.AccountName, confCode));
            Navigation.PushAsync(confirmationDialog);
            var result = await confirmationDialog.ShowAsync();

            if (result == null)
            {
                return;
            }

            string enteredCode = confirmationDialog.GetText().ToUpper();
            if (enteredCode != confCode)
            {
                await DisplayAlertAsync("Error","Confirmation codes do not match. Steam Guard not removed.","OK");
                return;
            }

            bool success = await currentAccount.DeactivateAuthenticator(scheme);
            if (success)
            {
                await DisplayAlertAsync(
                scheme == 2 ? "Removed completely" : "Switched to emails",
                $"Steam Guard {(scheme == 2 ? "removed completely" : "switched to emails")}. maFile will be deleted after hitting OK. If you need to make a backup, now's the time.",
                "OK");

                this.manifest.RemoveAccount(currentAccount);
                this.LoadAccountsList();
            }
            else
            {
                await DisplayAlertAsync("Error", "Steam Guard failed to deactivate.", "OK");
            }
        }
        else
        {
            await DisplayAlertAsync("Error", "Steam Guard was not removed. No action was taken.", "OK");
        }
    }
    private async Task ImportAccountAsync()
    {
        await Navigation.PushAsync(new ImportAccountPage());
        bool importResult = await ImportAccountPage.WaitForImportAsync();
        if (importResult)
            LoadAccountsList();
    }
    private async Task GoToSettings()
    {
        await this.ShowPopupAsync(new SettingsPopup(this));
        manifest = Manifest.GetManifest(true);
        LoadSettings();
    }

    private async Task PromptRefreshLogin(SteamGuardAccount account)
    {
        var loginForm = new LoginPage(LoginPage.LoginType.Refresh, account);
        Navigation.PushAsync(loginForm);
    }

    private string[] getAllNames()
    {
        string[] itemArray = new string[accounts.Count];
        for (int i = 0; i < itemArray.Length; i++)
        {
            itemArray[i] = accounts[i].AccountName;
        }
        return itemArray;
    }

    private void LoadSettings()
    {
        timerTradesPopup = Dispatcher.CreateTimer();
        timerTradesPopup.Interval = TimeSpan.FromSeconds(manifest.PeriodicCheckingInterval);


        timerTradesPopup.Tick += TimerTradesPopup_Tick;

        if (manifest.PeriodicChecking)
            timerTradesPopup.Start();
    }



    private async void TimerTradesPopup_Tick(object sender, EventArgs e)
    {
        if (currentAccount == null) return;
        if (!confirmationsSemaphore.Wait(0))
        {
            return;
        }

        List<Confirmation> confs = new List<Confirmation>();
        Dictionary<SteamGuardAccount, List<Confirmation>> autoAcceptConfirmations = new Dictionary<SteamGuardAccount, List<Confirmation>>();

        ObservableCollection<SteamGuardAccount> accs =
            manifest.CheckAllAccounts ? accounts : new ObservableCollection<SteamGuardAccount> { currentAccount };

        try
        {
            lblStatus.Text = "Checking confirmations...";

            foreach (var acc in accs)
            {

                if (acc.Session.IsRefreshTokenExpired())
                {
                    await DisplayAlertAsync("Trade Confirmations", "Your session for account " + acc.AccountName + " has expired. You will be prompted to login again.", "OK");
                    PromptRefreshLogin(acc);
                    break;
                }

                if (acc.Session.IsAccessTokenExpired())
                {
                    try
                    {
                        lblStatus.Text = "Refreshing session...";
                        await acc.Session.RefreshAccessToken();
                        lblStatus.Text = "Checking confirmations...";
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlertAsync("Steam Login Error", ex.Message, "OK");
                        break;
                    }
                }

                try
                {
                    Confirmation[] tmp = await acc.FetchConfirmationsAsync();
                    foreach (var conf in tmp)
                    {
                        if ((conf.ConfType == Confirmation.EMobileConfirmationType.MarketListing && manifest.AutoConfirmMarketTransactions) ||
                            (conf.ConfType == Confirmation.EMobileConfirmationType.Trade && manifest.AutoConfirmTrades))
                        {
                            if (!autoAcceptConfirmations.ContainsKey(acc))
                                autoAcceptConfirmations[acc] = new List<Confirmation>();
                            autoAcceptConfirmations[acc].Add(conf);
                        }
                        else
                            confs.Add(conf);
                    }
                }
                catch (Exception)
                {

                }
            }

            lblStatus.Text = "";

            if (confs.Count > 0)
            {
                popupFrm.Confirmations = confs.ToArray();
                popupFrm.ShowPopup(this);
            }
            if (autoAcceptConfirmations.Count > 0)
            {
                foreach (var acc in autoAcceptConfirmations.Keys)
                {
                    var confirmations = autoAcceptConfirmations[acc].ToArray();
                    await acc.AcceptMultipleConfirmations(confirmations);
                }
            }
        }
        catch (SteamGuardAccount.WGTokenInvalidException)
        {
            lblStatus.Text = "";
        }

        confirmationsSemaphore.Release();
    }




    private void OnSteamLoginClicked(object sender, EventArgs e)
    {
        OpenSteamLogin();
    }

    private async Task OpenSteamLogin()
    {
        await Navigation.PushAsync(new LoginPage());
    }

    private async void OnManageEncryptionClicked(object sender, EventArgs e)
    {
        if (manifest.Encrypted)
        {
            InputPage currentPassKeyForm = new InputPage("Enter current passkey", true);
            await Navigation.PushAsync(currentPassKeyForm);
            var result = currentPassKeyForm.ShowAsync();
            if (result == null)
            {
                return;
            }

            string curPassKey = currentPassKeyForm.GetText();

            InputPage changePassKeyForm = new InputPage("Enter new passkey, or leave blank to remove encryption.");
            await Navigation.PushAsync(changePassKeyForm);
            var result1 = changePassKeyForm.ShowAsync();

            if (result1 == null && !string.IsNullOrEmpty(changePassKeyForm.GetText()))
            {
                return;
            }

            InputPage changePassKeyForm2 = new InputPage("Confirm new passkey, or leave blank to remove encryption.");
            await Navigation.PushAsync(changePassKeyForm2);
            var result2 = changePassKeyForm2.ShowAsync();

            if (result2 == null && !string.IsNullOrEmpty(changePassKeyForm.GetText()))
            {
                return;
            }

            string newPassKey = changePassKeyForm.GetText();
            string confirmPassKey = changePassKeyForm2.GetText();

            if (newPassKey != confirmPassKey)
            {
                await DisplayAlertAsync("Error", "Passkeys do not match.", "OK");
                return;
            }

            if (newPassKey.Length == 0)
            {
                newPassKey = null;
            }

            string action = newPassKey == null ? "remove" : "change";
            if (!manifest.ChangeEncryptionKey(curPassKey, newPassKey))
            {
                await DisplayAlertAsync("Error","Unable to " + action + " passkey.", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error", "Passkey successfully " + action + "d.", "OK");
                this.LoadAccountsList();
            }
        }
        else
        {
            passKey = manifest.PromptSetupPassKey().ToString();
            this.LoadAccountsList();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        tokenSource?.Cancel();
        pbTimeout.AbortAnimation("Progress");
    }

    private void ViewConfirmations_Clicked(object sender, EventArgs e)
    {
        if (currentAccount == null) return;

        string oText = ViewConfirmations.Text;
        ViewConfirmations.Text = "Loading...";
        ViewConfirmations.Text = oText;

        ConfirmationPage confirms = new ConfirmationPage(currentAccount);
        Navigation.PushAsync(confirms);
    }
}
