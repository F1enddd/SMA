using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using SMA.Helpers;
using SMA.Pages;
using SteamAuth;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Discovery;
using SteamKit2.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMA.Pages
{
    public partial class LoginPage : ContentPage
    {
        private Entry txtUsername;
        private Entry txtPassword;
        private Label labelLoginExplanation;
        private Button btnSteamLogin;

        public SteamGuardAccount account;
        public LoginType LoginReason;
        public SessionData Session;

        private TaskCompletionSource<bool> _tcs;

        public enum LoginType
        {
            Initial,
            Refresh,
            Import
        }

        public LoginPage(LoginType loginReason = LoginType.Initial, SteamGuardAccount account = null)
        {
            this.LoginReason = loginReason;
            this.account = account;

            this.Title = "Login page";

            txtUsername = new Entry { Placeholder = "Username", Margin=10};
            txtPassword = new Entry { Placeholder = "Password", IsPassword = true, Margin = 10 };
            labelLoginExplanation = new Label();
            btnSteamLogin = new Button { Text = "Login", Margin=10};
            btnSteamLogin.Clicked += BtnSteamLogin_Clicked;

            _tcs = new TaskCompletionSource<bool>();

            var stack = new StackLayout
            {
                Padding = 20,
                Children = { txtUsername, txtPassword, labelLoginExplanation, btnSteamLogin }
            };

            Content = stack;

            try
            {
                if (this.LoginReason != LoginType.Initial && account != null)
                {
                    txtUsername.Text = account.AccountName;
                    txtUsername.IsEnabled = false;
                }

                if (this.LoginReason == LoginType.Refresh)
                    labelLoginExplanation.Text = "Your Steam credentials have expired. For trade and market confirmations to work properly, please login again.";
                else if (this.LoginReason == LoginType.Import)
                    labelLoginExplanation.Text = "Please login to your Steam account to import it.";
            }
            catch
            {
                _tcs.TrySetResult(false);
                DisplayAlert("Login Failed", "Failed to find your account. Try closing and reopening the app.", "OK");
                Navigation.PopToRootAsync();
            }
        }

        private void ResetLoginButton()
        {
            btnSteamLogin.IsEnabled = true;
            btnSteamLogin.Text = "Login";
        }

        private Task StartCallbackLoopAsync(CallbackManager manager, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        manager.RunWaitCallbacks(TimeSpan.FromMilliseconds(100));

                        await Task.Delay(50, token);
                    }
                }
                catch (OperationCanceledException)
                {

                }
                catch
                {

                }
            }, token);
        }


        private async Task<bool> ConnectSteamAsync(SteamClient client, CallbackManager manager, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            IDisposable cbConnected = null;
            IDisposable cbDisconnected = null;

            using var ctsLoop = new CancellationTokenSource();
            using var ctsTimeout = new CancellationTokenSource(timeoutMs);

            try
            {
                cbConnected = manager.Subscribe<SteamClient.ConnectedCallback>(_ =>
                {
                    tcs.TrySetResult(true);
                });

                cbDisconnected = manager.Subscribe<SteamClient.DisconnectedCallback>(_ =>
                {
                    tcs.TrySetResult(false);
                });

                var loopTask = StartCallbackLoopAsync(manager, ctsLoop.Token);

                client.Connect();

                using (ctsTimeout.Token.Register(() =>
                    tcs.TrySetResult(false)))
                {
                    var result = await tcs.Task;

                    ctsLoop.Cancel();
                    await loopTask;

                    return result;
                }
            }
            finally
            {
                cbConnected?.Dispose();
                cbDisconnected?.Dispose();
            }
        }


        private async void BtnSteamLogin_Clicked(object sender, EventArgs e)
        {
            btnSteamLogin.IsEnabled = false;
            btnSteamLogin.Text = "Logging in...";

            string username = txtUsername.Text;
            string password = txtPassword.Text;

            var config = SteamConfiguration.Create(b =>
            {
                b.WithProtocolTypes(ProtocolTypes.Tcp);
            });

            var steamClient = new SteamClient(config);
            var cbManager = new CallbackManager(steamClient);

            bool connected = await ConnectSteamAsync(steamClient, cbManager, 60000);
            if (!connected)
            {
                await DisplayAlert(
                    "Steam",
                    "Connection failed. Check INTERNET permission and network.",
                    "OK");

                ResetLoginButton();
                return;
            }

            CredentialsAuthSession authSession;
            try
            {
                authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = username,
                    Password = password,
                    IsPersistentSession = false,
                    PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                    ClientOSType = EOSType.Android9,
                    Authenticator = new UserFormAuthenticator(account),
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Steam Login Error", ex.Message, "OK");
                _tcs.TrySetResult(false);
                await Navigation.PopAsync();
                return;
            }

            AuthPollResult pollResponse;
            try
            {
                pollResponse = await authSession.PollingWaitForResultAsync();
            }
            catch (Exception ex)
            {
                _tcs.TrySetResult(false);
                await DisplayAlert("Steam Login Error", ex.Message, "OK");
                await Navigation.PopAsync();
                return;
            }

            SessionData sessionData = new SessionData
            {
                SteamID = authSession.SteamID.ConvertToUInt64(),
                AccessToken = pollResponse.AccessToken,
                RefreshToken = pollResponse.RefreshToken
            };

            Session = sessionData;

            if (LoginReason == LoginType.Import)
            {
                await Navigation.PopAsync();
                _tcs.TrySetResult(true);
                return;
            }

            if (LoginReason == LoginType.Refresh)
            {
                var manifest = Manifest.GetManifest();
                account.FullyEnrolled = true;
                account.Session = sessionData;
                await HandleManifest(manifest, true);
                _tcs.TrySetResult(true);
                await Navigation.PopAsync();
                return;
            }

            bool addAuthenticator = await DisplayAlert(
                "Steam Login",
                "Steam account login succeeded. Press OK to continue adding SDA as your authenticator.",
                "OK",
                "Cancel"
            );

            if (!addAuthenticator)
            {
                await DisplayAlert("Error", "Adding authenticator aborted.", "OK");
                _tcs.TrySetResult(false);
                ResetLoginButton();
                return;
            }

            AuthenticatorLinker linker = new AuthenticatorLinker(sessionData);
            AuthenticatorLinker.LinkResult linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;

            while (linkResponse != AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                try
                {
                    linkResponse = await linker.AddAuthenticator();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Steam Login", "Error adding your authenticator: " + ex.Message, "OK");
                    _tcs.TrySetResult(false);
                    ResetLoginButton();
                    return;
                }

                switch (linkResponse)
                {
                    case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
                        var phoneInputForm = new PhoneImportPage(account);
                        await Navigation.PushModalAsync(phoneInputForm);
                        if (phoneInputForm.Canceled)
                        {
                            await Navigation.PopAsync();
                            return;
                        }
                        linker.PhoneNumber = phoneInputForm.PhoneNumber;
                        linker.PhoneCountryCode = phoneInputForm.CountryCode;
                        break;

                    case AuthenticatorLinker.LinkResult.AuthenticatorPresent:
                        await DisplayAlert("Steam Login", "This account already has an authenticator linked. You must remove that authenticator to add SDA as your authenticator.", "OK");
                        _tcs.TrySetResult(false);
                        await Navigation.PopAsync();
                        return;

                    case AuthenticatorLinker.LinkResult.FailureAddingPhone:
                        await DisplayAlert("Steam Login", "Failed to add your phone number. Please try again or use a different phone number.", "OK");
                        _tcs.TrySetResult(false);
                        linker.PhoneNumber = null;
                        break;

                    case AuthenticatorLinker.LinkResult.MustRemovePhoneNumber:
                        linker.PhoneNumber = null;
                        break;

                    case AuthenticatorLinker.LinkResult.MustConfirmEmail:
                        await DisplayAlert("Steam Login", "Please check your email, and click the link Steam sent you before continuing.", "OK");
                        break;

                    case AuthenticatorLinker.LinkResult.GeneralFailure:
                        await DisplayAlert("Steam Login Error", "Error adding your authenticator.", "OK");
                        _tcs.TrySetResult(false);
                        await Navigation.PopAsync();
                        return;
                }
            }

            var manifestFinal = Manifest.GetManifest();
            string passKey = null;
            if (manifestFinal.Entries.Count == 0)
            {
                passKey = await manifestFinal.PromptSetupPassKey("Please enter an encryption passkey. Leave blank or hit cancel to not encrypt (VERY INSECURE).");
            }
            else if (manifestFinal.Entries.Count > 0 && manifestFinal.Encrypted)
            {
                bool passKeyValid = false;
                while (!passKeyValid)
                {
                    var passKeyForm = new InputPage("Please enter your current encryption passkey."); // заглушка
                    await Navigation.PushModalAsync(passKeyForm);
                    if (!passKeyForm.cancelled)
                    {
                        passKey = passKeyForm.GetText();
                        passKeyValid = manifestFinal.VerifyPasskey(passKey);
                        if (!passKeyValid)
                            await DisplayAlert("Error", "That passkey is invalid. Please enter the same passkey you used for your other accounts.", "OK");
                    }
                    else
                    {
                        await Navigation.PopAsync();
                        return;
                    }
                    await Navigation.PopAsync();
                }
            }

            if (!manifestFinal.SaveAccount(linker.LinkedAccount, passKey != null, passKey))
            {
                manifestFinal.RemoveAccount(linker.LinkedAccount);
                await DisplayAlert("Error", "Unable to save mobile authenticator file. The mobile authenticator has not been linked.", "OK");
                await Navigation.PopAsync();
                return;
            }

            await DisplayAlert("Info", "The Mobile Authenticator has not yet been linked. Before finalizing the authenticator, please write down your revocation code: " + linker.LinkedAccount.RevocationCode, "OK");

            AuthenticatorLinker.FinalizeResult finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;

            while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
            {
                var smsCodeForm = new InputPage("Please input the SMS code sent to your phone."); // заглушка
                string result = await smsCodeForm.ShowAsync();
                if (result == null)
                {
                    manifestFinal.RemoveAccount(linker.LinkedAccount);
                    await Navigation.PopAsync();
                    return;
                }
                await Navigation.PopAsync();

                var confirmRevocationCode = new InputPage("Please enter your revocation code to ensure you've saved it."); // заглушка
                await Navigation.PushModalAsync(confirmRevocationCode);
                string confirmRevocationCodestr = await confirmRevocationCode.ShowAsync();
                if (confirmRevocationCodestr != linker.LinkedAccount.RevocationCode)
                {
                    await DisplayAlert("Error", "Revocation code incorrect; the authenticator has not been linked.", "OK");
                    manifestFinal.RemoveAccount(linker.LinkedAccount);
                    await Navigation.PopAsync();
                    return;
                }
                await Navigation.PopAsync();

                finalizeResponse = await linker.FinalizeAddAuthenticator(smsCodeForm.GetText());

                if (finalizeResponse == AuthenticatorLinker.FinalizeResult.BadSMSCode)
                    continue;
                if (finalizeResponse == AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes ||
                    finalizeResponse == AuthenticatorLinker.FinalizeResult.GeneralFailure)
                {
                    await DisplayAlert("Error", "Unable to finalize this authenticator. Write down your revocation code if possible: " + linker.LinkedAccount.RevocationCode, "OK");
                    manifestFinal.RemoveAccount(linker.LinkedAccount);
                    await Navigation.PopAsync();
                    return;
                }
            }

            manifestFinal.SaveAccount(linker.LinkedAccount, passKey != null, passKey);
            await DisplayAlert("Success", "Mobile authenticator successfully linked. Revocation code: " + linker.LinkedAccount.RevocationCode, "OK");
            await Navigation.PopAsync();
        }

        private async Task HandleManifest(Manifest man, bool IsRefreshing = false)
        {
            string passKey = null;
            if (man.Entries.Count == 0)
            {
                passKey = await man.PromptSetupPassKey("Please enter an encryption passkey. Leave blank or hit cancel to not encrypt (VERY INSECURE).");
            }
            else if (man.Entries.Count > 0 && man.Encrypted)
            {
                bool passKeyValid = false;
                while (!passKeyValid)
                {
                    var passKeyForm = new InputPage("Please enter your current encryption passkey.");
                    await Navigation.PushModalAsync(passKeyForm);
                    string result = await passKeyForm.ShowAsync();
                    if (result == null)
                    {
                        passKey = passKeyForm.GetText();
                        passKeyValid = man.VerifyPasskey(passKey);
                        if (!passKeyValid)
                            await DisplayAlert("Error", "That passkey is invalid. Please enter the same passkey you used for your other accounts.", "OK");
                    }
                    else
                    {
                        await Navigation.PopAsync();
                        return;
                    }
                    await Navigation.PopAsync();
                }
            }

            man.SaveAccount(account, passKey != null, passKey);
            if (IsRefreshing)
                await DisplayAlert("Steam Login", "Your session was refreshed.", "OK");
            else
                await DisplayAlert("Steam Login", "Mobile authenticator successfully linked. Revocation code: " + account.RevocationCode, "OK");
        }

        public Task<bool> WaitForLoginAsync() => _tcs.Task;
    }
}
