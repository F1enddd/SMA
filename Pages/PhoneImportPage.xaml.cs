using SteamAuth;
using System.Text.RegularExpressions;

namespace SMA.Pages;

public partial class PhoneImportPage : ContentPage
{
    private SteamGuardAccount Account;
    public string PhoneNumber;
    public string CountryCode;
    public bool Canceled;
    public PhoneImportPage(SteamGuardAccount account)
	{
        this.Account = account;
        InitializeComponent();
	}

    private async void ButtonSubmit_Clicked(object sender, EventArgs e)
    {
        txtCountryCode.Text = txtCountryCode.Text.ToUpper();
        this.PhoneNumber = txtPhoneNumber.Text;
        this.CountryCode = txtCountryCode.Text;

        if (this.PhoneNumber[0] != '+')
        {
            await DisplayAlert("Phone Number", "Phone number must start with + and country code.", "OK");
            return;
        }

        await Navigation.PopModalAsync();
    }

    private async void ButtonCancel_Clicked(object sender, EventArgs e)
    {
        this.Canceled = true;
        await Navigation.PopModalAsync();
    }

    private static readonly Regex CountryRegex = new(@"[^a-zA-Z]", RegexOptions.Compiled);
    private void txtCountryCode_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue;
        if (string.IsNullOrEmpty(newText))
            return;

        if (CountryRegex.IsMatch(newText))
        {
            entry.Text = CountryRegex.Replace(newText, "");
        }
    }
    private static readonly Regex PhoneRegex = new(@"[^0-9\s\+]", RegexOptions.Compiled);
    private void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue;
        if (string.IsNullOrEmpty(newText))
            return;

        if (PhoneRegex.IsMatch(newText))
        {
            entry.Text = PhoneRegex.Replace(newText, "");
        }
    }
}