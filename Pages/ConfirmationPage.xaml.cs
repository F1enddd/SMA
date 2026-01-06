using SteamAuth;
using Microsoft.Maui.Controls;
using SMA.Controls;

namespace SMA.Pages;

public partial class ConfirmationPage : ContentPage
{
    private readonly SteamGuardAccount _steamAccount;

    public ConfirmationPage(SteamGuardAccount account)
    {
        InitializeComponent();
        _steamAccount = account;

        Title = $"Trade Confirmations - {account.AccountName}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        ConfirmationsContainer.Children.Clear();

        // Refresh token
        if (_steamAccount.Session.IsRefreshTokenExpired())
        {
            await DisplayAlert("Error",
                "Your session has expired. Please log in again.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // Access token
        if (_steamAccount.Session.IsAccessTokenExpired())
        {
            try
            {
                await _steamAccount.Session.RefreshAccessToken();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Steam Login Error", ex.Message, "OK");
                await Navigation.PopAsync();
                return;
            }
        }

        try
        {
            var confirmations = await _steamAccount.FetchConfirmationsAsync();

            if (confirmations == null || confirmations.Length == 0)
            {
                ConfirmationsContainer.Children.Add(new Label
                {
                    Text = "Nothing to confirm/cancel",
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black
                });
                return;
            }

            foreach (var confirmation in confirmations)
            {
                ConfirmationsContainer.Children.Add(BuildConfirmationView(confirmation));
            }
        }
        catch (Exception ex)
        {
            ConfirmationsContainer.Children.Add(new Label
            {
                Text = "Something went wrong:\n" + ex.Message,
                TextColor = Colors.Red
            });
        }
    }

    private View BuildConfirmationView(Confirmation confirmation)
    {
        var layout = new Grid
        {
            Padding = 10,
            BackgroundColor = Color.FromArgb("#112233"),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };

        // Icon
        if (!string.IsNullOrEmpty(confirmation.Icon))
        {
            var image = new Image
            {
                Source = ImageSource.FromUri(new Uri(confirmation.Icon)),
                HeightRequest = 60,
                WidthRequest = 60
            };

            layout.Add(image, 0, 0);
            Grid.SetRowSpan(image, 3);
        }


        // Headline
        layout.Add(new Label
        {
            Text = $"{confirmation.Headline}\n{confirmation.Creator}",
            TextColor = Colors.White
        }, 1, 0);

        // Buttons
        var acceptButton = new ConfirmationButton
        {
            Text = confirmation.Accept,
            Confirmation = confirmation,
            BackgroundColor = Colors.Black,
            TextColor = Colors.White,
            Margin = new Thickness(0, 5, 0, 0)
        };
        acceptButton.Clicked += OnAcceptClicked;

        var cancelButton = new ConfirmationButton
        {
            Text = confirmation.Cancel,
            Confirmation = confirmation,
            BackgroundColor = Colors.Black,
            TextColor = Colors.White,
            Margin = new Thickness(10, 5, 0, 0)
        };
        cancelButton.Clicked += OnCancelClicked;

        var btnLayout = new HorizontalStackLayout
        {
            Children = { acceptButton, cancelButton }
        };

        layout.Add(btnLayout, 1, 1);

        // Summary
        layout.Add(new Label
        {
            Text = string.Join("\n", confirmation.Summary),
            TextColor = Colors.White
        }, 1, 2);

        return layout;
    }

    private async void OnAcceptClicked(object sender, EventArgs e)
    {
        var button = (ConfirmationButton)sender;
        var confirmation = button.Confirmation;

        await _steamAccount.AcceptConfirmation(confirmation);
        await LoadData();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        var button = (ConfirmationButton)sender;
        var confirmation = button.Confirmation;

        await _steamAccount.DenyConfirmation(confirmation);
        await LoadData();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        RefreshButton.IsEnabled = false;
        RefreshButton.Text = "Refreshing...";

        await LoadData();

        RefreshButton.IsEnabled = true;
        RefreshButton.Text = "Refresh";
    }
}
